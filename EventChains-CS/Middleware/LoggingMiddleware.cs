namespace EventChainsCore.Middleware
{
    /// <summary>
    /// Middleware that logs event execution with timing and result information.
    /// </summary>
    public static class LoggingMiddleware
    {
        /// <summary>
        /// Creates logging middleware with the specified log action.
        /// </summary>
        /// <param name="logAction">Action to perform logging (event name, duration ms, success)</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> Create(
            Action<string, long, bool> logAction)
        {
            return next => (evt, ctx) =>
            {
                var eventName = evt.GetType().Name;
                var startTime = DateTime.UtcNow;

                try
                {
                    var result = next(evt, ctx);
                    var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    logAction(eventName, duration, result.Success);
                    return result;
                }
                catch (Exception ex)
                {
                    var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    logAction(eventName, duration, false);
                    throw;
                }
            };
        }

        /// <summary>
        /// Creates logging middleware with console output.
        /// </summary>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> Console()
        {
            return Create((eventName, duration, success) =>
            {
                var status = success ? "✓" : "✗";
                System.Console.WriteLine($"[{status}] {eventName} - {duration}ms");
            });
        }

        /// <summary>
        /// Creates detailed logging middleware with console output including precision scores.
        /// </summary>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> DetailedConsole()
        {
            return next => (evt, ctx) =>
            {
                var eventName = evt.GetType().Name;
                var startTime = DateTime.UtcNow;

                System.Console.WriteLine($"→ Starting {eventName}...");

                var result = next(evt, ctx);
                var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                var status = result.Success ? "✓" : "✗";
                System.Console.WriteLine($"[{status}] {eventName} - {duration}ms - Score: {result.PrecisionScore:F1}%");
                
                if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                {
                    System.Console.WriteLine($"  Error: {result.ErrorMessage}");
                }

                return result;
            };
        }
    }
}
