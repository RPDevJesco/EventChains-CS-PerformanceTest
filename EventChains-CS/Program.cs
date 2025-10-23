using System.Diagnostics;
using EventChainsCore;
using EventChains_CS.Validation_Events;
using EventChains_CS.DTOs;
using EventChains_CS.Utils;
using EventChainsCore.Middleware;
using MySqlConnector;

namespace EventChains_CS
{
    /// <summary>
    /// TITANIC DATASET VALIDATION & MySQL BENCHMARK PIPELINE
    /// 
    /// Complete pipeline: CSV → Validation → MySQL → Performance Metrics
    /// </summary>
    class Program
    {
        // MySQL Connection Configuration
        private const string CONNECTION_STRING = "Server=localhost;Port=3306;Database=benchmark_db;Uid=root;Pwd=rootpassword;";
        
        // Performance metrics
        private static int totalProcessed = 0;
        private static int successfulValidations = 0;
        private static int failedValidations = 0;
        private static int successfulInserts = 0;
        private static int failedInserts = 0;
        private static TimeSpan totalValidationTime = TimeSpan.Zero;
        private static TimeSpan totalInsertTime = TimeSpan.Zero;

        static async Task Main(string[] args)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  EventChains - Titanic Dataset MySQL Benchmark");
            Console.WriteLine("  Complete Validation + Database Pipeline");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");

            // Check for options
            bool quietMode = args.Contains("--quiet") || args.Contains("-q");
            bool skipInsert = args.Contains("--no-insert");
            bool recreateTable = args.Contains("--recreate-table");

            if (quietMode)
            {
                Console.WriteLine("Running in QUIET mode");
            }

            if (skipInsert)
            {
                Console.WriteLine("Database INSERT DISABLED (validation only)");
            }

            Console.WriteLine();

            // Load Titanic data
            List<TitanicPassenger> passengers;

            try
            {
                passengers = await TitanicCsvParser.ParseCsvAsync("Titanic.csv");
                Console.WriteLine($"✓ Loaded {passengers.Count} passenger records from Titanic.csv\n");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("⚠ Titanic.csv not found. Using sample data instead.\n");
                passengers = GetSampleTitanicData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Error loading CSV file: {ex.Message}");
                Console.WriteLine("  Using sample data instead.\n");
                passengers = GetSampleTitanicData();
            }

            // Initialize MySQL table
            if (!skipInsert)
            {
                Console.WriteLine("Initializing MySQL database...");
                try
                {
                    await InitializeMySqlTable(recreateTable);
                    Console.WriteLine("✓ Database ready\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Database initialization failed: {ex.Message}");
                    Console.WriteLine("  Continuing with validation only...\n");
                    skipInsert = true;
                }
            }

            // Overall performance tracking
            var overallStopwatch = Stopwatch.StartNew();

            // Process each passenger
            var results = new List<(TitanicPassenger passenger, ChainResult result)>();

            for (int i = 0; i < passengers.Count; i++)
            {
                var passenger = passengers[i];
                var result = await ProcessPassengerData(passenger, $"Passenger {i + 1}/{passengers.Count}", quietMode, skipInsert);
                results.Add((passenger, result));

                if (!quietMode && i < passengers.Count - 1)
                {
                    Console.WriteLine("\n" + new string('─', 70) + "\n");
                }
                else if (quietMode && (i + 1) % 100 == 0)
                {
                    Console.Write($"\rProcessed {i + 1}/{passengers.Count} passengers...");
                }
            }

            overallStopwatch.Stop();

            if (quietMode)
            {
                Console.WriteLine();
            }

            // Print comprehensive performance summary
            PrintPerformanceSummary(results, overallStopwatch.Elapsed);
        }

        static async Task<ChainResult> ProcessPassengerData(
            TitanicPassenger passenger, 
            string recordId, 
            bool quietMode,
            bool skipInsert)
        {
            var validationStopwatch = Stopwatch.StartNew();

            // Configure the validation chain
            var pipeline = EventChain.Lenient()
                .UseMiddleware(TimingMiddleware.Create())
                .AddEvent(new ValidateTitanicRequiredFields())
                .AddEvent(new ValidateTitanicDataRanges())
                .AddEvent(new ValidateTitanicAnnotations())
                .AddEvent(new EnrichTitanicData())
                .AddEvent(new CalculateTitanicRiskScore())
                .AddEvent(new RouteTitanicData());

            // Set up context
            var context = pipeline.GetContext();
            context.Set("passenger_data", passenger);
            context.Set("record_id", recordId);

            // Execute validation
            var result = pipeline.ExecuteWithResultsAsync();

            validationStopwatch.Stop();
            totalValidationTime += validationStopwatch.Elapsed;
            totalProcessed++;

            if (result.Success)
            {
                successfulValidations++;

                // Insert into MySQL if not skipped
                if (!skipInsert)
                {
                    var insertStopwatch = Stopwatch.StartNew();
                    bool insertSuccess = await InsertPassengerToMySQL(passenger, context);
                    insertStopwatch.Stop();
                    totalInsertTime += insertStopwatch.Elapsed;

                    if (insertSuccess)
                    {
                        successfulInserts++;
                    }
                    else
                    {
                        failedInserts++;
                    }
                }
            }
            else
            {
                failedValidations++;
            }

            // Display results if not in quiet mode
            if (!quietMode)
            {
                Console.WriteLine($"Record: {recordId}");
                Console.WriteLine($"Passenger: {passenger.Name}");
                Console.WriteLine($"Status: {(result.Success ? "✓ VALID" : "✗ FAILED")}");
                Console.WriteLine($"Quality Score: {result.TotalPrecisionScore:F1}%");
                Console.WriteLine($"Validation Time: {validationStopwatch.ElapsedMilliseconds}ms");

                if (result.Success && context.ContainsKey("routing_queue"))
                {
                    var queue = context.Get<string>("routing_queue");
                    var survivalRisk = context.Get<int>("survival_risk_score");
                    Console.WriteLine($"Routing: {queue}");
                    Console.WriteLine($"Survival Risk Score: {survivalRisk}/100");
                }

                if (!result.Success)
                {
                    Console.WriteLine($"Errors:");
                    foreach (var evt in result.EventResults.Where(e => !e.Success))
                    {
                        Console.WriteLine($"  - {evt.EventName}: {evt.ErrorMessage}");
                    }
                }
            }

            return result;
        }

        static async Task<bool> InsertPassengerToMySQL(TitanicPassenger passenger, IEventContext context)
        {
            try
            {
                using var connection = new MySqlConnection(CONNECTION_STRING);
                await connection.OpenAsync();

                var survivalRiskScore = context.Get<int>("survival_risk_score");
                var dataQualityScore = context.Get<int>("data_quality_score");
                var routingQueue = context.Get<string>("routing_queue");

                var query = @"
                    INSERT INTO titanic_passengers 
                    (passenger_id, survived, pclass, name, sex, age, sibsp, parch, 
                     ticket, fare, cabin, embarked, family_size, is_alone, 
                     survival_risk_score, data_quality_score, routing_queue)
                    VALUES 
                    (@PassengerId, @Survived, @Pclass, @Name, @Sex, @Age, @SibSp, @Parch,
                     @Ticket, @Fare, @Cabin, @Embarked, @FamilySize, @IsAlone,
                     @SurvivalRiskScore, @DataQualityScore, @RoutingQueue)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@PassengerId", passenger.PassengerId);
                command.Parameters.AddWithValue("@Survived", passenger.Survived);
                command.Parameters.AddWithValue("@Pclass", passenger.Pclass);
                command.Parameters.AddWithValue("@Name", passenger.Name);
                command.Parameters.AddWithValue("@Sex", passenger.Sex);
                command.Parameters.AddWithValue("@Age", passenger.Age.HasValue ? passenger.Age.Value : DBNull.Value);
                command.Parameters.AddWithValue("@SibSp", passenger.SibSp);
                command.Parameters.AddWithValue("@Parch", passenger.Parch);
                command.Parameters.AddWithValue("@Ticket", passenger.Ticket);
                command.Parameters.AddWithValue("@Fare", passenger.Fare);
                command.Parameters.AddWithValue("@Cabin", string.IsNullOrEmpty(passenger.Cabin) ? DBNull.Value : passenger.Cabin);
                command.Parameters.AddWithValue("@Embarked", passenger.Embarked);
                command.Parameters.AddWithValue("@FamilySize", passenger.FamilySize);
                command.Parameters.AddWithValue("@IsAlone", passenger.IsAlone);
                command.Parameters.AddWithValue("@SurvivalRiskScore", survivalRiskScore);
                command.Parameters.AddWithValue("@DataQualityScore", dataQualityScore);
                command.Parameters.AddWithValue("@RoutingQueue", routingQueue);

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database insert error: {ex.Message}");
                return false;
            }
        }

        static async Task InitializeMySqlTable(bool recreate = false)
        {
            using var connection = new MySqlConnection(CONNECTION_STRING);
            await connection.OpenAsync();

            // Check if table exists
            var checkQuery = @"
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_schema = 'benchmark_db' 
                AND table_name = 'titanic_passengers'";

            using (var checkCommand = new MySqlCommand(checkQuery, connection))
            {
                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                if (exists && !recreate)
                {
                    Console.WriteLine("  Table 'titanic_passengers' already exists (use --recreate-table to drop and recreate)");
                    return;
                }

                if (exists && recreate)
                {
                    Console.WriteLine("  Dropping existing table...");
                    var dropQuery = "DROP TABLE titanic_passengers";
                    using var dropCommand = new MySqlCommand(dropQuery, connection);
                    await dropCommand.ExecuteNonQueryAsync();
                }
            }

            // Create table
            Console.WriteLine("  Creating table 'titanic_passengers'...");
            var createQuery = @"
                CREATE TABLE titanic_passengers (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    passenger_id INT NOT NULL,
                    survived INT NOT NULL,
                    pclass INT NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    sex VARCHAR(10) NOT NULL,
                    age DECIMAL(5,2) NULL,
                    sibsp INT NOT NULL,
                    parch INT NOT NULL,
                    ticket VARCHAR(50) NOT NULL,
                    fare DECIMAL(10,4) NOT NULL,
                    cabin VARCHAR(50) NULL,
                    embarked VARCHAR(1) NOT NULL,
                    family_size INT NOT NULL,
                    is_alone BOOLEAN NOT NULL,
                    survival_risk_score INT NOT NULL,
                    data_quality_score INT NOT NULL,
                    routing_queue VARCHAR(50) NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    INDEX idx_passenger_id (passenger_id),
                    INDEX idx_survived (survived),
                    INDEX idx_pclass (pclass),
                    INDEX idx_quality (data_quality_score),
                    INDEX idx_risk (survival_risk_score),
                    INDEX idx_routing (routing_queue)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";

            using (var createCommand = new MySqlCommand(createQuery, connection))
            {
                await createCommand.ExecuteNonQueryAsync();
            }

            Console.WriteLine("  ✓ Table created successfully");
        }

        static void PrintPerformanceSummary(List<(TitanicPassenger passenger, ChainResult result)> results, TimeSpan totalTime)
        {
            Console.WriteLine("\n" + new string('═', 70));
            Console.WriteLine("  PERFORMANCE SUMMARY & BOTTLENECK ANALYSIS");
            Console.WriteLine(new string('═', 70) + "\n");

            // Overall metrics
            Console.WriteLine($"Total Records Processed:    {totalProcessed}");
            Console.WriteLine($"Total Execution Time:       {totalTime.TotalSeconds:F2}s");
            Console.WriteLine($"Overall Throughput:         {totalProcessed / totalTime.TotalSeconds:F2} records/sec\n");

            // Validation metrics
            Console.WriteLine("VALIDATION METRICS:");
            Console.WriteLine($"  Successful Validations:   {successfulValidations} ({100.0 * successfulValidations / totalProcessed:F1}%)");
            Console.WriteLine($"  Failed Validations:       {failedValidations} ({100.0 * failedValidations / totalProcessed:F1}%)");
            Console.WriteLine($"  Total Validation Time:    {totalValidationTime.TotalSeconds:F2}s");
            Console.WriteLine($"  Avg Validation Time:      {totalValidationTime.TotalMilliseconds / totalProcessed:F2}ms/record");
            Console.WriteLine($"  Validation Throughput:    {totalProcessed / totalValidationTime.TotalSeconds:F2} records/sec\n");

            // Database metrics
            if (successfulInserts + failedInserts > 0)
            {
                Console.WriteLine("DATABASE INSERT METRICS:");
                Console.WriteLine($"  Successful Inserts:       {successfulInserts} ({100.0 * successfulInserts / (successfulInserts + failedInserts):F1}%)");
                Console.WriteLine($"  Failed Inserts:           {failedInserts} ({100.0 * failedInserts / (successfulInserts + failedInserts):F1}%)");
                Console.WriteLine($"  Total Insert Time:        {totalInsertTime.TotalSeconds:F2}s");
                Console.WriteLine($"  Avg Insert Time:          {totalInsertTime.TotalMilliseconds / (successfulInserts + failedInserts):F2}ms/record");
                Console.WriteLine($"  Insert Throughput:        {(successfulInserts + failedInserts) / totalInsertTime.TotalSeconds:F2} records/sec\n");
            }

            // Time breakdown
            if (successfulInserts + failedInserts > 0)
            {
                Console.WriteLine("TIME BREAKDOWN:");
                var validationPercent = 100.0 * totalValidationTime.TotalSeconds / totalTime.TotalSeconds;
                var insertPercent = 100.0 * totalInsertTime.TotalSeconds / totalTime.TotalSeconds;
                var otherPercent = 100.0 - validationPercent - insertPercent;
                
                Console.WriteLine($"  Validation:               {totalValidationTime.TotalSeconds:F2}s ({validationPercent:F1}%)");
                Console.WriteLine($"  Database Inserts:         {totalInsertTime.TotalSeconds:F2}s ({insertPercent:F1}%)");
                Console.WriteLine($"  Other (I/O, overhead):    {(totalTime - totalValidationTime - totalInsertTime).TotalSeconds:F2}s ({otherPercent:F1}%)\n");

                // Bottleneck identification
                Console.WriteLine("BOTTLENECK ANALYSIS:");
                if (insertPercent > validationPercent * 2)
                {
                    Console.WriteLine("  ⚠ PRIMARY BOTTLENECK: Database Inserts");
                    Console.WriteLine("    Recommendations:");
                    Console.WriteLine("    - Use batch inserts instead of individual inserts");
                    Console.WriteLine("    - Consider using LOAD DATA INFILE for bulk operations");
                    Console.WriteLine("    - Add connection pooling");
                    Console.WriteLine("    - Tune MySQL buffer pool size");
                }
                else if (validationPercent > insertPercent * 2)
                {
                    Console.WriteLine("  ⚠ PRIMARY BOTTLENECK: Validation Logic");
                    Console.WriteLine("    Recommendations:");
                    Console.WriteLine("    - Optimize validation rules");
                    Console.WriteLine("    - Consider parallel validation");
                    Console.WriteLine("    - Cache validation results");
                }
                else
                {
                    Console.WriteLine("  ✓ Balanced performance - no significant bottlenecks detected");
                }
                Console.WriteLine();
            }

            // Quality distribution
            var qualityScores = results.Select(r => r.result.TotalPrecisionScore).ToList();
            if (qualityScores.Any())
            {
                Console.WriteLine("DATA QUALITY DISTRIBUTION:");
                Console.WriteLine($"  Average Quality Score:    {qualityScores.Average():F2}%");
                Console.WriteLine($"  High Quality (90-100%):   {qualityScores.Count(s => s >= 90)} records");
                Console.WriteLine($"  Medium Quality (70-89%):  {qualityScores.Count(s => s >= 70 && s < 90)} records");
                Console.WriteLine($"  Low Quality (<70%):       {qualityScores.Count(s => s < 70)} records\n");
            }

            Console.WriteLine(new string('═', 70));
        }

        static List<TitanicPassenger> GetSampleTitanicData()
        {
            return new List<TitanicPassenger>
            {
                new TitanicPassenger
                {
                    PassengerId = 1,
                    Survived = 0,
                    Pclass = 3,
                    Name = "Braund, Mr. Owen Harris",
                    Sex = "male",
                    Age = 22,
                    SibSp = 1,
                    Parch = 0,
                    Ticket = "A/5 21171",
                    Fare = 7.25,
                    Cabin = null,
                    Embarked = "S"
                },
                new TitanicPassenger
                {
                    PassengerId = 2,
                    Survived = 1,
                    Pclass = 1,
                    Name = "Cumings, Mrs. John Bradley (Florence Briggs Thayer)",
                    Sex = "female",
                    Age = 38,
                    SibSp = 1,
                    Parch = 0,
                    Ticket = "PC 17599",
                    Fare = 71.2833,
                    Cabin = "C85",
                    Embarked = "C"
                },
                new TitanicPassenger
                {
                    PassengerId = 3,
                    Survived = 1,
                    Pclass = 3,
                    Name = "Heikkinen, Miss. Laina",
                    Sex = "female",
                    Age = 26,
                    SibSp = 0,
                    Parch = 0,
                    Ticket = "STON/O2. 3101282",
                    Fare = 7.925,
                    Cabin = null,
                    Embarked = "S"
                },
                new TitanicPassenger
                {
                    PassengerId = 4,
                    Survived = 1,
                    Pclass = 1,
                    Name = "Futrelle, Mrs. Jacques Heath (Lily May Peel)",
                    Sex = "female",
                    Age = 35,
                    SibSp = 1,
                    Parch = 0,
                    Ticket = "113803",
                    Fare = 53.1,
                    Cabin = "C123",
                    Embarked = "S"
                },
                new TitanicPassenger
                {
                    PassengerId = 5,
                    Survived = 0,
                    Pclass = 3,
                    Name = "Allen, Mr. William Henry",
                    Sex = "male",
                    Age = 35,
                    SibSp = 0,
                    Parch = 0,
                    Ticket = "373450",
                    Fare = 8.05,
                    Cabin = null,
                    Embarked = "S"
                }
            };
        }
    }
}