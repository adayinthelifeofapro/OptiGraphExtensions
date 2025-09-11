namespace OptiGraphExtensions.Features.Common.Exceptions;

public abstract class OptiGraphExtensionsException : Exception
{
    protected OptiGraphExtensionsException(string message) : base(message) { }
    protected OptiGraphExtensionsException(string message, Exception innerException) : base(message, innerException) { }
}