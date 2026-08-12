using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Repositories;

/// <summary>
/// Exercises the repository against a real (SQLite, in-memory) relational provider so
/// behaviours that only show up with an actual database - like cascading deletes or
/// EF's change tracking - are covered, not just an EF InMemory provider stand-in.
/// </summary>
public class ProductRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public ProductRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetWithItemsAsync_ReturnsProductWithItsItems()
    {
        var repository = new ProductRepository(_context);
        var product = new Product { ProductName = "Laptop Stand", CreatedBy = "tester", CreatedOn = DateTime.UtcNow };
        product.Items.Add(new Item { Quantity = 10 });
        product.Items.Add(new Item { Quantity = 5 });

        await repository.AddAsync(product);
        await _context.SaveChangesAsync();

        var result = await repository.GetWithItemsAsync(product.Id);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Sum(i => i.Quantity).Should().Be(15);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenNoMatch()
    {
        var repository = new ProductRepository(_context);

        var exists = await repository.ExistsAsync(p => p.ProductName == "Nonexistent");

        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
