using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var products = _unitOfWork.Products.Query()
            .Include(p => p.Items)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            products = products.Where(p => p.ProductName.Contains(query.Search));
        }

        var totalCount = await products.CountAsync(cancellationToken);

        var items = await products
            .OrderBy(p => p.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return PagedResult<ProductDto>.Create(items, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<ProductDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetWithItemsAsync(id, cancellationToken);
        if (product is null || product.IsDeleted)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = _mapper.Map<Product>(dto);
        product.CreatedBy = _currentUser.UserName;
        product.CreatedOn = DateTime.UtcNow;

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product is null || product.IsDeleted)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        product.ProductName = dto.ProductName;
        product.ModifiedBy = _currentUser.UserName;
        product.ModifiedOn = DateTime.UtcNow;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product is null || product.IsDeleted)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        // Soft delete keeps referential integrity with any dependent Items.
        product.IsDeleted = true;
        product.ModifiedBy = _currentUser.UserName;
        product.ModifiedOn = DateTime.UtcNow;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
