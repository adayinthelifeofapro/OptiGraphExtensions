namespace OptiGraphExtensions.Features.Common.Exceptions;

public class ValidationException : OptiGraphExtensionsException
{
    public IList<string> ValidationErrors { get; }

    public ValidationException(string message) : base(message)
    {
        ValidationErrors = new List<string> { message };
    }

    public ValidationException(IList<string> validationErrors) 
        : base(string.Join("; ", validationErrors))
    {
        ValidationErrors = validationErrors;
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        ValidationErrors = new List<string> { message };
    }
}