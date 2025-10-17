using EventChains.Tests.Utilities;

using EventChains_CS;
using EventChains_CS.Validation_Events;

using EventChainsCore;

namespace EventChains.Tests.Validation;

/// <summary>
/// Tests for validation events - UPDATED FOR LENIENT MODE
/// Lenient mode provides partial credit and continues processing even with validation failures
/// </summary>
/// 
[TestFixture]
public class ValidateRequiredFieldsTests
{
    [Test]
    public async Task ExecuteAsync_AllFieldsPresent_ReturnsSuccess()
    {
        // Arrange
        var validator = new ValidateRequiredFields();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(100);
    }

    [Test]
    public async Task ExecuteAsync_MissingEmail_ReturnsFailure()
    {
        // Arrange
        var validator = new ValidateRequiredFields();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "",
            FirstName = "John",
            LastName = "Doe"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.PrecisionScore.Should().Be(0);
        result.ErrorMessage.Should().Contain("Email");
    }

    [Test]
    public async Task ExecuteAsync_MissingFirstName_ReturnsFailure()
    {
        // Arrange
        var validator = new ValidateRequiredFields();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "test@example.com",
            FirstName = null,
            LastName = "Doe"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("FirstName");
    }

    [Test]
    public void Diagnostic_SyncAllocationComparison()
    {
        var iterations = 1000;
        var customer = TestDataFactory.CreateValidCustomer();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        var pipeline = EventChain.Lenient();
        pipeline.AddEvent(new ValidateRequiredFields());
        pipeline.AddEvent(new ValidateEmailFormat());

        for (int i = 0; i < iterations; i++)
        {
            var context = pipeline.GetContext();
            context.Set("customer_data", customer);
            pipeline.ExecuteAsync();  // Now sync!
        }

        var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
        var totalAllocated = allocatedAfter - allocatedBefore;

        Console.WriteLine($"Sync version allocations:");
        Console.WriteLine($"  Total: {totalAllocated / 1024.0:F2} KB");
        Console.WriteLine($"  Per iteration: {totalAllocated / iterations:F2} bytes");
        Console.WriteLine($"  Per event: {totalAllocated / (iterations * 2):F2} bytes");
    }

    [Test]
    public async Task ExecuteAsync_MissingMultipleFields_ReturnsFailureWithAllFields()
    {
        // Arrange
        var validator = new ValidateRequiredFields();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "",
            FirstName = "",
            LastName = ""
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Email");
        result.ErrorMessage.Should().Contain("FirstName");
        result.ErrorMessage.Should().Contain("LastName");
    }

    [Test]
    public async Task ExecuteAsync_WhitespaceEmail_ReturnsFailure()
    {
        // Arrange
        var validator = new ValidateRequiredFields();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "   ",
            FirstName = "John",
            LastName = "Doe"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Email");
    }
}

/// <summary>
/// Tests for ValidateEmailFormat event - UPDATED FOR LENIENT MODE
/// </summary>
[TestFixture]
public class ValidateEmailFormatTests
{
    [Test]
    public async Task ExecuteAsync_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        ValidateEmailFormat.SkipDnsLookup = true;
        var validator = new ValidateEmailFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "test@example.com"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(100);
    }

    [Test]
    public async Task ExecuteAsync_InvalidEmailFormat_ReturnsPartialCredit()
    {
        // Arrange
        ValidateEmailFormat.SkipDnsLookup = true;
        var validator = new ValidateEmailFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "invalid-email"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        // UPDATED: Lenient mode gives partial credit (20) instead of 0
        result.PrecisionScore.Should().Be(20);
    }

    [Test]
    public async Task ExecuteAsync_EmailWithoutAtSign_ReturnsPartialCredit()
    {
        // Arrange
        ValidateEmailFormat.SkipDnsLookup = true;
        var validator = new ValidateEmailFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "testexample.com"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        // UPDATED: Gives partial credit
        result.PrecisionScore.Should().Be(20);
    }

    [Test]
    public async Task ExecuteAsync_EmailWithMultipleAtSigns_ReturnsPartialCredit()
    {
        // Arrange
        ValidateEmailFormat.SkipDnsLookup = true;
        var validator = new ValidateEmailFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "test@@example.com"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.PrecisionScore.Should().Be(20);
    }

    [Test]
    public async Task ExecuteAsync_ValidEmailWithSubdomain_ReturnsSuccess()
    {
        // Arrange
        ValidateEmailFormat.SkipDnsLookup = true;
        var validator = new ValidateEmailFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "test@mail.example.com"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(100);
    }

    [Test]
    public async Task ExecuteAsync_ValidEmailWithPlus_ReturnsSuccess()
    {
        // Arrange
        ValidateEmailFormat.SkipDnsLookup = true;
        var validator = new ValidateEmailFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Email = "test+tag@example.com"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(100);
    }
}

/// <summary>
/// Tests for ValidatePhoneFormat event - UPDATED FOR LENIENT MODE
/// </summary>
[TestFixture]
public class ValidatePhoneFormatTests
{
    [Test]
    public async Task ExecuteAsync_ValidUSPhone_ReturnsSuccess()
    {
        // Arrange
        var validator = new ValidatePhoneFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Phone = "+1 757 942 1256",
            Country = "US"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(100);
    }

    [Test]
    public async Task ExecuteAsync_ValidUSPhoneNoCountryCode_ReturnsPartialSuccess()
    {
        // Arrange
        var validator = new ValidatePhoneFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Phone = "(555) 123-4567"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        // Note: Actual behavior depends on libphonenumber parsing
        result.PrecisionScore.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task ExecuteAsync_InvalidPhone_ReturnsPartialCredit()
    {
        // Arrange
        var validator = new ValidatePhoneFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Phone = "123"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        // UPDATED: Lenient mode gives partial credit (30) instead of 0
        result.PrecisionScore.Should().Be(30);
    }

    [Test]
    public async Task ExecuteAsync_EmptyPhone_ReturnsPartialCredit()
    {
        // Arrange
        var validator = new ValidatePhoneFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Phone = ""
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        // UPDATED: Lenient mode treats empty phone as optional (partial success)
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(50);
    }

    [Test]
    public async Task ExecuteAsync_ValidInternationalPhone_ReturnsSuccess()
    {
        // Arrange
        var validator = new ValidatePhoneFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Phone = "+44 20 7946 0958"
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(100);
    }
}

/// <summary>
/// Tests for ValidateBusinessData event - UPDATED FOR LENIENT MODE
/// </summary>
[TestFixture]
public class ValidateBusinessDataTests
{
    [Test]
    public async Task ExecuteAsync_ValidAge_ReturnsPartialSuccess()
    {
        // Arrange
        var validator = new ValidateBusinessData();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 30
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        // UPDATED: With missing company data, precision is reduced
        // Age valid (30 points kept), but missing CompanyName (-20) and Revenue (-20)
        result.PrecisionScore.Should().Be(60);
    }

    [Test]
    public async Task ExecuteAsync_AgeTooYoung_ContinuesInLenientMode()
    {
        // Arrange
        var validator = new ValidateBusinessData();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 10 // Too young
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        // UPDATED: Lenient mode continues with reduced precision
        result.Success.Should().BeTrue();
        // Age invalid (-30), missing company (-20), missing revenue (-20) = 30 minimum
        result.PrecisionScore.Should().Be(30);
    }

    [Test]
    public async Task ExecuteAsync_AgeTooOld_ContinuesInLenientMode()
    {
        // Arrange
        var validator = new ValidateBusinessData();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 150 // Too old
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        // UPDATED: Lenient mode continues with reduced precision
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(30);
    }

    [Test]
    public async Task ExecuteAsync_ValidEdgeCaseAge18_ReturnsSuccess()
    {
        // Arrange
        var validator = new ValidateBusinessData();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 18
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_ValidEdgeCaseAge120_ReturnsSuccess()
    {
        // Arrange
        var validator = new ValidateBusinessData();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 120
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
    }
}

/// <summary>
/// Tests for CalculateRiskScore event - UPDATED FOR LENIENT MODE
/// </summary>
[TestFixture]
public class CalculateRiskScoreTests
{
    [Test]
    public async Task ExecuteAsync_HighCreditScore_ReturnsLowRisk()
    {
        // Arrange
        var calculator = new CalculateRiskScore();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 30,
            CreditScore = 800
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await calculator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().BeGreaterThan(70);
    }

    [Test]
    public async Task ExecuteAsync_LowCreditScore_ContinuesWithFullScore()
    {
        // Arrange
        var calculator = new CalculateRiskScore();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 25,
            CreditScore = 500
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await calculator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        // UPDATED: CalculateRiskScore always returns 100 in lenient mode
        // It calculates risk but doesn't penalize the precision score
        result.PrecisionScore.Should().Be(100);
    }

    [Test]
    public async Task ExecuteAsync_NoCreditScore_UsesDefault()
    {
        // Arrange
        var calculator = new CalculateRiskScore();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 30,
            CreditScore = 0 // No credit score
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await calculator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(100);
    }
}