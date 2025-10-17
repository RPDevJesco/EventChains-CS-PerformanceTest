using EventChainsCore;

namespace EventChains.Tests.Core;

/// <summary>
/// Tests for the core EventChain orchestration engine - CORRECTED VERSION
/// </summary>
[TestFixture]
public class EventChainTests
{
    [Test]
    public void EventChain_Constructor_CreatesStrictMode()
    {
        // Arrange & Act
        var chain = EventChain.Strict();

        // Assert
        chain.Should().NotBeNull();
        chain.FaultToleranceMode.Should().Be(FaultToleranceMode.Strict);
    }

    [Test]
    public void EventChain_Constructor_CreatesLenientMode()
    {
        // Arrange & Act
        var chain = EventChain.Lenient();

        // Assert
        chain.Should().NotBeNull();
        chain.FaultToleranceMode.Should().Be(FaultToleranceMode.Lenient);
    }

    [Test]
    public void EventChain_Constructor_CreatesBestEffortMode()
    {
        // Arrange & Act
        var chain = EventChain.BestEffort();

        // Assert
        chain.Should().NotBeNull();
        chain.FaultToleranceMode.Should().Be(FaultToleranceMode.BestEffort);
    }

    [Test]
    public void AddEvent_ValidEvent_AddsToChain()
    {
        // Arrange
        var chain = EventChain.Lenient();
        var testEvent = new TestSuccessEvent();

        // Act
        chain.AddEvent(testEvent);

        // Assert - verify by executing
        var result = chain.ExecuteWithResultsAsync().GetAwaiter().GetResult();
        result.EventResults.Should().HaveCount(1);
    }

    [Test]
    public async Task ExecuteWithResultsAsync_EmptyChain_ReturnsSuccessResult()
    {
        // Arrange
        var chain = EventChain.Lenient();

        // Act
        var result = await chain.ExecuteWithResultsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.EventResults.Should().BeEmpty();
        result.TotalPrecisionScore.Should().Be(100.0);
    }

    [Test]
    public async Task ExecuteWithResultsAsync_SingleSuccessEvent_ReturnsSuccess()
    {
        // Arrange
        var chain = EventChain.Lenient();
        chain.AddEvent(new TestSuccessEvent());

        // Act
        var result = await chain.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.EventResults.Should().HaveCount(1);
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(0);
        result.TotalPrecisionScore.Should().Be(100.0);
    }

    [Test]
    public async Task ExecuteWithResultsAsync_MultipleSuccessEvents_ReturnsSuccess()
    {
        // Arrange
        var chain = EventChain.Lenient();
        chain.AddEvent(new TestSuccessEvent());
        chain.AddEvent(new TestSuccessEvent());
        chain.AddEvent(new TestSuccessEvent());

        // Act
        var result = await chain.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.EventResults.Should().HaveCount(3);
        result.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
        result.TotalPrecisionScore.Should().Be(100.0);
    }

    [Test]
    public async Task ExecuteWithResultsAsync_StrictMode_FailureStopsExecution()
    {
        // Arrange
        var chain = EventChain.Strict();
        chain.AddEvent(new TestSuccessEvent());
        chain.AddEvent(new TestFailureEvent());
        chain.AddEvent(new TestSuccessEvent()); // Should not execute

        // Act
        var result = await chain.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.EventResults.Should().HaveCount(2); // Only first 2 events executed
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
    }

    [Test]
    public async Task ExecuteWithResultsAsync_LenientMode_ContinuesAfterFailure()
    {
        // Arrange
        var chain = EventChain.Lenient();
        chain.AddEvent(new TestSuccessEvent());
        chain.AddEvent(new TestFailureEvent());
        chain.AddEvent(new TestSuccessEvent()); // Should execute

        // Act
        var result = await chain.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeTrue(); // Lenient mode considers partial success
        result.EventResults.Should().HaveCount(3); // All events executed
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(1);
    }

    [Test]
    public async Task ExecuteWithResultsAsync_BestEffortMode_ExecutesAllEvents()
    {
        // Arrange
        var chain = EventChain.BestEffort();
        chain.AddEvent(new TestFailureEvent());
        chain.AddEvent(new TestFailureEvent());
        chain.AddEvent(new TestSuccessEvent());

        // Act
        var result = await chain.ExecuteWithResultsAsync();

        // Assert
        result.EventResults.Should().HaveCount(3); // All events executed
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(2);
    }

    [Test]
    public async Task ExecuteWithResultsAsync_PartialSuccessScores_CalculatesAverage()
    {
        // Arrange
        var chain = EventChain.Lenient();
        chain.AddEvent(new TestPartialSuccessEvent(100));
        chain.AddEvent(new TestPartialSuccessEvent(80));
        chain.AddEvent(new TestPartialSuccessEvent(60));

        // Act
        var result = await chain.ExecuteWithResultsAsync();

        // Assert
        result.TotalPrecisionScore.Should().Be(80.0); // Average: (100 + 80 + 60) / 3
    }

    [Test]
    public void GetContext_ReturnsSharedContext()
    {
        // Arrange
        var chain = EventChain.Lenient();

        // Act
        var context1 = chain.GetContext();
        var context2 = chain.GetContext();

        // Assert
        context1.Should().BeSameAs(context2); // Same instance
    }

    [Test]
    public async Task EventChain_ContextSharing_EventsAccessSameContext()
    {
        // Arrange
        var chain = EventChain.Lenient();
        var testKey = "test_data";
        var testValue = "test_value";

        chain.AddEvent(new TestContextWriteEvent(testKey, testValue));
        chain.AddEvent(new TestContextReadEvent(testKey, testValue));

        // Act
        var result = await chain.ExecuteWithResultsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.EventResults.Should().HaveCount(2);
        result.EventResults.Should().AllSatisfy(e => e.Success.Should().BeTrue());
    }

    [Test]
    public async Task ExecuteWithResultsAsync_ExceptionInEvent_HandlesGracefully()
    {
        // Arrange
        var chain = EventChain.Lenient();
        chain.AddEvent(new TestExceptionEvent());
        chain.AddEvent(new TestSuccessEvent());

        // Act
        var result = await chain.ExecuteWithResultsAsync();

        // Assert
        result.EventResults.Should().HaveCount(2);
        result.EventResults[0].Success.Should().BeFalse();
        result.EventResults[0].ErrorMessage.Should().Contain("Test exception");
    }

    [Test]
    public void ChainResult_GetGrade_HighScore_ReturnsA()
    {
        // Arrange
        var chain = EventChain.Lenient();
        chain.AddEvent(new TestPartialSuccessEvent(95));

        // Act
        var result = chain.ExecuteWithResultsAsync().GetAwaiter().GetResult();
        var grade = result.GetGrade();

        // Assert
        grade.Should().Be("A");
    }

    [Test]
    public void ChainResult_GetGrade_MediumScore_ReturnsB()
    {
        // Arrange
        var chain = EventChain.Lenient();
        chain.AddEvent(new TestPartialSuccessEvent(85));

        // Act
        var result = chain.ExecuteWithResultsAsync().GetAwaiter().GetResult();
        var grade = result.GetGrade();

        // Assert
        grade.Should().Be("B");
    }

    [Test]
    public void ChainResult_GetGrade_LowScore_ReturnsF()
    {
        // Arrange
        var chain = EventChain.Lenient();
        chain.AddEvent(new TestPartialSuccessEvent(45));

        // Act
        var result = chain.ExecuteWithResultsAsync().GetAwaiter().GetResult();
        var grade = result.GetGrade();

        // Assert
        grade.Should().Be("F");
    }
}

#region Test Helper Events

public class TestSuccessEvent : BaseEvent
{
    public override Task<EventResult> ExecuteAsync(IEventContext context)
    {
        return Task.FromResult(Success(precisionScore: 100));
    }
}

public class TestFailureEvent : BaseEvent
{
    public override Task<EventResult> ExecuteAsync(IEventContext context)
    {
        return Task.FromResult(Failure("Test failure", precisionScore: 0));
    }
}

public class TestPartialSuccessEvent : BaseEvent
{
    private readonly double _score;

    public TestPartialSuccessEvent(double score)
    {
        _score = score;
    }

    public override Task<EventResult> ExecuteAsync(IEventContext context)
    {
        return Task.FromResult(Success(precisionScore: _score));
    }
}

public class TestContextWriteEvent : BaseEvent
{
    private readonly string _key;
    private readonly string _value;

    public TestContextWriteEvent(string key, string value)
    {
        _key = key;
        _value = value;
    }

    public override Task<EventResult> ExecuteAsync(IEventContext context)
    {
        context.Set(_key, _value);
        return Task.FromResult(Success());
    }
}

public class TestContextReadEvent : BaseEvent
{
    private readonly string _key;
    private readonly string _expectedValue;

    public TestContextReadEvent(string key, string expectedValue)
    {
        _key = key;
        _expectedValue = expectedValue;
    }

    public override Task<EventResult> ExecuteAsync(IEventContext context)
    {
        var value = context.Get<string>(_key);
        if (value == _expectedValue)
        {
            return Task.FromResult(Success());
        }
        return Task.FromResult(Failure($"Expected '{_expectedValue}' but got '{value}'"));
    }
}

public class TestExceptionEvent : BaseEvent
{
    public override Task<EventResult> ExecuteAsync(IEventContext context)
    {
        throw new InvalidOperationException("Test exception");
    }
}

#endregion