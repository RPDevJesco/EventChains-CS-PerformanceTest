namespace EventChainsCore.Middleware
{
    /// <summary>
    /// Middleware that provides centralized error handling and recovery.
    /// </summary>
    public static class ErrorHandlingMiddleware
    {
        /// <summary>
        /// Creates error handling middleware with a custom error handler.
        /// </summary>
        /// <param name="errorHandler">Function to handle errors (exception, event) -> EventResult</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> Create(
            Func<Exception, IChainableEvent, EventResult> errorHandler)
        {
            return next => (evt, ctx) =>
            {
                try
                {
                    return next(evt, ctx);
                }
                catch (Exception ex)
                {
                    return errorHandler(ex, evt);
                }
            };
        }

        /// <summary>
        /// Creates error handling middleware that logs exceptions and returns failure results.
        /// </summary>
        /// <param name="logAction">Action to log errors</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> CreateWithLogging(
            Action<string, Exception> logAction)
        {
            return next => (evt, ctx) =>
            {
                try
                {
                    return next(evt, ctx);
                }
                catch (Exception ex)
                {
                    var eventName = evt.GetType().Name;
                    logAction(eventName, ex);

                    return EventResult.CreateFailure(
                        eventName,
                        $"Exception: {ex.Message}",
                        precisionScore: 0
                    );
                }
            };
        }

        /// <summary>
        /// Creates error handling middleware with retry logic.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="delayMs">Delay between retries in milliseconds</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> CreateWithRetry(
            int maxRetries = 3,
            int delayMs = 100)
        {
            return next => (evt, ctx) =>
            {
                Exception? lastException = null;
                
                for (int attempt = 0; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        return next(evt, ctx);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        
                        if (attempt < maxRetries)
                        {
                            Thread.Sleep(delayMs);
                        }
                    }
                }

                // All retries failed
                var eventName = evt.GetType().Name;
                return EventResult.CreateFailure(
                    eventName,
                    $"Failed after {maxRetries} retries: {lastException?.Message}",
                    precisionScore: 0
                );
            };
        }
    }
}
