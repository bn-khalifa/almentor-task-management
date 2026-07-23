namespace Almentor.TaskApi.Application.Common.Exceptions;

// Thrown when a requested entity does not exist. The middleware maps this to a 404.
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }
}
