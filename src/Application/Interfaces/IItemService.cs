using Application.DTOs;

namespace Application.Interfaces;

public interface IItemService
{
    Task<IReadOnlyList<ItemDto>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    Task<ItemDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ItemDto> CreateAsync(CreateItemDto dto, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, UpdateItemDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
