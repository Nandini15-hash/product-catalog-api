using Domain.Entities;

namespace Application.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default);
}
