namespace Domain.Common;

/// <summary>
/// Base class for entities that track who created/modified them and when.
/// </summary>
public abstract class BaseAuditableEntity
{
    public int Id { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }
}
