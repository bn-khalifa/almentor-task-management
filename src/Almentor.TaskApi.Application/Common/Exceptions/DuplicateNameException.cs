namespace Almentor.TaskApi.Application.Common.Exceptions;

// Thrown when a project name that must be unique already exists
public class DuplicateNameException : Exception
{
    public DuplicateNameException(string name)
        : base($"A project named '{name}' already exists.")
    {
    }
}
