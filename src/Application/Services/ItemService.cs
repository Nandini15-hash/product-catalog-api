using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;

public class ItemService : IItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ItemService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ItemDto>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product is null || product.IsDeleted)
        {
            throw new NotFoundException(nameof(Product), productId);
        }

        var items = await _unitOfWork.Items.GetByProductIdAsync(productId, cancellationToken);
        return _mapper.Map<List<ItemDto>>(items);
    }

    public async Task<ItemDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Items.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException(nameof(Item), id);
        }

        return _mapper.Map<ItemDto>(item);
    }

    public async Task<ItemDto> CreateAsync(CreateItemDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product is null || product.IsDeleted)
        {
            throw new NotFoundException(nameof(Product), dto.ProductId);
        }

        var item = _mapper.Map<Item>(dto);

        await _unitOfWork.Items.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ItemDto>(item);
    }

    public async Task UpdateAsync(int id, UpdateItemDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Items.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException(nameof(Item), id);
        }

        item.Quantity = dto.Quantity;

        _unitOfWork.Items.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Items.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException(nameof(Item), id);
        }

        _unitOfWork.Items.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
