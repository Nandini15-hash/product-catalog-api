using Domain.Entities;

namespace Application.Interfaces;

public interface IItemRepository : IGenericRepository<Item>
{
    Task<IReadOnlyList<Item>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
}
