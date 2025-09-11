namespace OptiGraphExtensions.Features.Common.Exceptions;

public class ConfigurationException : OptiGraphExtensionsException
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}