namespace OptiGraphExtensions.Features.Common.Exceptions;

public class ComponentException : OptiGraphExtensionsException
{
    public string OperationName { get; }

    public ComponentException(string message, string operationName) : base(message)
    {
        OperationName = operationName;
    }

    public ComponentException(string message, string operationName, Exception innerException) 
        : base(message, innerException)
    {
        OperationName = operationName;
    }
}