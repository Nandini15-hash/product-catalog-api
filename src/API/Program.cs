using System.Text;
using API.Extensions;
using API.Filters;
using API.Middleware;
using Application;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Bootstrap logger: catches anything that goes wrong before the full host (and its
// configuration-driven Serilog setup) is even up.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ProductCatalog.API"));

    // ---- Services -----------------------------------------------------

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddScoped<ValidateModelFilter>();
    builder.Services.AddControllers(options => options.Filters.Add<ValidateModelFilter>());

    builder.Services.AddApiVersioningSupport();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerDocumentation();

    builder.Services.AddCors(options => options.AddPolicy("Default", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()));

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<GzipCompressionProvider>();
    });

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

    // Bound lazily via IOptions<JwtSettings>, resolved from DI at the moment the auth
    // handler actually needs it - NOT snapshotted eagerly from builder.Configuration here.
    // TokenService (which signs tokens) reads the same IOptions<JwtSettings> lazily too,
    // so the two are guaranteed to agree. An eager read here previously could disagree
    // with TokenService's lazy read under WebApplicationFactory-based tests (which layer
    // configuration overrides on top after this point), causing every signed token to
    // fail validation with a mismatched secret.
    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
        {
            var settings = jwtOptions.Value;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    // ---- Pipeline -------------------------------------------------------

    app.UseCorrelationId();
    app.UseGlobalExceptionHandling();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at the app root, handy for the "running locally" screenshot.
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
        app.UseHsts();
    }

    app.UseResponseCompression();
    app.UseCors("Default");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timeUtc = DateTime.UtcNow }))
        .AllowAnonymous();

    await EnsureDatabaseAsync(app);

    Log.Information("Product Catalog API starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Product Catalog API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Creates the database schema on first run and seeds a couple of demo rows.
// A real production deployment should use EF Core migrations
// (dotnet ef migrations add InitialCreate) instead of EnsureCreated().
static async Task EnsureDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureCreatedAsync();

    if (!await context.Products.AnyAsync())
    {
        var now = DateTime.UtcNow;
        var products = new List<Product>
        {
            new() { ProductName = "Wireless Mouse", CreatedBy = "seed", CreatedOn = now },
            new() { ProductName = "Mechanical Keyboard", CreatedBy = "seed", CreatedOn = now },
            new() { ProductName = "27-inch Monitor", CreatedBy = "seed", CreatedOn = now }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        context.Items.AddRange(
            new Item { ProductId = products[0].Id, Quantity = 150 },
            new Item { ProductId = products[1].Id, Quantity = 75 },
            new Item { ProductId = products[2].Id, Quantity = 40 });

        await context.SaveChangesAsync();
    }
}

// Exposed for WebApplicationFactory<Program> in the integration test project.
public partial class Program
{
}
