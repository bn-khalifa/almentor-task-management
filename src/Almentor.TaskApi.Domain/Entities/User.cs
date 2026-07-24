using Almentor.TaskApi.Domain.Common;

namespace Almentor.TaskApi.Domain.Entities;

/// <summary>
/// An account that owns projects (and, through them, tasks). A plain domain
/// POCO — no dependency on ASP.NET Identity; only the password hashing borrows
/// Identity's hasher, in the Infrastructure layer.
/// </summary>
public class User : AuditableEntity
{
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    // Projects this user owns.
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
