using OptiGraphExtensions.Features.Common.Exceptions;

namespace OptiGraphExtensions.Features.Common.Services;

public class ComponentErrorHandler : IComponentErrorHandler
{
    public async Task<TResult> ExecuteWithErrorHandlingAsync<TResult>(
        Func<Task<TResult>> operation, 
        string operationName)
    {
        try
        {
            return await operation();
        }
        catch (UnauthorizedAccessException)
        {
            throw new ComponentException("You are not authenticated. Please sign in and try again.", operationName);
        }
        catch (ValidationException ex)
        {
            throw new ComponentException(string.Join("; ", ex.ValidationErrors), operationName);
        }
        catch (ArgumentException ex)
        {
            throw new ComponentException(ex.Message, operationName);
        }
        catch (HttpRequestException ex)
        {
            throw new ComponentException(ex.Message, operationName);
        }
        catch (InvalidOperationException ex)
        {
            throw new ComponentException(ex.Message, operationName);
        }
        catch (GraphSyncException ex)
        {
            throw new ComponentException(ex.Message, operationName);
        }
        catch (ConfigurationException ex)
        {
            throw new ComponentException($"Configuration error: {ex.Message}", operationName);
        }
        catch (Exception ex)
        {
            throw new ComponentException($"Unexpected error during {operationName}: {ex.Message}", operationName, ex);
        }
    }

    public async Task ExecuteWithErrorHandlingAsync(
        Func<Task> operation, 
        string operationName)
    {
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            await operation();
            return true; // Return dummy value since we need to return something
        }, operationName);
    }
}