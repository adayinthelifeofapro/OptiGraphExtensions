using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.Security.Claims;
using System.Text;

using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms
{
    public class SynonymBulkUploadModalBase : ComponentBase
    {
        private const long MaxFileBytes = 5 * 1024 * 1024;

        protected const string PastePlaceholder = "synonym,language,slot\n\"car, auto, vehicle\",en,ONE";

        protected enum ModalState
        {
            Empty,
            Validating,
            Validated,
            Importing,
            Done
        }

        [Inject]
        protected ISynonymBulkImportService BulkImportService { get; set; } = null!;

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

        [Parameter]
        public bool IsOpen { get; set; }

        [Parameter]
        public EventCallback OnClose { get; set; }

        [Parameter]
        public EventCallback OnImported { get; set; }

        protected ModalState State { get; set; } = ModalState.Empty;
        protected string PastedContent { get; set; } = string.Empty;
        protected string? SelectedFileName { get; set; }
        protected string? FileLoadError { get; set; }
        protected string? PendingCsvContent { get; set; }
        protected BulkSynonymPreviewResult? Preview { get; set; }
        protected BulkSynonymImportResult? ImportResult { get; set; }
        protected string? GeneralError { get; set; }

        protected bool CanValidate =>
            (State == ModalState.Empty || State == ModalState.Validated)
            && !string.IsNullOrWhiteSpace(PendingCsvContent);

        protected bool CanImport =>
            State == ModalState.Validated
            && Preview is not null
            && Preview.HasAnythingToImport;

        protected async Task OnFileSelected(InputFileChangeEventArgs e)
        {
            FileLoadError = null;
            GeneralError = null;
            var file = e.File;
            if (file == null)
            {
                SelectedFileName = null;
                PendingCsvContent = string.IsNullOrWhiteSpace(PastedContent) ? null : PastedContent;
                return;
            }

            if (file.Size > MaxFileBytes)
            {
                FileLoadError = $"File exceeds maximum size of {MaxFileBytes / (1024 * 1024)} MB.";
                SelectedFileName = file.Name;
                PendingCsvContent = null;
                return;
            }

            try
            {
                using var stream = file.OpenReadStream(MaxFileBytes);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                PendingCsvContent = await reader.ReadToEndAsync();
                SelectedFileName = file.Name;
                PastedContent = string.Empty;
                ResetValidationState();
            }
            catch (IOException ex)
            {
                FileLoadError = ex.Message;
                PendingCsvContent = null;
            }
        }

        protected void OnPastedContentChanged(ChangeEventArgs e)
        {
            PastedContent = e.Value?.ToString() ?? string.Empty;
            FileLoadError = null;
            SelectedFileName = null;
            PendingCsvContent = string.IsNullOrWhiteSpace(PastedContent) ? null : PastedContent;
            ResetValidationState();
        }

        protected async Task Validate()
        {
            if (string.IsNullOrWhiteSpace(PendingCsvContent))
            {
                GeneralError = "Provide a CSV file or paste CSV content first.";
                return;
            }

            GeneralError = null;
            State = ModalState.Validating;
            StateHasChanged();

            try
            {
                Preview = await BulkImportService.BuildPreviewAsync(PendingCsvContent);
                State = ModalState.Validated;
            }
            catch (Exception ex)
            {
                GeneralError = $"Failed to validate CSV: {ex.Message}";
                State = ModalState.Empty;
            }
        }

        protected async Task Import()
        {
            if (Preview is null || !Preview.HasAnythingToImport)
            {
                return;
            }

            State = ModalState.Importing;
            GeneralError = null;
            StateHasChanged();

            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var createdBy = authState.User?.Identity?.Name;
                ImportResult = await BulkImportService.ImportAsync(Preview, createdBy);
                State = ModalState.Done;
            }
            catch (Exception ex)
            {
                GeneralError = $"Import failed: {ex.Message}";
                State = ModalState.Validated;
            }
        }

        protected async Task CloseAndReset()
        {
            var didImport = State == ModalState.Done && ImportResult is not null && ImportResult.InsertedCount > 0;
            Reset();
            await OnClose.InvokeAsync();
            if (didImport)
            {
                await OnImported.InvokeAsync();
            }
        }

        protected void Reset()
        {
            State = ModalState.Empty;
            PastedContent = string.Empty;
            SelectedFileName = null;
            PendingCsvContent = null;
            Preview = null;
            ImportResult = null;
            FileLoadError = null;
            GeneralError = null;
        }

        private void ResetValidationState()
        {
            if (State != ModalState.Empty)
            {
                State = ModalState.Empty;
            }
            Preview = null;
            ImportResult = null;
        }

        protected static string GetStatusBadgeClass(BulkRowStatus status) => status switch
        {
            BulkRowStatus.Valid => "epi-status-badge epi-status-badge--success",
            BulkRowStatus.DuplicateInFile => "epi-status-badge epi-status-badge--warning",
            BulkRowStatus.DuplicateInDatabase => "epi-status-badge epi-status-badge--warning",
            BulkRowStatus.Invalid => "epi-status-badge epi-status-badge--danger",
            _ => "epi-status-badge"
        };

        protected static string GetStatusLabel(BulkRowStatus status) => status switch
        {
            BulkRowStatus.Valid => "Valid",
            BulkRowStatus.DuplicateInFile => "Duplicate (in file)",
            BulkRowStatus.DuplicateInDatabase => "Duplicate (in DB)",
            BulkRowStatus.Invalid => "Invalid",
            _ => status.ToString()
        };
    }
}
