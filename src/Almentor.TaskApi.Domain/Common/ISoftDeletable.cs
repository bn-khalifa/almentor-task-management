namespace Almentor.TaskApi.Domain.Common;

// Marks an entity that is soft-deleted rather than physically removed. 
// Kept separate from <see cref="AuditableEntity"/> on purpose.
// auditing ("when did this change?") nd deletability ("is this row alive?") are distinct capabilities.
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
