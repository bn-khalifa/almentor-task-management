using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Features.Auth;
using Almentor.TaskApi.Application.Features.Auth.Dtos;
using Almentor.TaskApi.Application.Features.Auth.Validators;
using Almentor.TaskApi.Domain.Entities;
using NSubstitute;
using Shouldly;

namespace Almentor.TaskApi.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _tokens = Substitute.For<IJwtTokenGenerator>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _tokens.Generate(Arg.Any<User>()).Returns(("signed.jwt.token", DateTime.UtcNow.AddHours(1)));
        _sut = new AuthService(
            _users, _hasher, _tokens,
            new RegisterRequestValidator(), new LoginRequestValidator());
    }

    [Fact]
    public async Task Register_with_a_taken_email_throws_EmailAlreadyExistsException()
    {
        _users.ExistsByEmailAsync("taken@test.local", Arg.Any<CancellationToken>()).Returns(true);

        var request = new RegisterRequest { Email = "taken@test.local", Password = "Password123!" };

        await Should.ThrowAsync<EmailAlreadyExistsException>(
            () => _sut.RegisterAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Register_normalizes_email_hashes_password_and_returns_a_token()
    {
        _users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash("Password123!").Returns("hashed");

        var request = new RegisterRequest { Email = "  MixedCase@Test.Local ", Password = "Password123!" };

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        result.AccessToken.ShouldBe("signed.jwt.token");
        result.Email.ShouldBe("mixedcase@test.local");
        // Stored user has the normalized email and the hashed (never plain) password.
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u != null && u.Email == "mixedcase@test.local" && u.PasswordHash == "hashed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_with_unknown_email_throws_InvalidCredentials()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new LoginRequest { Email = "nobody@test.local", Password = "whatever1" };

        await Should.ThrowAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Login_with_wrong_password_throws_InvalidCredentials()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "user@test.local", PasswordHash = "stored" };
        _users.GetByEmailAsync("user@test.local", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("stored", "wrongpass1").Returns(false);

        var request = new LoginRequest { Email = "user@test.local", Password = "wrongpass1" };

        await Should.ThrowAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Login_with_correct_password_returns_a_token()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "user@test.local", PasswordHash = "stored" };
        _users.GetByEmailAsync("user@test.local", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("stored", "correctpass1").Returns(true);

        var request = new LoginRequest { Email = "user@test.local", Password = "correctpass1" };

        var result = await _sut.LoginAsync(request, CancellationToken.None);

        result.AccessToken.ShouldBe("signed.jwt.token");
        result.UserId.ShouldBe(user.Id);
    }
}
