using System.Globalization;
using EventChains_CS.DTOs;

namespace EventChains_CS.Utils
{
    /// <summary>
    /// Utility class for parsing Titanic CSV data
    /// </summary>
    public static class TitanicCsvParser
    {
        /// <summary>
        /// Parses a Titanic CSV file and returns a list of TitanicPassenger objects
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>List of parsed TitanicPassenger objects</returns>
        public static async Task<List<TitanicPassenger>> ParseCsvAsync(string filePath)
        {
            var passengers = new List<TitanicPassenger>();

            using (var reader = new StreamReader(filePath))
            {
                // Read header line
                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(headerLine))
                {
                    throw new InvalidDataException("CSV file is empty or has no header");
                }

                var headers = ParseCsvLine(headerLine);
                var columnMap = MapHeaders(headers);

                // Read data lines
                int lineNumber = 1;
                while (!reader.EndOfStream)
                {
                    lineNumber++;
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var values = ParseCsvLine(line);
                        var passenger = ParsePassenger(values, columnMap, lineNumber);
                        passengers.Add(passenger);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to parse line {lineNumber}: {ex.Message}");
                        // Continue processing other lines
                    }
                }
            }

            return passengers;
        }

        /// <summary>
        /// Parses a CSV line, handling quoted fields properly
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentField.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        // Toggle quote state
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // Field separator
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            // Add last field
            fields.Add(currentField.ToString().Trim());

            return fields;
        }

        /// <summary>
        /// Maps CSV headers to column indices
        /// </summary>
        private static Dictionary<string, int> MapHeaders(List<string> headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i].Trim();
                map[header] = i;
            }

            // Verify required columns exist
            var requiredColumns = new[] 
            { 
                "PassengerId", "Survived", "Pclass", "Name", "Sex", 
                "Age", "SibSp", "Parch", "Ticket", "Fare", "Cabin", "Embarked" 
            };

            var missingColumns = requiredColumns.Where(col => !map.ContainsKey(col)).ToList();
            if (missingColumns.Any())
            {
                throw new InvalidDataException(
                    $"CSV is missing required columns: {string.Join(", ", missingColumns)}");
            }

            return map;
        }

        /// <summary>
        /// Parses a single passenger from CSV values
        /// </summary>
        private static TitanicPassenger ParsePassenger(
            List<string> values, 
            Dictionary<string, int> columnMap, 
            int lineNumber)
        {
            try
            {
                var passenger = new TitanicPassenger
                {
                    PassengerId = ParseInt(values[columnMap["PassengerId"]], "PassengerId"),
                    Survived = ParseInt(values[columnMap["Survived"]], "Survived"),
                    Pclass = ParseInt(values[columnMap["Pclass"]], "Pclass"),
                    Name = ParseString(values[columnMap["Name"]], "Name"),
                    Sex = ParseString(values[columnMap["Sex"]], "Sex").ToLower(),
                    Age = ParseNullableDouble(values[columnMap["Age"]]),
                    SibSp = ParseInt(values[columnMap["SibSp"]], "SibSp"),
                    Parch = ParseInt(values[columnMap["Parch"]], "Parch"),
                    Ticket = ParseString(values[columnMap["Ticket"]], "Ticket"),
                    Fare = ParseDouble(values[columnMap["Fare"]], "Fare"),
                    Cabin = ParseNullableString(values[columnMap["Cabin"]]),
                    Embarked = ParseString(values[columnMap["Embarked"]], "Embarked")
                };

                return passenger;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Error parsing line {lineNumber}: {ex.Message}", ex);
            }
        }

        #region Parsing Helper Methods

        private static int ParseInt(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{fieldName} is required but was empty");

            if (!int.TryParse(value, out int result))
                throw new FormatException($"{fieldName} must be an integer, got: '{value}'");

            return result;
        }

        private static double ParseDouble(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{fieldName} is required but was empty");

            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                throw new FormatException($"{fieldName} must be a number, got: '{value}'");

            return result;
        }

        private static double? ParseNullableDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;

            return null; // If parsing fails, treat as null rather than throwing
        }

        private static string ParseString(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{fieldName} is required but was empty");

            return value.Trim();
        }

        private static string? ParseNullableString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        #endregion

        /// <summary>
        /// Validates that a CSV file has the correct format
        /// </summary>
        public static async Task<bool> ValidateCsvFormatAsync(string filePath)
        {
            try
            {
                using var reader = new StreamReader(filePath);
                var headerLine = await reader.ReadLineAsync();
                
                if (string.IsNullOrEmpty(headerLine))
                    return false;

                var headers = ParseCsvLine(headerLine);
                var requiredColumns = new[] 
                { 
                    "PassengerId", "Survived", "Pclass", "Name", "Sex", 
                    "Age", "SibSp", "Parch", "Ticket", "Fare", "Cabin", "Embarked" 
                };

                return requiredColumns.All(col => 
                    headers.Any(h => h.Equals(col, StringComparison.OrdinalIgnoreCase)));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets basic statistics about a CSV file
        /// </summary>
        public static async Task<CsvStatistics> GetCsvStatisticsAsync(string filePath)
        {
            var stats = new CsvStatistics();

            using var reader = new StreamReader(filePath);
            
            // Skip header
            await reader.ReadLineAsync();
            
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                    stats.TotalLines++;
            }

            stats.FileSize = new FileInfo(filePath).Length;
            
            return stats;
        }
    }

    /// <summary>
    /// Statistics about a CSV file
    /// </summary>
    public class CsvStatistics
    {
        public int TotalLines { get; set; }
        public long FileSize { get; set; }
        public string FileSizeFormatted => FormatBytes(FileSize);

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}