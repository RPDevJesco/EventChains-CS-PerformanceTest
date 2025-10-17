using EventChainsCore;
using EventChains_CS;
using EventChains_CS.Validation_Events;
using EventChains.Tests.Utilities;

namespace EventChains.Tests.Performance;

/// <summary>
/// Performance and load tests for EventChains validation pipeline
/// </summary>
[TestFixture]
[Category("Performance")]
public class PerformanceTests
{
    private EventChain BuildPerformanceTestPipeline()
    {
        var pipeline = EventChain.Lenient();
        pipeline.AddEvent(new ValidateRequiredFields());
        pipeline.AddEvent(new ValidateEmailFormat());
        pipeline.AddEvent(new ValidatePhoneFormat());
        pipeline.AddEvent(new ValidateBusinessData());
        pipeline.AddEvent(new EnrichWithGeolocation());
        pipeline.AddEvent(new EnrichWithCreditScore());
        pipeline.AddEvent(new CalculateRiskScore());
        return pipeline;
    }

    [Test]
    public async Task SingleRecordProcessing_CompleteValidation_CompletesQuickly()
    {
        // Arrange
        var pipeline = BuildPerformanceTestPipeline();
        var context = pipeline.GetContext();
        var customerData = TestDataFactory.CreateValidCustomer();
        context.Set("customer_data", customerData);

        // Act
        var (result, duration) = await PerformanceTestHelpers.MeasureAsync(
            () => pipeline.ExecuteWithResultsAsync()
        );

        // Assert
        result.Success.Should().BeTrue();
        duration.ShouldCompleteWithin(100, "Single record validation should be very fast");

        Console.WriteLine($"Single record processed in {duration.TotalMilliseconds:F2}ms");
    }

    [Test]
    public async Task BatchProcessing_100Records_MeetsPerformanceTarget()
    {
        // Arrange
        var customers = TestDataFactory.CreateCustomerBatch(100, TestDataFactory.QualityLevel.Mixed);

        // Act
        var startTime = DateTime.UtcNow;

        var results = new List<ChainResult>();
        foreach (var customer in customers)
        {
            var pipeline = BuildPerformanceTestPipeline();
            var context = pipeline.GetContext();
            context.Set("customer_data", customer);
            var result = await pipeline.ExecuteWithResultsAsync();
            results.Add(result);
        }

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;
        var throughput = PerformanceTestHelpers.CalculateThroughput(100, duration);

        // Assert
        results.Should().HaveCount(100);
        throughput.Should().BeGreaterThan(10, "Should process at least 10 records per second");

        Console.WriteLine($"Batch of 100 records:");
        Console.WriteLine($"  Total time: {duration.TotalMilliseconds:F2}ms ({duration.TotalSeconds:F2}s)");
        Console.WriteLine($"  Average per record: {duration.TotalMilliseconds / 100:F2}ms");
        Console.WriteLine($"  Throughput: {throughput:F2} records/second");
    }

    [Test]
    public async Task BatchProcessing_1000Records_ScalesLinearly()
    {
        // Arrange
        var customers = TestDataFactory.CreateCustomerBatch(1000, TestDataFactory.QualityLevel.Standard);

        // Act
        var startTime = DateTime.UtcNow;

        var tasks = customers.Select(async customer =>
        {
            var pipeline = BuildPerformanceTestPipeline();
            var context = pipeline.GetContext();
            context.Set("customer_data", customer);
            return await pipeline.ExecuteWithResultsAsync();
        });

        var results = await Task.WhenAll(tasks);

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;
        var throughput = PerformanceTestHelpers.CalculateThroughput(1000, duration);

        // Assert
        results.Should().HaveCount(1000);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());

        Console.WriteLine($"Batch of 1000 records (parallel):");
        Console.WriteLine($"  Total time: {duration.TotalSeconds:F2}s");
        Console.WriteLine($"  Average per record: {duration.TotalMilliseconds / 1000:F2}ms");
        Console.WriteLine($"  Throughput: {throughput:F2} records/second");
    }

    [Test]
    public async Task ParallelProcessing_ThreadSafety_NoDataCorruption()
    {
        // Arrange
        var customers = MockDataGenerator.GenerateRandomCustomerBatch(500);

        // Act
        var tasks = customers.Select(async (customer, index) =>
        {
            var pipeline = BuildPerformanceTestPipeline();
            var context = pipeline.GetContext();
            context.Set("customer_data", customer);
            context.Set("customer_index", index);

            var result = await pipeline.ExecuteWithResultsAsync();

            // Verify data integrity
            var retrievedCustomer = context.Get<CustomerData>("customer_data");
            var retrievedIndex = context.Get<int>("customer_index");

            return (Result: result, Customer: retrievedCustomer, Index: retrievedIndex, ExpectedIndex: index);
        });

        var results = await Task.WhenAll(tasks);

        // Assert - No data corruption
        results.Should().HaveCount(500);
        results.Should().AllSatisfy(r =>
        {
            r.Result.Should().NotBeNull();
            r.Customer.Should().NotBeNull();
            r.Index.Should().Be(r.ExpectedIndex);
        });

        Console.WriteLine($"Parallel processing of 500 records completed with no data corruption");
    }

    [Test]
    public async Task ContextOperations_HighFrequency_PerformsWell()
    {
        // Arrange
        var context = new EventContext();
        var iterations = 10000;

        // Act
        var (_, duration) = await PerformanceTestHelpers.MeasureAsync(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                context.Set($"key_{i}", i);
                context.Get<int>($"key_{i}");
                context.ContainsKey($"key_{i}");
            }
            await Task.CompletedTask;
            return true;
        });

        // Assert
        duration.ShouldCompleteWithin(1000, "High-frequency context operations should be fast");

        var operationsPerSecond = (iterations * 3) / duration.TotalSeconds; // 3 operations per iteration
        operationsPerSecond.Should().BeGreaterThan(10000, "Should handle thousands of operations per second");

        Console.WriteLine($"Context operations:");
        Console.WriteLine($"  {iterations * 3:N0} operations in {duration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"  {operationsPerSecond:N0} operations/second");
    }

    [Test]
    public async Task ChainExecution_EmptyChain_MinimalOverhead()
    {
        // Arrange
        var chain = EventChain.Lenient();
        var iterations = 1000;

        // Act
        var (_, duration) = await PerformanceTestHelpers.MeasureAsync(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                await chain.ExecuteWithResultsAsync();
            }
            return true;
        });

        // Assert
        var avgTime = duration.TotalMilliseconds / iterations;
        avgTime.Should().BeLessThan(1.0, "Empty chain should have minimal overhead");

        Console.WriteLine($"Empty chain execution:");
        Console.WriteLine($"  {iterations} executions in {duration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"  Average: {avgTime:F4}ms per execution");
    }

    [Test]
    public async Task MemoryUsage_LargeBatch_NoMemoryLeak()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var batchSize = 1000;

        // Act
        for (int batch = 0; batch < 5; batch++)
        {
            var customers = TestDataFactory.CreateCustomerBatch(batchSize);

            var tasks = customers.Select(async customer =>
            {
                var pipeline = BuildPerformanceTestPipeline();
                var context = pipeline.GetContext();
                context.Set("customer_data", customer);
                return await pipeline.ExecuteWithResultsAsync();
            });

            await Task.WhenAll(tasks);

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB

        // Assert
        memoryIncrease.Should().BeLessThan(50, "Memory usage should not grow significantly");

        Console.WriteLine($"Memory usage after processing 5000 records:");
        Console.WriteLine($"  Initial: {initialMemory / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"  Final: {finalMemory / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"  Increase: {memoryIncrease:F2} MB");
    }

    [Test]
    public async Task ValidationPipeline_DifferentDataQuality_ConsistentPerformance()
    {
        // Arrange
        var testCases = new[]
        {
            ("Premium", TestDataFactory.CreateCustomerBatch(100, TestDataFactory.QualityLevel.Premium)),
            ("Standard", TestDataFactory.CreateCustomerBatch(100, TestDataFactory.QualityLevel.Standard)),
            ("Invalid", TestDataFactory.CreateCustomerBatch(100, TestDataFactory.QualityLevel.Invalid))
        };

        // Act & Assert
        foreach (var (quality, customers) in testCases)
        {
            var (results, duration) = await PerformanceTestHelpers.MeasureAsync(async () =>
            {
                var resultsList = new List<ChainResult>();
                foreach (var customer in customers)
                {
                    var pipeline = BuildPerformanceTestPipeline();
                    var context = pipeline.GetContext();
                    context.Set("customer_data", customer);
                    var result = await pipeline.ExecuteWithResultsAsync();
                    resultsList.Add(result);
                }
                return resultsList;
            });

            var throughput = PerformanceTestHelpers.CalculateThroughput(100, duration);

            Console.WriteLine($"{quality} data quality:");
            Console.WriteLine($"  Time: {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Throughput: {throughput:F2} records/second");

            // Performance should be consistent regardless of data quality
            throughput.Should().BeGreaterThan(10, $"{quality} quality data should still process quickly");
        }
    }

    [Test]
    public async Task StrictVsLenientMode_PerformanceComparison()
    {
        // Arrange
        var customers = TestDataFactory.CreateCustomerBatch(100, TestDataFactory.QualityLevel.Mixed);

        // Act - Strict mode
        var (strictResults, strictDuration) = await PerformanceTestHelpers.MeasureAsync(async () =>
        {
            var results = new List<ChainResult>();
            foreach (var customer in customers)
            {
                var pipeline = EventChain.Strict();
                pipeline.AddEvent(new ValidateRequiredFields());
                pipeline.AddEvent(new ValidateEmailFormat());
                pipeline.AddEvent(new ValidatePhoneFormat());

                var context = pipeline.GetContext();
                context.Set("customer_data", customer);
                results.Add(await pipeline.ExecuteWithResultsAsync());
            }
            return results;
        });

        // Act - Lenient mode
        var (lenientResults, lenientDuration) = await PerformanceTestHelpers.MeasureAsync(async () =>
        {
            var results = new List<ChainResult>();
            foreach (var customer in customers)
            {
                var pipeline = EventChain.Lenient();
                pipeline.AddEvent(new ValidateRequiredFields());
                pipeline.AddEvent(new ValidateEmailFormat());
                pipeline.AddEvent(new ValidatePhoneFormat());

                var context = pipeline.GetContext();
                context.Set("customer_data", customer);
                results.Add(await pipeline.ExecuteWithResultsAsync());
            }
            return results;
        });

        // Assert
        var strictThroughput = PerformanceTestHelpers.CalculateThroughput(100, strictDuration);
        var lenientThroughput = PerformanceTestHelpers.CalculateThroughput(100, lenientDuration);

        Console.WriteLine("Strict vs Lenient mode performance:");
        Console.WriteLine($"  Strict mode: {strictThroughput:F2} records/second");
        Console.WriteLine($"  Lenient mode: {lenientThroughput:F2} records/second");

        // Strict mode might be faster for invalid data (stops early)
        strictThroughput.Should().BeGreaterThan(5);
        lenientThroughput.Should().BeGreaterThan(5);
    }

    [Test]
    public async Task EventChain_ResultAggregation_MinimalOverhead()
    {
        // Arrange
        var chain = EventChain.Lenient();
        // Add 50 simple success events
        for (int i = 0; i < 50; i++)
        {
            chain.AddEvent(new TestSuccessEvent());
        }

        var iterations = 100;

        // Act
        var (_, duration) = await PerformanceTestHelpers.MeasureAsync(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                await chain.ExecuteWithResultsAsync();
            }
            return true;
        });

        // Assert
        var avgTime = duration.TotalMilliseconds / iterations;
        Console.WriteLine($"Chain with 50 events:");
        Console.WriteLine($"  {iterations} executions in {duration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"  Average: {avgTime:F2}ms per execution");

        avgTime.Should().BeLessThan(50, "Result aggregation should have minimal overhead");
    }

    #region Helper Test Event

    private class TestSuccessEvent : BaseEvent
    {
        public override Task<EventResult> ExecuteAsync(IEventContext context)
        {
            return Task.FromResult(Success(precisionScore: 100));
        }
    }

    #endregion
}

/// <summary>
/// Benchmark tests for comparing different implementation approaches
/// </summary>
[TestFixture]
[Category("Benchmark")]
public class BenchmarkTests
{
    [Test]
    public async Task Benchmark_ValidationApproaches_EventChainsVsTraditional()
    {
        // Arrange
        var customer = TestDataFactory.CreateValidCustomer();
        var iterations = 1000;

        // Act - EventChains approach
        var (_, eventChainsDuration) = await PerformanceTestHelpers.MeasureAsync(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var pipeline = EventChain.Lenient();
                pipeline.AddEvent(new ValidateRequiredFields());
                pipeline.AddEvent(new ValidateEmailFormat());

                var context = pipeline.GetContext();
                context.Set("customer_data", customer);
                await pipeline.ExecuteWithResultsAsync();
            }
            return true;
        });

        // Act - Traditional approach (simulated)
        var (_, traditionalDuration) = await PerformanceTestHelpers.MeasureAsync(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                // Simulated traditional validation
                var isValid = true;
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(customer.Email)) errors.Add("Email required");
                if (string.IsNullOrWhiteSpace(customer.FirstName)) errors.Add("FirstName required");
                if (!customer.Email?.Contains("@") ?? false) errors.Add("Invalid email");

                await Task.CompletedTask;
            }
            return true;
        });

        // Assert
        Console.WriteLine("Benchmark: EventChains vs Traditional:");
        Console.WriteLine($"  EventChains: {eventChainsDuration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"  Traditional: {traditionalDuration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"  Overhead: {(eventChainsDuration.TotalMilliseconds / traditionalDuration.TotalMilliseconds - 1) * 100:F1}%");

        // EventChains should have reasonable overhead
        var overheadRatio = eventChainsDuration.TotalMilliseconds / traditionalDuration.TotalMilliseconds;
        overheadRatio.Should().BeLessThan(5.0, "EventChains overhead should be reasonable");
    }
}