using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms.Services
{
    public class SynonymCsvParserService : ISynonymCsvParserService
    {
        private const int MaxRows = 10_000;
        private const int MaxBytes = 5 * 1024 * 1024;

        public SynonymCsvParseResult Parse(string csvContent)
        {
            if (csvContent == null)
            {
                return new SynonymCsvParseResult
                {
                    GlobalErrors = new[] { "CSV content is empty." }
                };
            }

            if (csvContent.Length > 0 && csvContent[0] == '﻿')
            {
                csvContent = csvContent.Substring(1);
            }

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                return new SynonymCsvParseResult
                {
                    GlobalErrors = new[] { "CSV content is empty." }
                };
            }

            if (csvContent.Length > MaxBytes)
            {
                return new SynonymCsvParseResult
                {
                    GlobalErrors = new[] { $"CSV content exceeds maximum size of {MaxBytes / (1024 * 1024)} MB." }
                };
            }

            var rows = new List<SynonymCsvParseRow>();
            var globalErrors = new List<string>();
            var headerDetected = false;
            var synonymCol = 0;
            var languageCol = 1;
            var slotCol = 2;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Mode = CsvMode.RFC4180,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                DetectColumnCountChanges = false,
                IgnoreBlankLines = true,
                HasHeaderRecord = false
            };

            using var reader = new StringReader(csvContent);
            using var parser = new CsvParser(reader, config);

            var isFirstRow = true;

            while (parser.Read())
            {
                if (rows.Count >= MaxRows)
                {
                    return new SynonymCsvParseResult
                    {
                        GlobalErrors = new[] { $"CSV content exceeds maximum of {MaxRows:N0} rows." }
                    };
                }

                var record = parser.Record;
                if (record == null || record.Length == 0)
                {
                    continue;
                }

                if (isFirstRow)
                {
                    isFirstRow = false;
                    if (TryDetectHeader(record, out var mappings))
                    {
                        headerDetected = true;
                        synonymCol = mappings.synonym;
                        languageCol = mappings.language;
                        slotCol = mappings.slot;
                        continue;
                    }
                }

                var row = new SynonymCsvParseRow
                {
                    LineNumber = parser.Row
                };

                if (record.Length != 3)
                {
                    row.ParseError = $"Expected 3 columns, found {record.Length}.";
                }
                else
                {
                    row.Synonym = record[synonymCol];
                    row.Language = record[languageCol];
                    row.Slot = record[slotCol];
                }

                rows.Add(row);
            }

            if (rows.Count == 0)
            {
                globalErrors.Add(headerDetected ? "No data rows found after header." : "No data rows found.");
            }

            return new SynonymCsvParseResult
            {
                Rows = rows,
                GlobalErrors = globalErrors,
                HeaderDetected = headerDetected
            };
        }

        private static bool TryDetectHeader(string[] record, out (int synonym, int language, int slot) mappings)
        {
            mappings = (0, 1, 2);
            if (record.Length != 3)
            {
                return false;
            }

            var indices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < record.Length; i++)
            {
                var name = record[i]?.Trim().ToLowerInvariant() ?? string.Empty;
                if (name != "synonym" && name != "language" && name != "slot")
                {
                    return false;
                }

                if (indices.ContainsKey(name))
                {
                    return false;
                }

                indices[name] = i;
            }

            if (indices.Count != 3)
            {
                return false;
            }

            mappings = (indices["synonym"], indices["language"], indices["slot"]);
            return true;
        }
    }
}
