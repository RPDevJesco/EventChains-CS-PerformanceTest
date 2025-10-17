# EventChains 2.0: Graduated Precision Edition

## 🎯 What's New in 2.0

EventChains 2.0 introduces **graduated precision** - a revolutionary approach to building systems where success isn't binary (pass/fail) but exists on a spectrum. This enables game mechanics, workflows, and interactions that are prohibitively complex with traditional approaches.

### Key Features

- ✅ **Graduated Success System**: Events can partially succeed with precision scores (0-100%)
- ✅ **Flexible Fault Tolerance**: Strict, Lenient, Best-Effort, and Custom modes
- ✅ **Layered Precision Events**: Nested timing windows for skill expression (perfect for QTEs)
- ✅ **Comprehensive Result Tracking**: Every event returns detailed results with precision metrics
- ✅ **Enhanced Context**: New convenience methods for scoring, tracking, and state management
- ✅ **Backwards Compatible**: Legacy code continues to work with minimal changes

---

## 🎮 The Killer Example: Layered Precision QTE

Traditional QTE (Quick Time Events) are binary: you either hit the button in time or you don't.

EventChains 2.0 enables **graduated precision QTE** with nested timing windows:

```csharp
// Create a best-effort chain - tries all layers
var precisionQTE = EventChain.BestEffort();

// Add precision layers (outermost to innermost)
precisionQTE.AddEvent(new PrecisionRingEvent(
    name: "Outer Ring",
    windowMs: 1000,      // 1 second window - EASY
    score: 50,
    effect: "normal_attack"
));

precisionQTE.AddEvent(new PrecisionRingEvent(
    name: "Middle Ring",
    windowMs: 500,       // 0.5 second window - MEDIUM
    score: 100,
    effect: "heavy_attack"
));

precisionQTE.AddEvent(new PrecisionRingEvent(
    name: "Center Ring",
    windowMs: 200,       // 0.2 second window - HARD
    score: 200,
    effect: "critical_strike"
));

// Simulate player input
var context = new EventContext();
context.Set("input_time_ms", 450.0); // Player pressed at 450ms

var result = await precisionQTE.ExecuteWithResultsAsync();

// Result analysis:
// - Rings Hit: 2/3 (Outer + Middle)
// - Total Score: 150 points
// - Precision: 66.7%
// - Grade: D
// - Effect: heavy_attack
```

### What Makes This Powerful

**Traditional Approach:**
```csharp
// Binary: hit or miss
if (inputTime < 500) {
    return "success";
} else {
    return "failure";
}
// No skill expression
// No graduated rewards
// No reusability
```

**EventChains Approach:**
```csharp
// Graduated: outer/middle/center rings
// - Hit all three: Perfect! (350 points, critical_strike)
// - Hit two: Great! (150 points, heavy_attack)
// - Hit one: OK (50 points, normal_attack)
// - Hit none: Miss (0 points)
// 
// ✅ Skill expression through precision
// ✅ Accessible (outer ring = success)
// ✅ Rewarding mastery (inner rings = bonus)
// ✅ Completely reusable and composable
```

---

## 🔄 Fault Tolerance Modes

EventChains 2.0 provides four fault tolerance modes:

### 1. STRICT (Default)
Any event failure stops the chain immediately.

```csharp
var chain = EventChain.Strict();
chain.AddEvent(new ValidateInput());
chain.AddEvent(new ProcessPayment());
chain.AddEvent(new SendConfirmation());

// If ProcessPayment fails, SendConfirmation never runs
var result = await chain.ExecuteWithResultsAsync();
```

**Use for:**
- Financial transactions
- Authentication flows  
- Critical workflows where partial completion is unacceptable

### 2. LENIENT
Non-critical failures are logged but chain continues.

```csharp
var chain = EventChain.Lenient();
chain.AddEvent(new ProcessOrder());
chain.AddEvent(new SendEmail()); // May fail, but order still processes
chain.AddEvent(new UpdateAnalytics()); // May fail, but order still processes

var result = await chain.ExecuteWithResultsAsync();
// result.Success is true if at least one event succeeded
```

**Use for:**
- Workflows where some steps are optional
- Background tasks that shouldn't block main flow

### 3. BEST_EFFORT (Perfect for Graduated Precision)
All events are attempted, failures are collected.

```csharp
var qte = EventChain.BestEffort();
qte.AddEvent(new OuterRing(1000ms));
qte.AddEvent(new MiddleRing(500ms));
qte.AddEvent(new CenterRing(200ms));

// Always tries all three rings
// Collects how many were hit
// Calculates overall precision score
var result = await qte.ExecuteWithResultsAsync();
Console.WriteLine($"Precision: {result.TotalPrecisionScore}%");
Console.WriteLine($"Grade: {result.GetGrade()}"); // S, A, B, C, D, or F
```

**Use for:**
- QTE systems with layered precision
- Batch operations (send notifications to all users)
- Fire-and-forget operations

### 4. CUSTOM
User-defined logic determines continuation.

```csharp
var chain = EventChain.Custom((eventResult, context) =>
{
    // Continue if error is non-fatal
    var errorCode = context.GetOrDefault<string>("error_code");
    return errorCode != "FATAL";
});

chain.AddEvent(new ProcessRecord1());
chain.AddEvent(new ProcessRecord2());
chain.AddEvent(new ProcessRecord3());

var result = await chain.ExecuteWithResultsAsync();
```

**Use for:**
- Complex error handling scenarios
- Multi-tenant processing where some failures are acceptable

---

## 📊 Result Tracking

Every event returns an `EventResult`:

```csharp
public class EventResult
{
    bool Success { get; }           // Did the event succeed?
    string? ErrorMessage { get; }   // Error details if failed
    object? Data { get; }           // Optional result data
    double PrecisionScore { get; }  // 0-100% precision score
    string EventName { get; }       // Name for tracking
}
```

The entire chain returns a `ChainResult`:

```csharp
public class ChainResult
{
    bool Success { get; }                    // Overall success
    List<EventResult> EventResults { get; }  // Individual results
    double TotalPrecisionScore { get; }      // Aggregated precision
    int SuccessCount { get; }                // Events that succeeded
    int FailureCount { get; }                // Events that failed
    IEventContext Context { get; }           // Final context state
    long ExecutionTimeMs { get; }            // Total execution time
    
    string GetGrade() { }  // S, A, B, C, D, or F based on precision
}
```

**Example:**

```csharp
var result = await chain.ExecuteWithResultsAsync();

Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Events: {result.SuccessCount}/{result.TotalCount}");
Console.WriteLine($"Precision: {result.TotalPrecisionScore:F1}%");
Console.WriteLine($"Grade: {result.GetGrade()}");
Console.WriteLine($"Time: {result.ExecutionTimeMs}ms");

// Inspect individual events
foreach (var eventResult in result.EventResults)
{
    Console.WriteLine($"  {eventResult.EventName}: {eventResult.PrecisionScore:F1}%");
}
```

---

## 🎨 Base Event Classes

EventChains 2.0 provides base classes for common patterns:

### BaseEvent

```csharp
public class MyCustomEvent : BaseEvent
{
    public override async Task<EventResult> ExecuteAsync(IEventContext context)
    {
        // Do your logic
        if (successful)
        {
            return Success(data, precisionScore: 95.0);
        }
        
        return Failure("Something went wrong", precisionScore: 30.0);
    }
}
```

### TimingEvent (For QTE Systems)

```csharp
public class QuickTimeEvent : TimingEvent
{
    public QuickTimeEvent()
        : base(windowMs: 500, precisionScore: 100, effect: "perfect_dodge")
    {
    }
    
    // Automatically checks input_time_ms against window
    // Calculates precision based on how close to perfect
    // Updates total_score in context
}
```

### LayeredPrecisionEvent

```csharp
public class CombatQTE : LayeredPrecisionEvent
{
    public CombatQTE()
    {
        // Add layers from outermost to innermost
        AddLayer("Outer", windowMs: 1000, score: 50, effect: "hit");
        AddLayer("Middle", windowMs: 500, score: 100, effect: "critical");
        AddLayer("Center", windowMs: 200, score: 200, effect: "devastating");
    }
    
    // Automatically determines which layers were hit
    // Awards best effect achieved
    // Calculates precision based on layers hit
}
```

### ConditionalEvent

```csharp
var conditionalEvent = new ConditionalEvent(
    condition: ctx => ctx.Get<int>("player_level") >= 10,
    innerEvent: new HardModeEvent(),
    conditionDescription: "Player level >= 10"
);

// Only executes HardModeEvent if condition is met
// Otherwise skips gracefully
```

### SubChainEvent

```csharp
// Build a sub-chain
var attackCombo = EventChain.BestEffort();
attackCombo.AddEvent(new Jab());
attackCombo.AddEvent(new Cross());
attackCombo.AddEvent(new Uppercut());

// Use it as a single event in another chain
var battleChain = new EventChain();
battleChain.AddEvent(new PlayerTurn(new SubChainEvent(attackCombo)));
battleChain.AddEvent(new EnemyTurn());
```

---

## 🔧 Enhanced Context

The `IEventContext` interface now includes powerful convenience methods:

### Increment
```csharp
// Track cumulative scores
context.Increment("total_damage", 50.0, initialValue: 0.0);
context.Increment("combo_count", 1, initialValue: 0);
```

### Append
```csharp
// Collect results from multiple events
context.Append("errors", "Validation failed");
context.Append("successful_attacks", attackData);

var allErrors = context.Get<List<string>>("errors");
```

### UpdateIfBetter
```csharp
// Track the best result seen so far
context.UpdateIfBetter("highest_score", newScore);
context.UpdateIfBetter("best_effect", "critical_strike");

// Uses default comparer or provide your own
context.UpdateIfBetter("best_player", player, new PlayerSkillComparer());
```

### GetOrDefault
```csharp
// Safe access with fallback
var score = context.GetOrDefault<double>("score", 0.0);
var name = context.GetOrDefault<string>("player_name", "Unknown");
```

---

## 🎯 Real-World Use Cases

### 1. Fighting Game Combo System

```csharp
var combo = EventChain.Lenient(); // Can miss some inputs

// Each hit is a graduated precision QTE
combo.AddEvent(new SubChainEvent(CreateQTE("Jab", windows: [800, 400, 200])));
combo.AddEvent(new SubChainEvent(CreateQTE("Cross", windows: [700, 350, 150])));
combo.AddEvent(new SubChainEvent(CreateQTE("Uppercut", windows: [600, 300, 100])));

var result = await combo.ExecuteWithResultsAsync();

// Player gets partial credit for hits
// Final damage based on total precision score
var damage = result.TotalPrecisionScore * 2.0;
```

### 2. Adaptive Difficulty System

```csharp
EventChain CreateAdaptiveChallenge(int playerSkill)
{
    var chain = EventChain.BestEffort();
    
    // Always have easy layer (accessible)
    chain.AddEvent(new EasyLayer(1000ms, 50));
    
    // Add medium layer for intermediate players
    if (playerSkill >= 5)
        chain.AddEvent(new MediumLayer(500ms, 100));
    
    // Add hard layer for experts
    if (playerSkill >= 8)
        chain.AddEvent(new HardLayer(200ms, 200));
    
    return chain;
}
```

### 3. API Request Pipeline with Graduated Validation

```csharp
var pipeline = EventChain.Lenient(); // Some validations can fail

pipeline.AddEvent(new ValidateRequiredFields());     // Critical
pipeline.AddEvent(new ValidateOptionalFields());     // Nice to have
pipeline.AddEvent(new ValidateBusinessRules());      // Important
pipeline.AddEvent(new EnrichData());                 // Optional
pipeline.AddEvent(new ProcessRequest());             // Critical

var result = await pipeline.ExecuteWithResultsAsync();

// Response includes validation score
return new ApiResponse
{
    Success = result.Success,
    ValidationScore = result.TotalPrecisionScore,
    Warnings = result.EventResults
        .Where(r => !r.Success)
        .Select(r => r.ErrorMessage)
        .ToList()
};
```

### 4. Data Processing with Graduated Quality

```csharp
var pipeline = EventChain.BestEffort(); // Try all quality checks

pipeline.AddEvent(new BasicValidation());      // 20 points
pipeline.AddEvent(new SchemaValidation());     // 30 points
pipeline.AddEvent(new BusinessRuleCheck());    // 30 points
pipeline.AddEvent(new DataEnrichment());       // 20 points

var result = await pipeline.ExecuteWithResultsAsync();

// Data quality score determines downstream processing
if (result.TotalPrecisionScore >= 80)
{
    await SendToProductionDB();
}
else if (result.TotalPrecisionScore >= 50)
{
    await SendToStagingDB();
}
else
{
    await SendToQuarantineDB();
}
```

---

## 🔄 Migration from 1.x

EventChains 2.0 is mostly backwards compatible. Here's how to upgrade:

### Before (1.x)
```csharp
public class MyEvent : IChainableEvent
{
    public async Task ExecuteAsync(IEventContext context)
    {
        // Do work
        if (!success)
            throw new Exception("Failed");
    }
}

var chain = new EventChain();
chain.AddEvent(new MyEvent());
await chain.ExecuteAsync(); // Throws on failure
```

### After (2.0)
```csharp
public class MyEvent : IChainableEvent
{
    public async Task<EventResult> ExecuteAsync(IEventContext context)
    {
        // Do work
        if (!success)
            return EventResult.CreateFailure("MyEvent", "Failed");
        
        return EventResult.CreateSuccess("MyEvent");
    }
}

var chain = new EventChain(); // Still works
chain.AddEvent(new MyEvent());

// Option 1: Legacy behavior (throws on failure)
await chain.ExecuteAsync();

// Option 2: New behavior (returns detailed results)
var result = await chain.ExecuteWithResultsAsync();
if (!result.Success)
{
    // Handle gracefully
}
```

### Quick Migration Tips

1. **Change event return type**: `Task` → `Task<EventResult>`
2. **Return results instead of throwing**: Use `EventResult.CreateSuccess/Failure`
3. **Use new execution method**: `ExecuteWithResultsAsync()` instead of `ExecuteAsync()`
4. **Choose fault tolerance mode**: `EventChain.Strict()` matches old behavior

---

## 📚 Documentation & Examples

- **Core Library**: `/EventChains.Core/` - Main implementation
- **QTE Example**: `/Examples/GraduatedQTE/` - Comprehensive QTE demonstration
- **Original Examples**: `/Examples/*/` - CI/CD, File Processing, API, ML examples
- **README**: Complete pattern documentation

---

## 🎓 Why EventChains 2.0 Matters

EventChains 2.0 enables **game mechanics and workflows that are prohibitively complex with traditional approaches**:

**Traditional QTE:**
- Binary success (hit/miss)
- No skill expression
- One-size-fits-all difficulty
- Hard to make accessible yet rewarding

**EventChains QTE:**
- Graduated success (outer/middle/center rings)
- Skill expression through precision
- Dynamic difficulty (add/remove layers)
- Accessible (outer ring) + rewarding (inner rings)
- Composable and reusable

This pattern applies beyond games:
- **API Validation**: Graduated quality scores instead of pass/fail
- **Data Processing**: Quality levels determine routing
- **Testing**: Test suites with varying importance levels
- **Workflows**: Partial success with degraded functionality

---

## 🚀 Getting Started

```bash
# Install via NuGet (when published)
dotnet add package EventChains.Core

# Or clone and build
git clone https://github.com/RPDevJesco/EventChains-CS
cd EventChains-CS
dotnet build
dotnet run --project Examples/GraduatedQTE
```

---

## 📝 License

MIT License - See LICENSE file for details

---

## 🤝 Contributing

Contributions welcome! This is a genuinely novel approach to graduated success systems, and there are likely many more applications waiting to be discovered.

---

**EventChains 2.0** - *Where success isn't binary, it's graduated.*
