using EventChainsCore;
using EventChains_CS;
using EventChains_CS.Validation_Events;
using EventChains.Tests.Utilities;

namespace EventChains.Tests.Performance;

/// <summary>
/// Performance and load tests for EventChains validation pipeline - UPDATED FOR LENIENT MODE
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
        var duration = (endTime - startTime).TotalSeconds;

        // Assert
        results.Should().HaveCount(100);
        var throughput = 100 / duration;

        Console.WriteLine($"Batch Processing Results:");
        Console.WriteLine($"  Total time: {duration:F2}s");
        Console.WriteLine($"  Throughput: {throughput:F0} records/second");

        throughput.Should().BeGreaterThan(10, "Should process at least 10 records per second");
    }

    [Test]
    public async Task MemoryUsage_LargeDataset_RemainsStable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var iterations = 1000;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var pipeline = BuildPerformanceTestPipeline();
            var context = pipeline.GetContext();
            var customerData = TestDataFactory.CreateValidCustomer();
            context.Set("customer_data", customerData);
            await pipeline.ExecuteWithResultsAsync();
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = (finalMemory - initialMemory) / 1024.0 / 1024.0;

        // Assert
        Console.WriteLine($"Memory usage after {iterations} iterations:");
        Console.WriteLine($"  Initial: {initialMemory / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"  Final: {finalMemory / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"  Increase: {memoryIncrease:F2} MB");

        memoryIncrease.Should().BeLessThan(100, "Memory increase should be reasonable");
    }

    [Test]
    public async Task ChainResultAggregation_MultipleEvents_MinimalOverhead()
    {
        // Arrange
        var chain = EventChain.Lenient();
        for (int i = 0; i < 50; i++)
        {
            chain.AddEvent(new TestSuccessEvent());
        }

        var iterations = 100;

        // Act
        var startTime = DateTime.UtcNow;
        for (int i = 0; i < iterations; i++)
        {
            await chain.ExecuteWithResultsAsync();
        }
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

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
/// Benchmark tests for comparing different implementation approaches - UPDATED FOR LENIENT MODE
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
        var overheadRatio = eventChainsDuration.TotalMilliseconds / traditionalDuration.TotalMilliseconds;

        Console.WriteLine($"Benchmark: EventChains vs Traditional:");
        Console.WriteLine($"  EventChains: {eventChainsDuration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"  Traditional: {traditionalDuration.TotalMilliseconds:F2}ms");
        Console.WriteLine($"  Overhead: {(overheadRatio - 1) * 100:F1}%");

        // UPDATED: Lenient mode has more overhead due to graduated precision scoring
        // Accept up to 10x overhead as reasonable for the added functionality
        overheadRatio.Should().BeLessThan(10.0,
            "EventChains overhead should be reasonable considering the added functionality");
    }
}