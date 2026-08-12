namespace Domain.Entities;

/// <summary>
/// Stock/line-item record tied to a Product. Maps to dbo.Item.
/// </summary>
public class Item
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public Product? Product { get; set; }
}
