using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Almentor.TaskApi.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    // Emails are stored already-normalized (lower-case), so an exact match is
    // sufficient and index-friendly.
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) =>
        _context.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        user.Id = Guid.NewGuid();
        await _context.Users.AddAsync(user, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
