using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class GraphCollectionResponse
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Key { get; set; }
        public bool IsActive { get; set; }

        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? CreatedAt { get; set; }

        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Custom JSON converter that handles DateTime values with space separator (e.g., "2025-12-14 19:54:23.449")
    /// as returned by the Optimizely Graph API, in addition to standard ISO 8601 format.
    /// </summary>
    public class FlexibleDateTimeConverter : JsonConverter<DateTime?>
    {
        private static readonly string[] SupportedFormats = new[]
        {
            "yyyy-MM-dd HH:mm:ss.fff",  // Graph API format with milliseconds
            "yyyy-MM-dd HH:mm:ss",       // Graph API format without milliseconds
            "yyyy-MM-ddTHH:mm:ss.fffZ",  // ISO 8601 with milliseconds and Z
            "yyyy-MM-ddTHH:mm:ssZ",      // ISO 8601 with Z
            "yyyy-MM-ddTHH:mm:ss.fff",   // ISO 8601 with milliseconds
            "yyyy-MM-ddTHH:mm:ss",       // ISO 8601
            "o"                           // Round-trip format
        };

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var dateString = reader.GetString();
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParseExact(dateString, SupportedFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
            {
                return result;
            }

            // Fallback to general parsing
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            throw new JsonException($"Unable to parse DateTime value: {dateString}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
            else
                writer.WriteNullValue();
        }
    }
}