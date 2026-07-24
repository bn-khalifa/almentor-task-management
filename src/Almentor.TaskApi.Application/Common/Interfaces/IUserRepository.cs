using Almentor.TaskApi.Domain.Entities;

namespace Almentor.TaskApi.Application.Common.Interfaces;

public interface IUserRepository
{
    /// <summary>Case-insensitive lookup by email; null if no such user.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);

    Task AddAsync(User user, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
