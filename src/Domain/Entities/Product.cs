using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A sellable catalog item. Maps to dbo.Product.
/// </summary>
public class Product : BaseAuditableEntity
{
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Soft-delete flag so history/audit trails survive a "delete" operation.
    /// Not part of the original script, added defensively; defaults to false.
    /// </summary>
    public bool IsDeleted { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
