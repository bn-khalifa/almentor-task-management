namespace Almentor.TaskApi.Domain.Common;

/* 
   Base class for entities that carry an identity and audit timestamps.
   CreatedAt/UpdatedAt are stamped automatically by the DbContext on save,
   so no service or controller has to remember to set them.
*/
public abstract class AuditableEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
