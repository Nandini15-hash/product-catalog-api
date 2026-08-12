using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Common;
using Application.DTOs;
using FluentAssertions;
using Xunit;

namespace API.Tests;

// See the comment on AuthControllerTests - same named collection, so xUnit runs
// this class's tests sequentially relative to AuthControllerTests instead of
// spinning up both WebApplicationFactory hosts at the same time.
[Collection("SequentialApiHosts")]
public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSeededProducts()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/products/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutAuthToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/products", new CreateProductDto { ProductName = "Should Fail" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithValidTokenAndPayload_ReturnsCreated_ThenGetByIdReturnsIt()
    {
        var token = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", new CreateProductDto { ProductName = "Integration Test Product" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        created.Should().NotBeNull();
        created!.ProductName.Should().Be("Integration Test Product");

        var getResponse = await _client.GetAsync($"/api/v1/products/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithBlankName_ReturnsBadRequestWithValidationErrors()
    {
        var token = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/products", new CreateProductDto { ProductName = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto
        {
            Username = "admin",
            Password = "Passw0rd!"
        });

        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        return tokens!.AccessToken;
    }
}
