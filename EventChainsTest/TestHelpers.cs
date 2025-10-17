using EventChainsCore;
using EventChains_CS;

namespace EventChains.Tests.Utilities;

/// <summary>
/// Helper class for creating test data consistently across all tests - CORRECTED VERSION
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Creates a customer with all valid, high-quality data
    /// </summary>
    public static CustomerData CreateValidCustomer(string? email = null)
    {
        return new CustomerData
        {
            Email = email ?? "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "+1-555-123-4567",
            Age = 30,
            City = "New York",
            Country = "USA",
            CreditScore = 750
        };
    }

    /// <summary>
    /// Creates a customer with premium-quality data (>90% quality score)
    /// </summary>
    public static CustomerData CreatePremiumCustomer()
    {
        return new CustomerData
        {
            Email = "premium@example.com",
            FirstName = "Premium",
            LastName = "Customer",
            Phone = "+1-555-111-2222",
            Age = 35,
            City = "San Francisco",
            Country = "USA",
            CreditScore = 850
        };
    }

    /// <summary>
    /// Creates a customer with standard-quality data (70-89% quality score)
    /// </summary>
    public static CustomerData CreateStandardCustomer()
    {
        return new CustomerData
        {
            Email = "standard@example.com",
            FirstName = "Standard",
            LastName = "Customer",
            Phone = "+1-555-333-4444",
            Age = 28
            // Missing some optional fields
        };
    }

    /// <summary>
    /// Creates a customer with minimal valid data
    /// </summary>
    public static CustomerData CreateMinimalCustomer()
    {
        return new CustomerData
        {
            Email = "minimal@example.com",
            FirstName = "Min",
            LastName = "Customer"
            // Only required fields
        };
    }

    /// <summary>
    /// Creates a customer with one or more invalid fields
    /// </summary>
    public static CustomerData CreateInvalidCustomer(
        bool invalidEmail = false,
        bool invalidPhone = false,
        bool invalidAge = false,
        bool missingRequired = false)
    {
        return new CustomerData
        {
            Email = missingRequired || invalidEmail ? "invalid-email" : "test@example.com",
            FirstName = missingRequired ? "" : "Test",
            LastName = missingRequired ? "" : "User",
            Phone = invalidPhone ? "123" : "+1-555-123-4567",
            Age = invalidAge ? 150 : 25
        };
    }

    /// <summary>
    /// Creates a batch of customers with varying quality levels
    /// </summary>
    public static List<CustomerData> CreateCustomerBatch(int count, QualityLevel quality = QualityLevel.Mixed)
    {
        var customers = new List<CustomerData>();

        for (int i = 0; i < count; i++)
        {
            CustomerData customer;

            if (quality == QualityLevel.Mixed)
            {
                // Mixed quality based on index
                customer = (i % 4) switch
                {
                    0 => CreatePremiumCustomer(),
                    1 => CreateStandardCustomer(),
                    2 => CreateMinimalCustomer(),
                    _ => CreateInvalidCustomer(invalidPhone: true)
                };
            }
            else
            {
                // Specific quality level
                customer = quality switch
                {
                    QualityLevel.Premium => CreatePremiumCustomer(),
                    QualityLevel.Standard => CreateStandardCustomer(),
                    QualityLevel.Minimal => CreateMinimalCustomer(),
                    QualityLevel.Invalid => CreateInvalidCustomer(invalidAge: true),
                    _ => CreateValidCustomer()
                };
            }

            // Ensure unique email for each customer
            if (!string.IsNullOrEmpty(customer.Email))
            {
                var emailParts = customer.Email.Split('@');
                if (emailParts.Length == 2)
                {
                    customer.Email = $"{emailParts[0]}_{i}@{emailParts[1]}";
                }
            }

            customers.Add(customer);
        }

        return customers;
    }

    public enum QualityLevel
    {
        Premium,
        Standard,
        Minimal,
        Invalid,
        Mixed
    }
}

/// <summary>
/// Helper class for common test assertions and validations
/// </summary>
public static class TestAssertions
{
    /// <summary>
    /// Asserts that a result meets premium queue criteria
    /// </summary>
    public static void ShouldRouteToPremiumQueue(this ChainResult result)
    {
        result.Success.Should().BeTrue();
        result.TotalPrecisionScore.Should().BeGreaterOrEqualTo(90.0);
        result.GetGrade().Should().BeOneOf("A", "A+");
    }

    /// <summary>
    /// Asserts that a result meets standard queue criteria
    /// </summary>
    public static void ShouldRouteToStandardQueue(this ChainResult result)
    {
        result.Success.Should().BeTrue();
        result.TotalPrecisionScore.Should().BeInRange(70.0, 89.9);
        result.GetGrade().Should().BeOneOf("B", "C");
    }

    /// <summary>
    /// Asserts that a result meets manual review criteria
    /// </summary>
    public static void ShouldRouteToManualReview(this ChainResult result)
    {
        result.TotalPrecisionScore.Should().BeInRange(50.0, 69.9);
        result.GetGrade().Should().BeOneOf("C", "D");
    }

    /// <summary>
    /// Asserts that a result should be quarantined
    /// </summary>
    public static void ShouldRouteToQuarantine(this ChainResult result)
    {
        result.TotalPrecisionScore.Should().BeLessThan(50.0);
        result.GetGrade().Should().Be("F");
    }

    /// <summary>
    /// Asserts that all events in the chain executed successfully
    /// </summary>
    public static void ShouldHaveAllEventsSucceed(this ChainResult result)
    {
        result.Success.Should().BeTrue();
        result.EventResults.Should().AllSatisfy(e => e.Success.Should().BeTrue());
        result.FailureCount.Should().Be(0);
    }

    /// <summary>
    /// Asserts that the chain has proper audit trail
    /// </summary>
    public static void ShouldHaveCompleteAuditTrail(this ChainResult result)
    {
        result.EventResults.Should().NotBeEmpty();
        result.EventResults.Should().AllSatisfy(e =>
        {
            e.EventName.Should().NotBeNullOrEmpty();
            e.PrecisionScore.Should().BeInRange(0, 100);
            // Note: Timing property may not exist - verify in your implementation
        });
    }
}

/// <summary>
/// Performance testing utilities
/// </summary>
public static class PerformanceTestHelpers
{
    /// <summary>
    /// Measures execution time of an async operation
    /// </summary>
    public static async Task<(T Result, TimeSpan Duration)> MeasureAsync<T>(Func<Task<T>> operation)
    {
        var startTime = DateTime.UtcNow;
        var result = await operation();
        var endTime = DateTime.UtcNow;

        return (result, endTime - startTime);
    }

    /// <summary>
    /// Asserts that an operation completes within a specified time
    /// </summary>
    public static void ShouldCompleteWithin(this TimeSpan duration, int milliseconds, string because = "")
    {
        duration.TotalMilliseconds.Should().BeLessThan(milliseconds, because);
    }

    /// <summary>
    /// Calculates throughput (operations per second)
    /// </summary>
    public static double CalculateThroughput(int operationCount, TimeSpan duration)
    {
        return operationCount / duration.TotalSeconds;
    }
}

/// <summary>
/// Constants used across tests
/// </summary>
public static class TestConstants
{
    // Valid test emails
    public const string ValidEmail = "test@example.com";
    public const string ValidEmailWithSubdomain = "test@mail.example.com";
    public const string ValidEmailWithPlus = "test+tag@example.com";

    // Invalid test emails
    public const string InvalidEmailNoAt = "testexample.com";
    public const string InvalidEmailMultipleAt = "test@@example.com";
    public const string InvalidEmailNoLocal = "@example.com";
    public const string InvalidEmailNoDomain = "test@";

    // Valid test phones
    public const string ValidUSPhone = "+1-555-123-4567";
    public const string ValidUKPhone = "+44 20 7946 0958";
    public const string ValidInternationalPhone = "+81 3-1234-5678";

    // Invalid test phones
    public const string InvalidPhoneTooShort = "123";
    public const string InvalidPhoneLetters = "ABC-DEF-GHIJ";

    // Valid ages
    public const int ValidAgeMinimum = 18;
    public const int ValidAgeTypical = 30;
    public const int ValidAgeMaximum = 120;

    // Invalid ages
    public const int InvalidAgeTooYoung = 10;
    public const int InvalidAgeTooOld = 150;

    // Quality thresholds
    public const double PremiumQualityThreshold = 90.0;
    public const double StandardQualityThreshold = 70.0;
    public const double ManualReviewThreshold = 50.0;

    // Performance expectations
    public const int MaxExecutionTimeMs = 5000;
    public const int MinThroughputRecordsPerSecond = 1000;
}

/// <summary>
/// Mock data generators for testing
/// </summary>
public static class MockDataGenerator
{
    private static readonly Random Random = new Random();

    private static readonly string[] FirstNames = { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Eve", "Frank" };
    private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
    private static readonly string[] Cities = { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia" };
    private static readonly string[] Countries = { "USA", "UK", "Canada", "Australia", "Germany", "France" };

    /// <summary>
    /// Generates a random customer with realistic data
    /// </summary>
    public static CustomerData GenerateRandomCustomer()
    {
        var firstName = FirstNames[Random.Next(FirstNames.Length)];
        var lastName = LastNames[Random.Next(LastNames.Length)];

        return new CustomerData
        {
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
            FirstName = firstName,
            LastName = lastName,
            Phone = $"+1-555-{Random.Next(100, 999)}-{Random.Next(1000, 9999)}",
            Age = Random.Next(18, 80),
            City = Cities[Random.Next(Cities.Length)],
            Country = Countries[Random.Next(Countries.Length)],
            CreditScore = Random.Next(300, 850)
        };
    }

    /// <summary>
    /// Generates a batch of random customers
    /// </summary>
    public static List<CustomerData> GenerateRandomCustomerBatch(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => GenerateRandomCustomer())
            .ToList();
    }
}