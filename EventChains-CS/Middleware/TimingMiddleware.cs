namespace EventChainsCore.Middleware
{
    /// <summary>
    /// Middleware that tracks execution timing and stores metrics in context.
    /// </summary>
    public static class TimingMiddleware
    {
        /// <summary>
        /// Creates timing middleware that stores timing data in the context.
        /// Timing data is stored in a dictionary at context key "event_timings".
        /// </summary>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> Create()
        {
            return next => (evt, ctx) =>
            {
                var eventName = evt.GetType().Name;
                var startTime = DateTime.UtcNow;

                var result = next(evt, ctx);

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Store timing in context
                if (!ctx.TryGet<Dictionary<string, double>>("event_timings", out var timings))
                {
                    timings = new Dictionary<string, double>();
                    ctx.Set("event_timings", timings);
                }

                timings[eventName] = duration;

                return result;
            };
        }

        /// <summary>
        /// Creates timing middleware with a threshold alert action.
        /// </summary>
        /// <param name="thresholdMs">Threshold in milliseconds</param>
        /// <param name="alertAction">Action to call when threshold is exceeded</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> CreateWithThreshold(
            long thresholdMs,
            Action<string, long> alertAction)
        {
            return next => (evt, ctx) =>
            {
                var eventName = evt.GetType().Name;
                var startTime = DateTime.UtcNow;

                var result = next(evt, ctx);

                var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (duration > thresholdMs)
                {
                    alertAction(eventName, duration);
                }

                // Store timing in context
                if (!ctx.TryGet<Dictionary<string, double>>("event_timings", out var timings))
                {
                    timings = new Dictionary<string, double>();
                    ctx.Set("event_timings", timings);
                }

                timings[eventName] = duration;

                return result;
            };
        }
    }
}