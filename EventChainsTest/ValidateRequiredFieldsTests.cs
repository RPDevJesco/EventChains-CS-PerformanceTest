using EventChainsCore;
using EventChains_CS;
using EventChains_CS.Validation_Events;

namespace EventChains.Tests.Validation;

/// <summary>
/// Tests for ValidateRequiredFields event
/// </summary>
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
/// Tests for ValidateEmailFormat event
/// </summary>
[TestFixture]
public class ValidateEmailFormatTests
{
    [Test]
    public async Task ExecuteAsync_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        ValidateEmailFormat.SkipDnsLookup = true; // Set static flag
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
        result.PrecisionScore.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task ExecuteAsync_InvalidEmailFormat_ReturnsFailure()
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
        result.PrecisionScore.Should().Be(0);
    }

    [Test]
    public async Task ExecuteAsync_EmailWithoutAtSign_ReturnsFailure()
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
    }

    [Test]
    public async Task ExecuteAsync_EmailWithMultipleAtSigns_ReturnsFailure()
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
    }
}

/// <summary>
/// Tests for ValidatePhoneFormat event
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
            Phone = "+1-555-123-4567"
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
    public async Task ExecuteAsync_InvalidPhone_ReturnsFailure()
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
        result.PrecisionScore.Should().Be(0);
    }

    [Test]
    public async Task ExecuteAsync_EmptyPhone_ReturnsFailure()
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
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_ValidInternationalPhone_ReturnsSuccess()
    {
        // Arrange
        var validator = new ValidatePhoneFormat();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Phone = "+44 20 7946 0958" // UK number
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
    }
}

/// <summary>
/// Tests for ValidateBusinessData event
/// </summary>
[TestFixture]
public class ValidateBusinessDataTests
{
    [Test]
    public async Task ExecuteAsync_ValidAge_ReturnsSuccess()
    {
        // Arrange
        var validator = new ValidateBusinessData();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 25
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.PrecisionScore.Should().Be(100);
    }

    [Test]
    public async Task ExecuteAsync_AgeTooYoung_ReturnsFailure()
    {
        // Arrange
        var validator = new ValidateBusinessData();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 10
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("age");
    }

    [Test]
    public async Task ExecuteAsync_AgeTooOld_ReturnsFailure()
    {
        // Arrange
        var validator = new ValidateBusinessData();
        var context = new EventContext();
        var customerData = new CustomerData
        {
            Age = 150
        };
        context.Set("customer_data", customerData);

        // Act
        var result = await validator.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("age");
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
/// Tests for CalculateRiskScore event
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
    public async Task ExecuteAsync_LowCreditScore_ReturnsHighRisk()
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
        // Risk score should be lower for low credit score
        result.PrecisionScore.Should().BeLessThan(70);
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
        result.PrecisionScore.Should().BeGreaterThan(0);
    }
}