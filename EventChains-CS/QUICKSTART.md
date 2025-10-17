# EventChains 2.0 - Quick Start Guide

## 5-Minute Introduction

EventChains 2.0 enables **graduated precision** - where success isn't binary but exists on a spectrum. Perfect for QTE systems, skill-based gameplay, and workflows with varying quality levels.

---

## Installation

```bash
# Clone repository
git clone https://github.com/RPDevJesco/EventChains-CS
cd EventChains-CS

# Build
dotnet build

# Run examples
dotnet run --project Examples/GraduatedQTE
```

---

## Your First Graduated Precision Chain

### Step 1: Create a Chain with Fault Tolerance Mode

```csharp
using EventChains.Core;
using EventChains.Core.Events;

// Best-effort mode: tries all events, collects results
var chain = EventChain.BestEffort();
```

### Step 2: Add Events with Different Precision Levels

```csharp
// Outer ring: Easy, low score
chain.AddEvent(new TimingEvent(
    windowMs: 1000,
    precisionScore: 50,
    effect: "normal"
));

// Middle ring: Medium, medium score
chain.AddEvent(new TimingEvent(
    windowMs: 500,
    precisionScore: 100,
    effect: "critical"
));

// Inner ring: Hard, high score
chain.AddEvent(new TimingEvent(
    windowMs: 200,
    precisionScore: 200,
    effect: "perfect"
));
```

### Step 3: Set Input and Execute

```csharp
var context = chain.GetContext();
context.Set("input_time_ms", 450.0); // Player input at 450ms

var result = await chain.ExecuteWithResultsAsync();
```

### Step 4: Analyze Results

```csharp
Console.WriteLine($"Rings Hit: {result.SuccessCount}/{result.TotalCount}");
Console.WriteLine($"Total Score: {context.GetOrDefault<double>("total_score")}");
Console.WriteLine($"Precision: {result.TotalPrecisionScore:F1}%");
Console.WriteLine($"Grade: {result.GetGrade()}"); // S, A, B, C, D, or F
Console.WriteLine($"Effect: {context.GetOrDefault<string>("best_effect")}");
```

**Output:**
```
Rings Hit: 2/3
Total Score: 150
Precision: 66.7%
Grade: D
Effect: critical
```

---

## Common Patterns

### Pattern 1: Simple QTE

```csharp
public class SimpleQTE : TimingEvent
{
    public SimpleQTE() : base(
        windowMs: 500,
        precisionScore: 100,
        effect: "dodge_success"
    ) { }
}

var qte = EventChain.Strict();
qte.AddEvent(new SimpleQTE());
```

### Pattern 2: Layered Precision QTE

```csharp
public class LayeredQTE : LayeredPrecisionEvent
{
    public LayeredQTE()
    {
        AddLayer("Outer", 1000, 50, "hit");
        AddLayer("Middle", 500, 100, "critical");
        AddLayer("Center", 200, 200, "perfect");
    }
}

var qte = EventChain.BestEffort();
qte.AddEvent(new LayeredQTE());
```

### Pattern 3: Adaptive Difficulty

```csharp
EventChain CreateAdaptiveQTE(int playerSkill)
{
    var chain = EventChain.BestEffort();
    
    // Always have easy ring
    chain.AddEvent(new EasyRing());
    
    // Add harder rings for skilled players
    if (playerSkill >= 5)
        chain.AddEvent(new MediumRing());
    
    if (playerSkill >= 8)
        chain.AddEvent(new HardRing());
    
    return chain;
}
```

### Pattern 4: Combo System

```csharp
var combo = EventChain.Lenient(); // Can miss some hits

combo.AddEvent(new SubChainEvent(CreateQTE("Hit1")));
combo.AddEvent(new SubChainEvent(CreateQTE("Hit2")));
combo.AddEvent(new SubChainEvent(CreateQTE("Hit3")));

var result = await combo.ExecuteWithResultsAsync();

// Total score reflects entire combo performance
var damageMultiplier = result.TotalPrecisionScore / 100.0;
```

---

## Fault Tolerance Modes

### Strict (Default)
```csharp
var chain = EventChain.Strict();
// Any failure stops execution
// Use for: Critical workflows
```

### Lenient
```csharp
var chain = EventChain.Lenient();
// Non-critical failures continue
// Use for: Optional steps
```

### Best-Effort ⭐
```csharp
var chain = EventChain.BestEffort();
// All events attempted
// Perfect for: Graduated precision systems
```

### Custom
```csharp
var chain = EventChain.Custom((result, ctx) => {
    // Your logic here
    return shouldContinue;
});
```

---

## Event Result Handling

### Individual Event Results

```csharp
var result = await chain.ExecuteWithResultsAsync();

foreach (var eventResult in result.EventResults)
{
    Console.WriteLine($"{eventResult.EventName}:");
    Console.WriteLine($"  Success: {eventResult.Success}");
    Console.WriteLine($"  Precision: {eventResult.PrecisionScore:F1}%");
    
    if (eventResult.Data != null)
    {
        Console.WriteLine($"  Data: {eventResult.Data}");
    }
}
```

### Aggregate Results

```csharp
var result = await chain.ExecuteWithResultsAsync();

Console.WriteLine($"Overall Success: {result.Success}");
Console.WriteLine($"Events Succeeded: {result.SuccessCount}");
Console.WriteLine($"Events Failed: {result.FailureCount}");
Console.WriteLine($"Total Precision: {result.TotalPrecisionScore:F1}%");
Console.WriteLine($"Grade: {result.GetGrade()}");
Console.WriteLine($"Execution Time: {result.ExecutionTimeMs}ms");
```

---

## Context Helpers

### Increment (For Scoring)

```csharp
// Accumulate score across events
context.Increment("total_damage", 50.0, initialValue: 0.0);
context.Increment("hits_landed", 1, initialValue: 0);
```

### Append (For Collecting)

```csharp
// Collect results from multiple events
context.Append("combo_moves", "uppercut");
context.Append("errors", errorMessage);

var allMoves = context.Get<List<string>>("combo_moves");
```

### UpdateIfBetter (For Best Results)

```csharp
// Track highest score, best effect, etc.
context.UpdateIfBetter("max_damage", newDamage);
context.UpdateIfBetter("best_effect", "critical_strike");
```

---

## Next Steps

1. **Read the full documentation**: `EVENTCHAINS_2.0.md`
2. **Explore examples**: `Examples/GraduatedQTE/`
3. **Check original examples**: `Examples/CiCdPipeline/`, `FileProcessing/`, etc.
4. **Build your own**: Start with a simple QTE and expand

---

## Common Questions

**Q: How is this different from a regular if-else chain?**

A: EventChains provides:
- Graduated success (not just pass/fail)
- Automatic precision scoring
- Composable and reusable events
- Fault tolerance modes
- Comprehensive result tracking
- Clean separation of concerns

**Q: Can I use this for non-game applications?**

A: Absolutely! Use it for:
- API validation with quality scores
- Data processing with graduated quality
- Workflow engines with partial success
- Testing frameworks with priority levels

**Q: Is it backwards compatible with EventChains 1.x?**

A: Mostly yes. Change event return type to `Task<EventResult>` and use `ExecuteWithResultsAsync()`. See migration guide in main docs.

**Q: What's the performance overhead?**

A: Minimal. The pattern adds microseconds per event. For typical use cases (game logic, API requests), this is negligible.

---

**Ready to build something amazing? Start with the Graduated QTE example!**

```bash
dotnet run --project Examples/GraduatedQTE
```
