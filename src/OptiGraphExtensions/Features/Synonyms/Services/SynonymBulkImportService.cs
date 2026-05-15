using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Repositories;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms.Services
{
    public class SynonymBulkImportService : ISynonymBulkImportService
    {
        private const int MaxSynonymLength = 255;
        private const int MaxLanguageLength = 10;

        private readonly ISynonymCsvParserService _parser;
        private readonly ISynonymRepository _repository;
        private readonly ISynonymGraphSyncService _graphSyncService;

        public SynonymBulkImportService(
            ISynonymCsvParserService parser,
            ISynonymRepository repository,
            ISynonymGraphSyncService graphSyncService)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _graphSyncService = graphSyncService ?? throw new ArgumentNullException(nameof(graphSyncService));
        }

        public async Task<BulkSynonymPreviewResult> BuildPreviewAsync(string csvContent)
        {
            var parseResult = _parser.Parse(csvContent);

            if (parseResult.GlobalErrors.Count > 0)
            {
                return new BulkSynonymPreviewResult
                {
                    Rows = Array.Empty<BulkSynonymPreviewRow>(),
                    GlobalErrors = parseResult.GlobalErrors
                };
            }

            var existing = await _repository.GetAllAsync();
            var existingKeys = new HashSet<string>(
                existing.Select(BuildKey),
                StringComparer.Ordinal);

            var rows = new List<BulkSynonymPreviewRow>();
            var seenInFile = new HashSet<string>(StringComparer.Ordinal);

            foreach (var parsed in parseResult.Rows)
            {
                rows.Add(EvaluateRow(parsed, existingKeys, seenInFile));
            }

            return new BulkSynonymPreviewResult
            {
                Rows = rows,
                GlobalErrors = Array.Empty<string>()
            };
        }

        public async Task<BulkSynonymImportResult> ImportAsync(BulkSynonymPreviewResult preview, string? createdBy)
        {
            ArgumentNullException.ThrowIfNull(preview);

            var result = new BulkSynonymImportResult
            {
                SkippedDuplicateCount = preview.DuplicateInFileCount + preview.DuplicateInDatabaseCount,
                InvalidSkippedCount = preview.InvalidCount
            };

            var validRows = preview.Rows.Where(r => r.Status == BulkRowStatus.Valid).ToList();
            if (validRows.Count == 0)
            {
                return result;
            }

            var now = DateTime.UtcNow;
            var entities = validRows.Select(r => new Synonym
            {
                Id = Guid.NewGuid(),
                SynonymItem = r.Synonym,
                Language = r.Language,
                Slot = r.Slot ?? SynonymSlot.ONE,
                CreatedAt = now,
                CreatedBy = createdBy
            }).ToList();

            await _repository.CreateManyAsync(entities);
            result.InsertedCount = entities.Count;

            var affectedLanguages = entities
                .Where(e => !string.IsNullOrEmpty(e.Language))
                .Select(e => e.Language!)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            result.AffectedLanguages = affectedLanguages;
            result.GraphSyncAttempted = true;

            var syncErrors = new List<string>();
            foreach (var language in affectedLanguages)
            {
                try
                {
                    await _graphSyncService.SyncSynonymsForLanguageAsync(language);
                }
                catch (GraphSyncException ex)
                {
                    syncErrors.Add($"Language '{language}': {ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    syncErrors.Add($"Language '{language}': {ex.Message}");
                }
            }

            if (syncErrors.Count > 0)
            {
                result.GraphSyncSucceeded = false;
                result.GraphSyncErrorMessage = string.Join("; ", syncErrors);
            }
            else
            {
                result.GraphSyncSucceeded = true;
            }

            return result;
        }

        private static BulkSynonymPreviewRow EvaluateRow(
            SynonymCsvParseRow parsed,
            HashSet<string> existingKeys,
            HashSet<string> seenInFile)
        {
            var row = new BulkSynonymPreviewRow
            {
                LineNumber = parsed.LineNumber,
                Synonym = parsed.Synonym?.Trim(),
                Language = parsed.Language?.Trim(),
                RawSlot = parsed.Slot
            };

            if (!string.IsNullOrEmpty(parsed.ParseError))
            {
                row.Status = BulkRowStatus.Invalid;
                row.Message = parsed.ParseError;
                return row;
            }

            if (string.IsNullOrWhiteSpace(row.Synonym))
            {
                row.Status = BulkRowStatus.Invalid;
                row.Message = "Synonym is required.";
                return row;
            }

            if (row.Synonym.Length > MaxSynonymLength)
            {
                row.Status = BulkRowStatus.Invalid;
                row.Message = $"Synonym must be {MaxSynonymLength} characters or less.";
                return row;
            }

            if (string.IsNullOrWhiteSpace(row.Language))
            {
                row.Status = BulkRowStatus.Invalid;
                row.Message = "Language is required.";
                return row;
            }

            if (row.Language.Length > MaxLanguageLength)
            {
                row.Status = BulkRowStatus.Invalid;
                row.Message = $"Language must be {MaxLanguageLength} characters or less.";
                return row;
            }

            if (!TryParseSlot(parsed.Slot, out var slot))
            {
                row.Status = BulkRowStatus.Invalid;
                row.Message = $"Slot must be ONE or TWO (received '{parsed.Slot}').";
                return row;
            }

            row.Slot = slot;

            var key = BuildKey(row.Synonym, row.Language, slot);

            if (!seenInFile.Add(key))
            {
                row.Status = BulkRowStatus.DuplicateInFile;
                row.Message = "Duplicate of an earlier row in this upload.";
                return row;
            }

            if (existingKeys.Contains(key))
            {
                row.Status = BulkRowStatus.DuplicateInDatabase;
                row.Message = "Already exists in the database.";
                return row;
            }

            row.Status = BulkRowStatus.Valid;
            return row;
        }

        private static bool TryParseSlot(string? input, out SynonymSlot slot)
        {
            slot = SynonymSlot.ONE;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var trimmed = input.Trim();
            if (string.Equals(trimmed, "ONE", StringComparison.OrdinalIgnoreCase) || trimmed == "1")
            {
                slot = SynonymSlot.ONE;
                return true;
            }

            if (string.Equals(trimmed, "TWO", StringComparison.OrdinalIgnoreCase) || trimmed == "2")
            {
                slot = SynonymSlot.TWO;
                return true;
            }

            return false;
        }

        private static string BuildKey(Synonym synonym) =>
            BuildKey(synonym.SynonymItem, synonym.Language, synonym.Slot);

        private static string BuildKey(string? synonym, string? language, SynonymSlot slot) =>
            $"{synonym?.Trim().ToLowerInvariant()}|{language?.Trim().ToLowerInvariant()}|{slot}";
    }
}
