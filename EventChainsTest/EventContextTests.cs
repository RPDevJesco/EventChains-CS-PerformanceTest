using EventChainsCore;

namespace EventChains.Tests.Core;

/// <summary>
/// Tests for the EventContext shared state management - CORRECTED VERSION
/// </summary>
[TestFixture]
public class EventContextTests
{
    [Test]
    public void Set_ValidKeyValue_StoresValue()
    {
        // Arrange
        var context = new EventContext();
        var key = "test_key";
        var value = "test_value";

        // Act
        context.Set(key, value);

        // Assert
        var result = context.Get<string>(key);
        result.Should().Be(value);
    }

    [Test]
    public void Set_OverwriteExistingKey_UpdatesValue()
    {
        // Arrange
        var context = new EventContext();
        var key = "test_key";

        // Act
        context.Set(key, "first_value");
        context.Set(key, "second_value");

        // Assert
        var result = context.Get<string>(key);
        result.Should().Be("second_value");
    }

    [Test]
    public void Get_ExistingKey_ReturnsValue()
    {
        // Arrange
        var context = new EventContext();
        var key = "test_key";
        var value = 42;
        context.Set(key, value);

        // Act
        var result = context.Get<int>(key);

        // Assert
        result.Should().Be(value);
    }

    [Test]
    public void Get_NonExistentKey_ThrowsException()
    {
        // Arrange
        var context = new EventContext();

        // Act
        Action act = () => context.Get<string>("non_existent_key");

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void TryGet_ExistingKey_ReturnsTrueAndValue()
    {
        // Arrange
        var context = new EventContext();
        var key = "test_key";
        var value = "test_value";
        context.Set(key, value);

        // Act
        var success = context.TryGet(key, out string? result);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(value);
    }

    [Test]
    public void TryGet_NonExistentKey_ReturnsFalseAndDefault()
    {
        // Arrange
        var context = new EventContext();

        // Act
        var success = context.TryGet("non_existent_key", out string? result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Test]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var context = new EventContext();
        var key = "test_key";
        context.Set(key, "value");

        // Act
        var result = context.ContainsKey(key);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ContainsKey_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var context = new EventContext();

        // Act
        var result = context.ContainsKey("non_existent_key");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void Clear_RemovesAllValues()
    {
        // Arrange
        var context = new EventContext();
        context.Set("key1", "value1");
        context.Set("key2", "value2");
        context.Set("key3", "value3");

        // Act
        context.Clear();

        // Assert
        context.ContainsKey("key1").Should().BeFalse();
        context.ContainsKey("key2").Should().BeFalse();
        context.ContainsKey("key3").Should().BeFalse();
    }

    [Test]
    public void Set_DifferentTypes_StoresCorrectly()
    {
        // Arrange
        var context = new EventContext();

        // Act
        context.Set("string", "text");
        context.Set("int", 42);
        context.Set("bool", true);
        context.Set("double", 3.14);
        context.Set("object", new { Name = "Test" });

        // Assert
        context.Get<string>("string").Should().Be("text");
        context.Get<int>("int").Should().Be(42);
        context.Get<bool>("bool").Should().BeTrue();
        context.Get<double>("double").Should().Be(3.14);
        context.Get<object>("object").Should().NotBeNull();
    }

    [Test]
    public void Get_WrongType_ThrowsException()
    {
        // Arrange
        var context = new EventContext();
        context.Set("key", "string_value");

        // Act
        Action act = () => context.Get<int>("key");

        // Assert
        act.Should().Throw<InvalidCastException>();
    }

    [Test]
    public void Context_ThreadSafety_HandlesConcurrentAccess()
    {
        // Arrange
        var context = new EventContext();
        var tasks = new List<Task>();
        var iterations = 1000;

        // Act - Multiple threads reading and writing
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    var key = $"key_{taskId}_{j}";
                    context.Set(key, j);
                    context.Get<int>(key);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - No exceptions thrown, all operations completed
        tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
    }

    [Test]
    public void Set_NullValue_StoresNull()
    {
        // Arrange
        var context = new EventContext();
        var key = "null_key";

        // Act
        context.Set<string?>(key, null);

        // Assert
        context.ContainsKey(key).Should().BeTrue();
        context.Get<string?>(key).Should().BeNull();
    }

    [Test]
    public void Set_ComplexObject_StoresAndRetrieves()
    {
        // Arrange
        var context = new EventContext();
        var complexObject = new TestComplexObject
        {
            Id = 1,
            Name = "Test",
            Items = new List<string> { "Item1", "Item2" }
        };

        // Act
        context.Set("complex", complexObject);
        var retrieved = context.Get<TestComplexObject>("complex");

        // Assert
        retrieved.Should().BeSameAs(complexObject); // Same reference
        retrieved.Id.Should().Be(1);
        retrieved.Name.Should().Be("Test");
        retrieved.Items.Should().HaveCount(2);
    }

    private class TestComplexObject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<string>? Items { get; set; }
    }
}