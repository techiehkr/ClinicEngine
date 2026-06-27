namespace ClinicEngine.Domain.Common;


// Base class for all entities.
public abstract class AuditableEntity
{
    public int Id { get; protected set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
