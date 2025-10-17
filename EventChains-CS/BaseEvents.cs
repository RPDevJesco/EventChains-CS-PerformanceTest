namespace EventChains.Core.Events
{
    /// <summary>
    /// Base class for events that provides common functionality and naming.
    /// </summary>
    public abstract class BaseEvent : IChainableEvent
    {
        /// <summary>
        /// Gets the name of this event, used for result tracking and debugging.
        /// Defaults to the class name but can be overridden.
        /// </summary>
        public virtual string EventName => GetType().Name;

        /// <summary>
        /// Executes the event logic and returns a result.
        /// </summary>
        public abstract Task<EventResult> ExecuteAsync(IEventContext context);

        /// <summary>
        /// Helper method to create a success result for this event.
        /// </summary>
        protected EventResult Success(object? data = null, double precisionScore = 100.0)
        {
            return EventResult.CreateSuccess(EventName, data, precisionScore);
        }

        /// <summary>
        /// Helper method to create a failure result for this event.
        /// </summary>
        protected EventResult Failure(string message, double precisionScore = 0.0)
        {
            return EventResult.CreateFailure(EventName, message, precisionScore);
        }

        /// <summary>
        /// Helper method to create a partial success result for this event.
        /// </summary>
        protected EventResult PartialSuccess(string message, double precisionScore, object? data = null)
        {
            return EventResult.CreatePartialSuccess(EventName, message, precisionScore, data);
        }
    }

    /// <summary>
    /// Base class for timing-based events with precision windows.
    /// Perfect for QTE systems with graduated precision.
    /// </summary>
    public abstract class TimingEvent : BaseEvent
    {
        /// <summary>
        /// The timing window in milliseconds. Events within this window succeed.
        /// </summary>
        public double WindowMs { get; set; }

        /// <summary>
        /// The precision score awarded for hitting within the window.
        /// </summary>
        public double PrecisionScore { get; set; }

        /// <summary>
        /// Optional effect name or identifier for game logic.
        /// </summary>
        public string? Effect { get; set; }

        protected TimingEvent(double windowMs, double precisionScore, string? effect = null)
        {
            WindowMs = windowMs;
            PrecisionScore = precisionScore;
            Effect = effect;
        }

        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask; // Allow for async subclass implementations

            var elapsed = context.GetOrDefault<double>("elapsed_time_ms");
            var inputTime = context.GetOrDefault<double?>("input_time_ms");

            if (inputTime.HasValue && inputTime.Value <= WindowMs)
            {
                // Hit within window!
                var hitData = new
                {
                    WindowMs,
                    ActualTimeMs = inputTime.Value,
                    Effect,
                    Precision = CalculatePrecisionWithinWindow(inputTime.Value)
                };

                // Update context with cumulative scoring
                context.Increment("total_score", PrecisionScore, 0.0);

                if (Effect != null)
                {
                    context.UpdateIfBetter("best_effect", Effect, StringComparer.Ordinal);
                }

                return Success(hitData, CalculatePrecisionWithinWindow(inputTime.Value));
            }

            // Missed this window
            return Failure($"Missed {EventName} ({WindowMs}ms window)", 0.0);
        }

        /// <summary>
        /// Calculates precision score within the window (100 at 0ms, scaling down to configured score at window edge).
        /// Override for custom precision curves.
        /// </summary>
        protected virtual double CalculatePrecisionWithinWindow(double actualTimeMs)
        {
            // Linear interpolation: perfect at 0ms, configured score at window edge
            var ratio = 1.0 - (actualTimeMs / WindowMs);
            return PrecisionScore + (ratio * (100.0 - PrecisionScore));
        }
    }

    /// <summary>
    /// Base class for layered precision events - like concentric QTE rings.
    /// Each layer represents a different precision threshold with different rewards.
    /// </summary>
    public abstract class LayeredPrecisionEvent : BaseEvent
    {
        /// <summary>
        /// Configuration for a single precision layer.
        /// </summary>
        public class PrecisionLayer
        {
            public string Name { get; set; } = "";
            public double WindowMs { get; set; }
            public double Score { get; set; }
            public string? Effect { get; set; }
            public object? AdditionalData { get; set; }
        }

        protected List<PrecisionLayer> Layers { get; set; } = new();

        /// <summary>
        /// Adds a precision layer. Call from constructor or initialization.
        /// Layers should be added from outermost (easiest) to innermost (hardest).
        /// </summary>
        protected void AddLayer(string name, double windowMs, double score, string? effect = null, object? data = null)
        {
            Layers.Add(new PrecisionLayer
            {
                Name = name,
                WindowMs = windowMs,
                Score = score,
                Effect = effect,
                AdditionalData = data
            });
        }

        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var inputTime = context.GetOrDefault<double?>("input_time_ms");

            if (!inputTime.HasValue)
            {
                return Failure("No input detected");
            }

            // Find the best (innermost) layer that was hit
            PrecisionLayer? bestLayer = null;
            foreach (var layer in Layers.OrderBy(l => l.WindowMs))
            {
                if (inputTime.Value <= layer.WindowMs)
                {
                    bestLayer = layer;
                }
            }

            if (bestLayer == null)
            {
                return Failure($"Missed all layers (input at {inputTime.Value}ms)", 0.0);
            }

            // Hit! Update context with best result
            context.Increment("total_score", bestLayer.Score, 0.0);

            if (bestLayer.Effect != null)
            {
                context.Set("best_effect", bestLayer.Effect);
            }

            var hitData = new
            {
                Layer = bestLayer.Name,
                WindowMs = bestLayer.WindowMs,
                ActualTimeMs = inputTime.Value,
                Effect = bestLayer.Effect,
                LayersHit = Layers.Count(l => inputTime.Value <= l.WindowMs)
            };

            // Calculate precision score based on how many layers were hit
            var layersHit = Layers.Count(l => inputTime.Value <= l.WindowMs);
            var precisionScore = (layersHit / (double)Layers.Count) * 100.0;

            return Success(hitData, precisionScore);
        }
    }

    /// <summary>
    /// A conditional event that only executes if a context condition is met.
    /// Useful for branching logic in chains.
    /// </summary>
    public class ConditionalEvent : BaseEvent
    {
        private readonly Func<IEventContext, bool> _condition;
        private readonly IChainableEvent _innerEvent;
        private readonly string _conditionDescription;

        public ConditionalEvent(
            Func<IEventContext, bool> condition,
            IChainableEvent innerEvent,
            string conditionDescription = "condition")
        {
            _condition = condition;
            _innerEvent = innerEvent;
            _conditionDescription = conditionDescription;
        }

        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            if (_condition(context))
            {
                return await _innerEvent.ExecuteAsync(context);
            }

            // Condition not met - skip this event
            return Success(new { Skipped = true, Reason = $"Condition '{_conditionDescription}' not met" }, 100.0);
        }
    }

    /// <summary>
    /// Executes a sub-chain as a single event.
    /// Useful for composing complex behaviors from simpler chains.
    /// </summary>
    public class SubChainEvent : BaseEvent
    {
        private readonly EventChain _subChain;

        public SubChainEvent(EventChain subChain, string? name = null)
        {
            _subChain = subChain;
            if (name != null)
            {
                _subChainName = name;
            }
        }

        private string? _subChainName;
        public override string EventName => _subChainName ?? base.EventName;

        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            var result = await _subChain.ExecuteWithResultsAsync();

            if (result.Success)
            {
                return Success(new
                {
                    SubChainResult = result,
                    EventsExecuted = result.TotalCount,
                    Precision = result.TotalPrecisionScore
                }, result.TotalPrecisionScore);
            }

            return Failure($"Sub-chain failed: {result.FailureCount} failures", result.TotalPrecisionScore);
        }
    }

    /// <summary>
    /// A validation event that checks context conditions.
    /// </summary>
    public class ValidationEvent : BaseEvent
    {
        private readonly Func<IEventContext, (bool isValid, string? errorMessage)> _validator;

        public ValidationEvent(
            Func<IEventContext, (bool, string?)> validator,
            string? name = null)
        {
            _validator = validator;
            if (name != null)
            {
                _validationName = name;
            }
        }

        private string? _validationName;
        public override string EventName => _validationName ?? base.EventName;

        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var (isValid, errorMessage) = _validator(context);

            if (isValid)
            {
                return Success(new { ValidationPassed = true });
            }

            return Failure(errorMessage ?? "Validation failed");
        }
    }
}
