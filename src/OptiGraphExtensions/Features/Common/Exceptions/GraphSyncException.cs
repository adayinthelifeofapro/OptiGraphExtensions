namespace OptiGraphExtensions.Features.Common.Exceptions;

public class GraphSyncException : OptiGraphExtensionsException
{
    public GraphSyncException(string message) : base(message) { }
    public GraphSyncException(string message, Exception innerException) : base(message, innerException) { }
}