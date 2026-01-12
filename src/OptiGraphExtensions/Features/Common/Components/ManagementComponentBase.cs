using Microsoft.AspNetCore.Components;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Exceptions;

namespace OptiGraphExtensions.Features.Common.Components;

public abstract class ManagementComponentBase<TEntity, TModel> : ComponentBase
    where TEntity : class
    where TModel : class, new()
{
    [Inject]
    protected IComponentErrorHandler ErrorHandler { get; set; } = null!;

    protected bool IsLoading { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    protected virtual void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    protected virtual void SetSuccessMessage(string message)
    {
        SuccessMessage = message;
        ErrorMessage = null;
    }

    protected virtual void SetErrorMessage(string message)
    {
        ErrorMessage = message;
        SuccessMessage = null;
    }

    protected virtual void ResetFormAfterSuccess<T>(ref T model, string successMessage) where T : class, new()
    {
        model = new T();
        SetSuccessMessage(successMessage);
    }

    protected async Task ExecuteWithLoadingAndErrorHandlingAsync(
        Func<Task> operation, 
        string operationName,
        bool showLoading = true)
    {
        try
        {
            if (showLoading)
            {
                IsLoading = true;
                StateHasChanged();
            }

            await ErrorHandler.ExecuteWithErrorHandlingAsync(operation, operationName);
        }
        catch (ComponentException ex)
        {
            SetErrorMessage(ex.Message);
        }
        finally
        {
            if (showLoading)
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
    }

    protected async Task<T?> ExecuteWithLoadingAndErrorHandlingAsync<T>(
        Func<Task<T>> operation, 
        string operationName,
        bool showLoading = true)
    {
        try
        {
            if (showLoading)
            {
                IsLoading = true;
                StateHasChanged();
            }

            return await ErrorHandler.ExecuteWithErrorHandlingAsync(operation, operationName);
        }
        catch (ComponentException ex)
        {
            SetErrorMessage(ex.Message);
            return default;
        }
        finally
        {
            if (showLoading)
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
    }

    protected abstract Task LoadDataAsync();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    // Pagination helper methods
    protected virtual void NavigateToPage(int page, int currentPage, int totalPages, Action<int> setCurrentPage, Func<Task> loadData)
    {
        if (page < 1 || page > totalPages || page == currentPage) return;

        setCurrentPage(page);
        _ = ExecuteBackgroundTaskAsync(loadData);
    }

    /// <summary>
    /// Executes a background task with proper error handling to avoid unobserved exceptions.
    /// </summary>
    private async Task ExecuteBackgroundTaskAsync(Func<Task> task)
    {
        try
        {
            await task();
        }
        catch (Exception ex)
        {
            await InvokeAsync(() =>
            {
                SetErrorMessage($"Background operation failed: {ex.Message}");
                StateHasChanged();
            });
        }
    }

    protected virtual void NavigateToFirstPage(int currentPage, Action<int> setCurrentPage, Func<Task> loadData)
    {
        NavigateToPage(1, currentPage, int.MaxValue, setCurrentPage, loadData);
    }

    protected virtual void NavigateToPreviousPage(int currentPage, Action<int> setCurrentPage, Func<Task> loadData)
    {
        NavigateToPage(currentPage - 1, currentPage, int.MaxValue, setCurrentPage, loadData);
    }

    protected virtual void NavigateToNextPage(int currentPage, int totalPages, Action<int> setCurrentPage, Func<Task> loadData)
    {
        NavigateToPage(currentPage + 1, currentPage, totalPages, setCurrentPage, loadData);
    }

    protected virtual void NavigateToLastPage(int currentPage, int totalPages, Action<int> setCurrentPage, Func<Task> loadData)
    {
        NavigateToPage(totalPages, currentPage, totalPages, setCurrentPage, loadData);
    }
}