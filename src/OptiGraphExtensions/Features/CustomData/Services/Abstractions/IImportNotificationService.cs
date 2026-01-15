using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.CustomData.Models;

namespace OptiGraphExtensions.Features.CustomData.Services.Abstractions
{
    /// <summary>
    /// Service for sending notifications about import execution status.
    /// </summary>
    public interface IImportNotificationService
    {
        /// <summary>
        /// Sends a failure notification after all retry attempts have been exhausted.
        /// </summary>
        /// <param name="config">The import configuration that failed.</param>
        /// <param name="result">The last import result.</param>
        /// <param name="totalAttempts">Total number of attempts made.</param>
        Task SendFailureNotificationAsync(ImportConfiguration config, ImportResult result, int totalAttempts);

        /// <summary>
        /// Sends a success notification after a previously failing import recovers.
        /// </summary>
        /// <param name="config">The import configuration that succeeded.</param>
        /// <param name="result">The import result.</param>
        Task SendRecoveryNotificationAsync(ImportConfiguration config, ImportResult result);
    }
}
