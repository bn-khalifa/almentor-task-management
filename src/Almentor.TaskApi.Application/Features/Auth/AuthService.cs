using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Features.Auth.Dtos;
using Almentor.TaskApi.Domain.Entities;
using FluentValidation;

namespace Almentor.TaskApi.Application.Features.Auth;

/// <summary>
/// Registration and login. Emails are normalized to lower-case so uniqueness and
/// lookups are case-insensitive regardless of DB collation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        await _registerValidator.ValidateAndThrowAsync(request, ct);

        var email = Normalize(request.Email);

        if (await _userRepository.ExistsByEmailAsync(email, ct))
        {
            throw new EmailAlreadyExistsException(email);
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        await _userRepository.AddAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);

        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await _loginValidator.ValidateAndThrowAsync(request, ct);

        var user = await _userRepository.GetByEmailAsync(Normalize(request.Email), ct);

        // Same exception whether the email is unknown or the password is wrong —
        // don't leak which accounts exist.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        return BuildResponse(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expiresAt) = _tokenGenerator.Generate(user);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAt,
            UserId = user.Id,
            Email = user.Email
        };
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
