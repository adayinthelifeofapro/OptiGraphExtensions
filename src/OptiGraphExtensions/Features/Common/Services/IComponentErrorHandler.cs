namespace OptiGraphExtensions.Features.Common.Services;

public interface IComponentErrorHandler
{
    Task<TResult> ExecuteWithErrorHandlingAsync<TResult>(
        Func<Task<TResult>> operation, 
        string operationName);

    Task ExecuteWithErrorHandlingAsync(
        Func<Task> operation, 
        string operationName);
}

public record ComponentErrorResult(
    bool IsSuccess,
    string? ErrorMessage,
    string? SuccessMessage
);