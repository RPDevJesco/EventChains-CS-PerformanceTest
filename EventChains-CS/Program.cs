using EventChains.Core;
using EventChains.Core.Events;

namespace EventChains.Examples.GraduatedQTE
{
    /// <summary>
    /// Demonstrates the power of EventChains for creating graduated precision QTE systems.
    /// This is the "killer example" showing how EventChains enables mechanics that are
    /// prohibitively complex with traditional approaches.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║   EventChains - Graduated Precision QTE Demo         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine("This demo shows how EventChains enables LAYERED PRECISION");
            Console.WriteLine("instead of traditional binary pass/fail QTE systems.");
            Console.WriteLine();

            // Demo 1: Simple layered precision
            await DemoSimpleLayeredPrecision();
            Console.WriteLine("\n" + new string('═', 55) + "\n");

            // Demo 2: Context-adaptive difficulty
            await DemoAdaptiveDifficulty();
            Console.WriteLine("\n" + new string('═', 55) + "\n");

            // Demo 3: Combo chain with graduated success
            await DemoComboSystem();

            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Demo 1: Simple Layered Precision QTE
        /// Traditional: Hit or Miss
        /// EventChains: Outer ring, middle ring, or perfect center
        /// </summary>
        static async Task DemoSimpleLayeredPrecision()
        {
            Console.WriteLine("📍 DEMO 1: Simple Layered Precision QTE");
            Console.WriteLine("   Traditional: Binary (hit/miss)");
            Console.WriteLine("   EventChains: Graduated (outer/middle/center)\n");

            // Create a best-effort chain - try all layers
            var qteChain = EventChain.BestEffort();

            // Add three precision layers (outermost to innermost)
            qteChain.AddEvent(new PrecisionRingEvent(
                name: "Outer Ring",
                windowMs: 1000,
                score: 50,
                effect: "normal_attack"
            ));

            qteChain.AddEvent(new PrecisionRingEvent(
                name: "Middle Ring",
                windowMs: 500,
                score: 100,
                effect: "heavy_attack"
            ));

            qteChain.AddEvent(new PrecisionRingEvent(
                name: "Center Ring",
                windowMs: 200,
                score: 200,
                effect: "critical_strike"
            ));

            // Simulate different player timing performances
            await SimulateQTEInput("Perfect timing", qteChain, 150);
            await SimulateQTEInput("Good timing", qteChain, 450);
            await SimulateQTEInput("OK timing", qteChain, 800);
            await SimulateQTEInput("Too slow", qteChain, 1100);
        }

        /// <summary>
        /// Demo 2: Context-Adaptive Difficulty
        /// Dynamically add/remove layers based on player skill
        /// </summary>
        static async Task DemoAdaptiveDifficulty()
        {
            Console.WriteLine("📍 DEMO 2: Adaptive Difficulty");
            Console.WriteLine("   Beginners: 1 layer (forgiving)");
            Console.WriteLine("   Intermediate: 2 layers (skill expression)");
            Console.WriteLine("   Expert: 3 layers (mastery required)\n");

            var playerSkills = new[] { 1, 5, 9 }; // Beginner, Intermediate, Expert
            var skillLabels = new[] { "Beginner", "Intermediate", "Expert" };

            for (int i = 0; i < playerSkills.Length; i++)
            {
                Console.WriteLine($"   Player Skill Level: {playerSkills[i]}/10 ({skillLabels[i]})");
                var adaptiveChain = CreateAdaptiveQTE(playerSkills[i]);

                // Simulate a decent input
                var context = new EventContext();
                context.Set("input_time_ms", 450.0);

                var result = await adaptiveChain.ExecuteWithResultsAsync();

                Console.WriteLine($"   Layers Active: {result.TotalCount}");
                Console.WriteLine($"   Layers Hit: {result.SuccessCount}");
                Console.WriteLine($"   Precision Score: {result.TotalPrecisionScore:F1}%");
                Console.WriteLine($"   Grade: {result.GetGrade()}");
                Console.WriteLine($"   Effect: {result.Context.GetOrDefault<string>("best_effect", "none")}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Demo 3: Combo System
        /// Multiple sequential QTEs with graduated success throughout
        /// </summary>
        static async Task DemoComboSystem()
        {
            Console.WriteLine("📍 DEMO 3: Combo Attack System");
            Console.WriteLine("   Three sequential QTEs with graduated precision");
            Console.WriteLine("   Total score reflects overall performance\n");

            // Create a lenient chain for the combo (can miss some hits)
            var comboChain = EventChain.Lenient();

            // Hit 1: Opening strike (easier)
            var hit1 = CreateQTESubChain("Hit 1", new[]
            {
                (1000.0, 50.0, "opener"),
                (600.0, 100.0, "strong_opener")
            });

            // Hit 2: Follow-up (medium)
            var hit2 = CreateQTESubChain("Hit 2", new[]
            {
                (800.0, 75.0, "followup"),
                (400.0, 125.0, "power_followup")
            });

            // Hit 3: Finisher (hardest, most rewarding)
            var hit3 = CreateQTESubChain("Hit 3", new[]
            {
                (600.0, 100.0, "finisher"),
                (300.0, 150.0, "devastating_finisher"),
                (100.0, 300.0, "ultimate_finisher")
            });

            comboChain.AddEvent(new SubChainEvent(hit1, "Opening Strike"));
            comboChain.AddEvent(new SubChainEvent(hit2, "Follow-Up"));
            comboChain.AddEvent(new SubChainEvent(hit3, "Finisher"));

            // Simulate a combo attempt with varying precision
            var context = new EventContext();

            // Hit 1: Hit outer ring
            context.Set("input_time_ms", 700.0);
            await hit1.ExecuteWithResultsAsync();

            // Hit 2: Hit both rings
            context.Set("input_time_ms", 350.0);
            await hit2.ExecuteWithResultsAsync();

            // Hit 3: Perfect! Hit all three rings
            context.Set("input_time_ms", 80.0);
            var finalResult = await hit3.ExecuteWithResultsAsync();

            Console.WriteLine("   Combo Results:");
            Console.WriteLine($"   Hit 1: 1/2 rings (Outer only) - OK");
            Console.WriteLine($"   Hit 2: 2/2 rings (Both) - Great!");
            Console.WriteLine($"   Hit 3: 3/3 rings (PERFECT) - Amazing!");
            Console.WriteLine();
            Console.WriteLine($"   Total Score: {context.GetOrDefault<double>("total_score")}");
            Console.WriteLine($"   Final Effect: {context.GetOrDefault<string>("best_effect", "none")}");
            Console.WriteLine($"   Player Performance: Mastery Achieved!");
        }

        // Helper methods

        static async Task SimulateQTEInput(string label, EventChain qteChain, double inputTimeMs)
        {
            Console.WriteLine($"   Testing: {label} (input at {inputTimeMs}ms)");

            var context = new EventContext();
            context.Set("input_time_ms", inputTimeMs);

            var result = await qteChain.ExecuteWithResultsAsync();

            Console.WriteLine($"   ├─ Rings Hit: {result.SuccessCount}/{result.TotalCount}");
            Console.WriteLine($"   ├─ Total Score: {context.GetOrDefault<double>("total_score")}");
            Console.WriteLine($"   ├─ Precision: {result.TotalPrecisionScore:F1}%");
            Console.WriteLine($"   ├─ Grade: {result.GetGrade()}");
            Console.WriteLine($"   └─ Effect: {context.GetOrDefault<string>("best_effect", "miss")}");
            Console.WriteLine();
        }

        static EventChain CreateAdaptiveQTE(int playerSkill)
        {
            var chain = EventChain.BestEffort();

            // Always have outer ring (accessible to all)
            chain.AddEvent(new PrecisionRingEvent(
                name: "Outer Ring",
                windowMs: 1000,
                score: 50,
                effect: "normal"
            ));

            // Add middle ring for intermediate+ players
            if (playerSkill >= 4)
            {
                chain.AddEvent(new PrecisionRingEvent(
                    name: "Middle Ring",
                    windowMs: 500,
                    score: 100,
                    effect: "skilled"
                ));
            }

            // Add center ring for expert players
            if (playerSkill >= 7)
            {
                chain.AddEvent(new PrecisionRingEvent(
                    name: "Center Ring",
                    windowMs: 200,
                    score: 200,
                    effect: "masterful"
                ));
            }

            return chain;
        }

        static EventChain CreateQTESubChain(string name, (double windowMs, double score, string effect)[] layers)
        {
            var chain = EventChain.BestEffort();

            foreach (var (windowMs, score, effect) in layers)
            {
                chain.AddEvent(new PrecisionRingEvent(
                    name: $"{name} - {windowMs}ms",
                    windowMs: windowMs,
                    score: score,
                    effect: effect
                ));
            }

            return chain;
        }
    }

    /// <summary>
    /// A precision ring event for QTE systems.
    /// Represents a single timing window with a score and effect.
    /// </summary>
    public class PrecisionRingEvent : TimingEvent
    {
        private readonly string _name;

        public PrecisionRingEvent(string name, double windowMs, double score, string effect)
            : base(windowMs, score, effect)
        {
            _name = name;
        }

        public override string EventName => _name;

        protected override double CalculatePrecisionWithinWindow(double actualTimeMs)
        {
            // More forgiving precision curve for outer rings
            // Perfect score if within first 20% of window
            if (actualTimeMs <= WindowMs * 0.2)
            {
                return 100.0;
            }

            // Linear falloff after that
            var ratio = (WindowMs - actualTimeMs) / (WindowMs * 0.8);
            return PrecisionScore + (ratio * (100.0 - PrecisionScore));
        }
    }
}
