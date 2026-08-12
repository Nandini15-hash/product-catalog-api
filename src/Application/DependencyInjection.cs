using System.Reflection;
using Application.Interfaces;
using Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper 13+ folded AddAutoMapper into the core package and dropped the
        // separate AutoMapper.Extensions.Microsoft.DependencyInjection package; every
        // overload now takes a (possibly empty) config action as the first argument.
        services.AddAutoMapper(_ => { }, Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IItemService, ItemService>();

        return services;
    }
}
