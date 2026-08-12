using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Application.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly IMapper _mapper;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        // Built through the same AddAutoMapper(...) DI registration used in
        // Application/DependencyInjection.cs, rather than constructing
        // MapperConfiguration directly - AutoMapper has changed that
        // constructor's signature across major versions before.
        var services = new ServiceCollection();
        services.AddLogging(); // AutoMapper 15's DI registration resolves ILoggerFactory internally
        services.AddAutoMapper(_ => { }, typeof(MappingProfile).Assembly);
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepositoryMock.Object);
        _currentUserMock.Setup(u => u.UserName).Returns("test-user");

        _sut = new ProductService(_unitOfWorkMock.Object, _mapper, _currentUserMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsMappedDetailDto()
    {
        var product = new Product { Id = 1, ProductName = "Widget", CreatedBy = "seed", CreatedOn = DateTime.UtcNow };
        _productRepositoryMock
            .Setup(r => r.GetWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.ProductName.Should().Be("Widget");
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductMissing_ThrowsNotFoundException()
    {
        _productRepositoryMock
            .Setup(r => r.GetWithItemsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_PersistsProductAndStampsAuditFields()
    {
        Product? captured = null;
        _productRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        var dto = new CreateProductDto { ProductName = "New Product" };

        var result = await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.ProductName.Should().Be("New Product");
        captured.CreatedBy.Should().Be("test-user");
        result.ProductName.Should().Be("New Product");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductMissing_ThrowsNotFoundException()
    {
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(5));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesAndSaves()
    {
        var product = new Product { Id = 5, ProductName = "To be removed", CreatedBy = "seed", CreatedOn = DateTime.UtcNow };
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        await _sut.DeleteAsync(5);

        product.IsDeleted.Should().BeTrue();
        _productRepositoryMock.Verify(r => r.Update(product), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
