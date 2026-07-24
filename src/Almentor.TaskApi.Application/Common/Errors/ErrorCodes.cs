namespace Almentor.TaskApi.Application.Common.Errors;

// Stable, machine-readable error codes returned in <c>ApiResponse.Error.Code</c>.
// Clients should branch on these, never on the human-readable message.
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string DuplicateName = "DUPLICATE_NAME";
    public const string Conflict = "CONFLICT";
    public const string InternalError = "INTERNAL_ERROR";
    public const string EmailTaken = "EMAIL_TAKEN";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
}
