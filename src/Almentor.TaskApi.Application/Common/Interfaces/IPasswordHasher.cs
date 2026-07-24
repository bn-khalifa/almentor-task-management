namespace Almentor.TaskApi.Application.Common.Interfaces;

/// <summary>
/// Hashes and verifies passwords. A tiny abstraction so the Application layer
/// stays free of any specific crypto library; Infrastructure implements it over
/// ASP.NET Core's PasswordHasher (PBKDF2, salted, versioned).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}
