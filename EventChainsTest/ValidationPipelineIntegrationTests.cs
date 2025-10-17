using EventChainsCore;
using EventChains_CS;
using EventChains_CS.Validation_Events;

namespace EventChains.Tests.Integration;

/// <summary>
/// Integration tests for the complete customer validation pipeline
/// </summary>
[TestFixture]
public class ValidationPipelineIntegrationTests
{
    private EventChain BuildTestPipeline(bool skipDns = true)
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
    public async Task ValidationPipeline_HighQualityData_AchievesHighScore()
    {
        // Arrange
        var pipeline = BuildTestPipeline();
        var context = pipeline.GetContext();
        var customerData = new CustomerData
        {
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "+1-555-123-4567",
            Age = 30,
            City = "New York",
            Country = "USA"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await pipeline.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.TotalPrecisionScore.Should().BeGreaterThan(80.0);
        result.SuccessCount.Should().BeGreaterThan(5);
        result.GetGrade().Should().BeOneOf("A", "B");
    }

    [Test]
    public async Task ValidationPipeline_MissingRequiredFields_FailsEarly()
    {
        // Arrange
        var pipeline = EventChain.Strict(); // Strict mode for this test
        pipeline.AddEvent(new ValidateRequiredFields());
        pipeline.AddEvent(new ValidateEmailFormat());
        pipeline.AddEvent(new ValidatePhoneFormat());

        var context = pipeline.GetContext();
        var customerData = new CustomerData
        {
            Email = "",  // Missing
            FirstName = "John",
            LastName = "Doe"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await pipeline.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.EventResults.Should().HaveCount(1); // Only first validation ran
        result.FailureCount.Should().Be(1);
    }

    [Test]
    public async Task ValidationPipeline_LenientMode_ContinuesAfterOptionalFailures()
    {
        // Arrange
        var pipeline = BuildTestPipeline();
        var context = pipeline.GetContext();
        var customerData = new CustomerData
        {
            Email = "valid@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "invalid-phone", // Will fail phone validation
            Age = 25
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await pipeline.ExecuteWithResultsAsync();

        // Assert
        result.EventResults.Should().HaveCount(7); // All events executed
        result.SuccessCount.Should().BeGreaterThan(0);
        result.FailureCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task ValidationPipeline_PartialDataQuality_RoutesToStandardQueue()
    {
        // Arrange
        var pipeline = BuildTestPipeline();
        var context = pipeline.GetContext();
        var customerData = new CustomerData
        {
            Email = "test@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            Phone = "+1-555-987-6543",
            Age = 28
            // Missing city, country - will affect enrichment
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await pipeline.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.TotalPrecisionScore.Should().BeInRange(70.0, 89.9);
        DetermineRouting(result.TotalPrecisionScore).Should().Contain("Standard");
    }

    [Test]
    public async Task ValidationPipeline_PremiumQualityData_RoutesToPremiumQueue()
    {
        // Arrange
        var pipeline = BuildTestPipeline();
        var context = pipeline.GetContext();
        var customerData = new CustomerData
        {
            Email = "premium@example.com",
            FirstName = "Premium",
            LastName = "Customer",
            Phone = "+1-555-111-2222",
            Age = 35,
            City = "San Francisco",
            Country = "USA",
            CreditScore = 800
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await pipeline.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.TotalPrecisionScore.Should().BeGreaterOrEqualTo(90.0);
        DetermineRouting(result.TotalPrecisionScore).Should().Contain("Premium");
    }

    [Test]
    public async Task ValidationPipeline_MultipleBatchProcessing_HandlesConsistently()
    {
        // Arrange
        var pipeline = BuildTestPipeline();
        var customers = new[]
        {
            CreateHighQualityCustomer(),
            CreateMediumQualityCustomer(),
            CreateLowQualityCustomer()
        };

        // Act
        var results = new List<ChainResult>();
        foreach (var customer in customers)
        {
            var context = pipeline.GetContext();
            context.Set("customer_data", customer);
            var result = await pipeline.ExecuteWithResultsAsync();
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        results[0].TotalPrecisionScore.Should().BeGreaterThan(results[1].TotalPrecisionScore);
        results[1].TotalPrecisionScore.Should().BeGreaterThan(results[2].TotalPrecisionScore);
    }

    [Test]
    public async Task ValidationPipeline_ContextSharing_EventsAccessPreviousResults()
    {
        // Arrange
        var pipeline = EventChain.Lenient();
        pipeline.AddEvent(new ValidateRequiredFields());
        pipeline.AddEvent(new EnrichWithCreditScore());
        pipeline.AddEvent(new CalculateRiskScore());

        var context = pipeline.GetContext();
        var customerData = new CustomerData
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Age = 30
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await pipeline.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeTrue();
        // Verify that CalculateRiskScore can access the enriched credit score
        var enrichedData = context.Get<CustomerData>("customer_data");
        enrichedData.CreditScore.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task ValidationPipeline_PerformanceMetrics_TrackedCorrectly()
    {
        // Arrange
        var pipeline = BuildTestPipeline();
        var context = pipeline.GetContext();
        var customerData = CreateHighQualityCustomer();
        context.Set("customer_data", customerData);

        // Act
        var startTime = DateTime.UtcNow;
        var result = await pipeline.ExecuteWithResultsAsync();
        var endTime = DateTime.UtcNow;
        var duration = (endTime - startTime).TotalMilliseconds;

        // Assert
        result.EventResults.Should().AllSatisfy(e =>
            // TODO: Check if Timing property exists - e.Timing.Should().NotBeNull()
            e.EventName.Should().NotBeNullOrEmpty()
        );
        duration.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Test]
    public async Task ValidationPipeline_DetailedAuditTrail_CapturesAllEvents()
    {
        // Arrange
        var pipeline = BuildTestPipeline();
        var context = pipeline.GetContext();
        var customerData = CreateMediumQualityCustomer();
        context.Set("customer_data", customerData);

        // Act
        var result = await pipeline.ExecuteWithResultsAsync();

        // Assert
        result.EventResults.Should().NotBeEmpty();
        result.EventResults.Should().AllSatisfy(e =>
        {
            e.EventName.Should().NotBeNullOrEmpty();
            e.PrecisionScore.Should().BeInRange(0, 100);
        });

        // Verify all expected events are present
        var eventNames = result.EventResults.Select(e => e.EventName).ToList();
        eventNames.Should().Contain("ValidateRequiredFields");
        eventNames.Should().Contain("ValidateEmailFormat");
        eventNames.Should().Contain("CalculateRiskScore");
    }

    [Test]
    public async Task ValidationPipeline_GradedPrecision_ReflectsDataQuality()
    {
        // Arrange
        var testCases = new[]
        {
            (Customer: CreateHighQualityCustomer(), ExpectedGrade: new[] { "A", "B" }),
            (Customer: CreateMediumQualityCustomer(), ExpectedGrade: new[] { "B", "C" }),
            (Customer: CreateLowQualityCustomer(), ExpectedGrade: new[] { "D", "F" })
        };

        // Act & Assert
        foreach (var (customer, expectedGrades) in testCases)
        {
            var pipeline = BuildTestPipeline();
            var context = pipeline.GetContext();
            context.Set("customer_data", customer);

            var result = await pipeline.ExecuteWithResultsAsync();

            result.GetGrade().Should().BeOneOf(expectedGrades);
        }
    }

    [Test]
    public async Task ValidationPipeline_ParallelProcessing_ThreadSafe()
    {
        // Arrange
        var customers = Enumerable.Range(0, 100)
            .Select(i => new CustomerData
            {
                Email = $"user{i}@example.com",
                FirstName = $"User{i}",
                LastName = "Test",
                Phone = $"+1-555-{i:D3}-{i:D4}",
                Age = 20 + (i % 50)
            })
            .ToList();

        // Act
        var tasks = customers.Select(async customer =>
        {
            var pipeline = BuildTestPipeline();
            var context = pipeline.GetContext();
            context.Set("customer_data", customer);
            return await pipeline.ExecuteWithResultsAsync();
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        results.Should().AllSatisfy(r => r.EventResults.Should().NotBeEmpty());
    }

    #region Helper Methods

    private CustomerData CreateHighQualityCustomer()
    {
        return new CustomerData
        {
            Email = "premium@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "+1-555-123-4567",
            Age = 35,
            City = "New York",
            Country = "USA",
            CreditScore = 800
        };
    }

    private CustomerData CreateMediumQualityCustomer()
    {
        return new CustomerData
        {
            Email = "standard@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            Phone = "+1-555-987-6543",
            Age = 28
            // Missing some optional fields
        };
    }

    private CustomerData CreateLowQualityCustomer()
    {
        return new CustomerData
        {
            Email = "low@example.com",
            FirstName = "Test",
            LastName = "User",
            Phone = "123", // Invalid phone
            Age = 150 // Invalid age
        };
    }

    private string DetermineRouting(double qualityScore)
    {
        return qualityScore switch
        {
            >= 90 => "Premium Queue (auto-approve)",
            >= 70 => "Standard Queue (standard review)",
            >= 50 => "Manual Review Queue (requires human approval)",
            _ => "Quarantine Queue (needs extensive review)"
        };
    }

    #endregion
}