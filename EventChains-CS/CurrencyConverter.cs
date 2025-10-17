using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventChains_CS
{
    /// <summary>
    /// Custom JSON converter that handles currency-formatted strings like "$78139781.29"
    /// </summary>
    public class CurrencyConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDecimal();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();

                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }

                // Remove currency symbols, commas, and whitespace
                var cleanedValue = stringValue
                    .Replace("$", "")
                    .Replace("£", "")
                    .Replace("€", "")
                    .Replace("¥", "")
                    .Replace(",", "")
                    .Trim();

                if (decimal.TryParse(cleanedValue, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }

                // If parsing fails, return null instead of throwing
                return null;
            }

            throw new JsonException($"Unable to convert \"{reader.GetString()}\" to decimal.");
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue(value.Value);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}