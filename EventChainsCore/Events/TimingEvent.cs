namespace EventChainsCore
{
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
}
