using System.Text.Json;
using EventChainsCore;
using EventChains_CS.Validation_Events;

namespace EventChains_CS
{
    /// <summary>
    /// REAL-WORLD PROBLEM: Multi-Stage Customer Data Validation & Enrichment Pipeline
    /// 
    /// SCENARIO: You receive customer data from various sources (API, CSV imports, web forms, Mockaroo).
    /// Each record needs validation, enrichment, and routing based on data quality.
    /// 
    /// REQUIREMENTS:
    /// 1. Validate required fields (strict - must pass)
    /// 2. Validate optional fields (lenient - can fail)
    /// 3. Enrich with external data (can fail, affects quality score)
    /// 4. Calculate risk score (depends on validation results)
    /// 5. Route to appropriate processing queue based on quality
    /// 6. Generate detailed audit trail with quality metrics
    /// 
    /// TRADITIONAL CODE PROBLEMS:
    /// - 500+ lines of nested try-catch blocks
    /// - Manual quality score tracking across methods
    /// - Complex branching logic for routing decisions
    /// - Tight coupling between validation stages
    /// - Difficult to test individual validators
    /// - Hard to add/remove validation steps
    /// - Inconsistent error handling
    /// 
    /// EVENTCHAINS SOLUTION:
    /// - 150 lines with clean separation
    /// - Automatic quality score aggregation
    /// - Each validator independently testable
    /// - Easy to modify pipeline
    /// - Consistent graduated precision scoring
    /// - Built-in audit trail
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  EventChains - Enterprise Data Validation Example");
            Console.WriteLine("  (Loads data from Mockaroo JSON file)");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");

            // Check for quiet mode and DNS flag
            bool quietMode = args.Contains("--quiet") || args.Contains("-q");
            bool skipDns = args.Contains("--no-dns");

            // Load customer data from JSON file
            List<CustomerData> customers;

            try
            {
                customers = await LoadCustomerDataFromFile("customers.json");
                Console.WriteLine($"✓ Loaded {customers.Count} customer records from customers.json\n");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("⚠ customers.json not found. Using sample data instead.\n");
                Console.WriteLine("  To use Mockaroo data:");
                Console.WriteLine("  1. Go to https://mockaroo.com");
                Console.WriteLine("  2. Create fields matching CustomerData structure");
                Console.WriteLine("  3. Generate JSON and save as customers.json\n");
                customers = GetSampleCustomerData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Error loading JSON file: {ex.Message}");
                Console.WriteLine("  Using sample data instead.\n");
                customers = GetSampleCustomerData();
            }

            if (quietMode)
            {
                Console.WriteLine("Running in QUIET mode (use --verbose for detailed output)");
            }
            else
            {
                Console.WriteLine("Tip: Use --quiet or -q flag for faster processing without per-record output");
            }

            if (skipDns)
            {
                Console.WriteLine("DNS validation DISABLED (use without --no-dns to enable)");
                ValidateEmailFormat.SkipDnsLookup = true;
            }
            else
            {
                Console.WriteLine("Tip: Use --no-dns flag to skip DNS lookups for 10x faster email validation");
            }

            Console.WriteLine();

            // Process each customer through the validation pipeline
            var results = new List<(CustomerData customer, ChainResult result)>();

            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < customers.Count; i++)
            {
                var customer = customers[i];
                var result = await ProcessCustomerData(customer, $"Record {i + 1}/{customers.Count}", quietMode);
                results.Add((customer, result));

                if (!quietMode && i < customers.Count - 1)
                {
                    Console.WriteLine("\n" + new string('─', 55) + "\n");
                }
                else if (quietMode && (i + 1) % 100 == 0)
                {
                    // Progress indicator in quiet mode
                    Console.Write($"\rProcessed {i + 1}/{customers.Count} records...");
                }
            }

            if (quietMode)
            {
                Console.WriteLine(); // New line after progress indicator
            }

            totalStopwatch.Stop();

            // Summary statistics
            Console.WriteLine("\n" + new string('═', 55));
            Console.WriteLine("SUMMARY STATISTICS");
            Console.WriteLine(new string('═', 55) + "\n");

            PrintSummaryStatistics(results, totalStopwatch.ElapsedMilliseconds);

            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task<List<CustomerData>> LoadCustomerDataFromFile(string filename)
        {
            var json = await File.ReadAllTextAsync(filename);
            var customers = JsonSerializer.Deserialize<List<CustomerData>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return customers ?? new List<CustomerData>();
        }

        static List<CustomerData> GetSampleCustomerData()
        {
            return new List<CustomerData>
            {
                // High quality data
                new CustomerData
                {
                    Email = "john.doe@company.com",
                    Phone = "+1 202-555-0123",
                    FirstName = "John",
                    LastName = "Doe",
                    Age = 35,
                    Country = "US",
                    CompanyName = "Acme Corp",
                    Revenue = 1_000_000
                },
                // Medium quality - missing optional fields
                new CustomerData
                {
                    Email = "jane@example.com",
                    Phone = "555-0199",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Age = 28,
                    Country = "US"
                },
                // Low quality - barely passes required validations
                new CustomerData
                {
                    Email = "test@test.com",
                    Phone = "invalid",
                    FirstName = "T",
                    LastName = "User",
                    Age = 150,
                    Country = "XX"
                }
            };
        }

        static void PrintSummaryStatistics(List<(CustomerData customer, ChainResult result)> results, long totalMilliseconds)
        {
            var totalRecords = results.Count;
            var premiumQueue = results.Count(r => r.result.TotalPrecisionScore >= 90);
            var standardQueue = results.Count(r => r.result.TotalPrecisionScore >= 70 && r.result.TotalPrecisionScore < 90);
            var manualReview = results.Count(r => r.result.TotalPrecisionScore >= 50 && r.result.TotalPrecisionScore < 70);
            var quarantine = results.Count(r => r.result.TotalPrecisionScore < 50);

            var avgQuality = results.Average(r => r.result.TotalPrecisionScore);
            var avgValidationsPassed = results.Average(r => r.result.SuccessCount);
            var avgValidationsTotal = results.Average(r => r.result.TotalCount);

            Console.WriteLine($"Total Records Processed: {totalRecords}");
            Console.WriteLine($"Average Quality Score: {avgQuality:F1}%");
            Console.WriteLine($"Average Validations Passed: {avgValidationsPassed:F1}/{avgValidationsTotal:F1}");
            Console.WriteLine();

            // Performance metrics
            Console.WriteLine("Performance Metrics:");
            Console.WriteLine($"  Total Processing Time: {totalMilliseconds:N0}ms ({totalMilliseconds / 1000.0:F2}s)");
            Console.WriteLine($"  Average Time Per Record: {totalMilliseconds / (double)totalRecords:F2}ms");
            Console.WriteLine($"  Throughput: {totalRecords * 1000.0 / totalMilliseconds:F1} records/second");
            Console.WriteLine();

            Console.WriteLine("Routing Distribution:");
            Console.WriteLine($"  Premium Queue (≥90%):        {premiumQueue,3} ({(premiumQueue * 100.0 / totalRecords):F1}%)");
            Console.WriteLine($"  Standard Queue (70-89%):     {standardQueue,3} ({(standardQueue * 100.0 / totalRecords):F1}%)");
            Console.WriteLine($"  Manual Review (50-69%):      {manualReview,3} ({(manualReview * 100.0 / totalRecords):F1}%)");
            Console.WriteLine($"  Quarantine (<50%):           {quarantine,3} ({(quarantine * 100.0 / totalRecords):F1}%)");
            Console.WriteLine();

            Console.WriteLine("Grade Distribution:");
            var gradeGroups = results.GroupBy(r => r.result.GetGrade()).OrderByDescending(g => g.Key);
            foreach (var grade in gradeGroups)
            {
                var count = grade.Count();
                Console.WriteLine($"  Grade {grade.Key}: {count,3} ({(count * 100.0 / totalRecords):F1}%)");
            }
            Console.WriteLine();

            Console.WriteLine("Top Validation Issues:");
            var allFailures = results
                .SelectMany(r => r.result.EventResults.Where(e => !e.Success))
                .GroupBy(e => e.EventName)
                .OrderByDescending(g => g.Count())
                .Take(5);

            foreach (var issue in allFailures)
            {
                Console.WriteLine($"  {issue.Key}: {issue.Count()} failures ({(issue.Count() * 100.0 / totalRecords):F1}%)");
            }

            Console.WriteLine();
            Console.WriteLine("Quality Score Ranges:");
            var ranges = new[] { (0, 50), (50, 60), (60, 70), (70, 80), (80, 90), (90, 101) };
            foreach (var (min, max) in ranges)
            {
                var count = results.Count(r => r.result.TotalPrecisionScore >= min && r.result.TotalPrecisionScore < max);
                var bar = new string('█', Math.Min(count * 50 / totalRecords, 50));
                Console.WriteLine($"  {min,3}-{max - 1,3}%: {bar} {count}");
            }
        }

        static async Task<ChainResult> ProcessCustomerData(CustomerData data, string label, bool quietMode = false)
        {
            if (!quietMode)
            {
                Console.WriteLine($"Processing: {label}");
                Console.WriteLine($"  Email: {data.Email}");
                Console.WriteLine($"  Phone: {data.Phone ?? "N/A"}");
                Console.WriteLine($"  Name: {data.FirstName} {data.LastName}");
                Console.WriteLine();
            }

            // THIS IS THE MAGIC: One pipeline handles everything
            var pipeline = BuildValidationPipeline();

            var context = pipeline.GetContext();
            context.Set("customer_data", data);

            var result = await pipeline.ExecuteWithResultsAsync();

            if (!quietMode)
            {
                // Results automatically aggregated
                Console.WriteLine($"  Overall Quality: {result.TotalPrecisionScore:F1}%");
                Console.WriteLine($"  Grade: {result.GetGrade()}");
                Console.WriteLine($"  Validations Passed: {result.SuccessCount}/{result.TotalCount}");

                // Routing decision based on quality
                var routing = DetermineRouting(result.TotalPrecisionScore);
                Console.WriteLine($"  Routing: {routing}");

                // Audit trail automatically built
                Console.WriteLine($"\n  Validation Details:");
                foreach (var eventResult in result.EventResults)
                {
                    var status = eventResult.Success ? "✓" : "✗";
                    Console.WriteLine($"    {status} {eventResult.EventName}: {eventResult.PrecisionScore:F0}%");
                    if (!eventResult.Success)
                    {
                        Console.WriteLine($"      Error: {eventResult.ErrorMessage}");
                    }
                }
            }

            return result;
        }

        static EventChain BuildValidationPipeline()
        {
            // LENIENT mode: Some validations can fail, but we collect all results
            var pipeline = EventChain.Lenient();

            // Critical validations (must pass)
            pipeline.AddEvent(new ValidateRequiredFields());
            pipeline.AddEvent(new ValidateEmailFormat());

            // Important validations (failures lower quality score)
            pipeline.AddEvent(new ValidatePhoneFormat());
            pipeline.AddEvent(new ValidateBusinessData());

            // Optional enrichments (best effort)
            pipeline.AddEvent(new EnrichWithGeolocation());
            pipeline.AddEvent(new EnrichWithCreditScore());

            // Risk assessment (depends on all previous validations)
            pipeline.AddEvent(new CalculateRiskScore());

            return pipeline;
        }

        static string DetermineRouting(double qualityScore)
        {
            return qualityScore switch
            {
                >= 90 => "Premium Queue (auto-approve)",
                >= 70 => "Standard Queue (standard review)",
                >= 50 => "Manual Review Queue (requires human approval)",
                _ => "Quarantine Queue (needs extensive review)"
            };
        }
    }
}