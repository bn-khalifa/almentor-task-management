using Almentor.TaskApi.Application.Features.Auth.Dtos;
using FluentValidation;

namespace Almentor.TaskApi.Application.Features.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Only presence is validated here; a wrong (but well-formed) credential
        // yields 401 from the service, not a 400 — we don't reveal which field failed.
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}
