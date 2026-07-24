namespace Almentor.TaskApi.Application.Common.Exceptions;

/// <summary>
/// Login with a wrong email or password. Deliberately does not say which was
/// wrong (avoids account enumeration). Mapped to 401 Unauthorized.
/// </summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
