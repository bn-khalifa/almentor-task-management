namespace Almentor.TaskApi.Application.Common.Exceptions;

/// <summary>Registration with an email that's already taken. Mapped to 409 Conflict.</summary>
public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email)
        : base($"An account with email '{email}' already exists.")
    {
    }
}
