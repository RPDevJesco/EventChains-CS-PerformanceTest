namespace EventChainsCore
{
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
}
