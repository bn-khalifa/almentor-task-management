using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Domain.Entities;
using Identity = Microsoft.AspNetCore.Identity;

namespace Almentor.TaskApi.Infrastructure.Auth;

/// <summary>
/// Wraps ASP.NET Core's <see cref="Identity.PasswordHasher{TUser}"/>  
/// behind our small <see cref="IPasswordHasher"/> — using its battle-tested 
/// hashing without pulling the rest of the Identity stack into the app.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private static readonly User Dummy = new();
    private readonly Identity.PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(Dummy, password);

    public bool Verify(string hash, string password)
    {
        var result = _inner.VerifyHashedPassword(Dummy, hash, password);
        return result != Identity.PasswordVerificationResult.Failed;
    }
}
