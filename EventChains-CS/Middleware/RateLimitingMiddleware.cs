namespace EventChainsCore.Middleware
{
    /// <summary>
    /// Middleware that implements rate limiting for event execution.
    /// </summary>
    public static class RateLimitingMiddleware
    {
        /// <summary>
        /// Creates rate limiting middleware using a token bucket algorithm.
        /// </summary>
        /// <param name="maxRequests">Maximum number of requests allowed</param>
        /// <param name="timeWindowSeconds">Time window in seconds</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> Create(
            int maxRequests,
            int timeWindowSeconds)
        {
            var requestTimes = new Queue<DateTime>();
            var lockObject = new object();

            return next => (evt, ctx) =>
            {
                lock (lockObject)
                {
                    var now = DateTime.UtcNow;
                    var windowStart = now.AddSeconds(-timeWindowSeconds);

                    // Remove old requests outside the window
                    while (requestTimes.Count > 0 && requestTimes.Peek() < windowStart)
                    {
                        requestTimes.Dequeue();
                    }

                    // Check if rate limit exceeded
                    if (requestTimes.Count >= maxRequests)
                    {
                        return EventResult.CreateFailure(
                            evt.GetType().Name,
                            $"Rate limit exceeded: {maxRequests} requests per {timeWindowSeconds} seconds",
                            precisionScore: 0
                        );
                    }

                    // Add current request
                    requestTimes.Enqueue(now);
                }

                return next(evt, ctx);
            };
        }

        /// <summary>
        /// Creates per-user rate limiting middleware.
        /// </summary>
        /// <param name="maxRequests">Maximum requests per user</param>
        /// <param name="timeWindowSeconds">Time window in seconds</param>
        /// <param name="userIdContextKey">Context key for user ID</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> CreatePerUser(
            int maxRequests,
            int timeWindowSeconds,
            string userIdContextKey = "user_id")
        {
            var userRequestTimes = new Dictionary<string, Queue<DateTime>>();
            var lockObject = new object();

            return next => (evt, ctx) =>
            {
                if (!ctx.TryGet<string>(userIdContextKey, out var userId))
                {
                    userId = "anonymous";
                }

                lock (lockObject)
                {
                    if (!userRequestTimes.ContainsKey(userId))
                    {
                        userRequestTimes[userId] = new Queue<DateTime>();
                    }

                    var requestTimes = userRequestTimes[userId];
                    var now = DateTime.UtcNow;
                    var windowStart = now.AddSeconds(-timeWindowSeconds);

                    // Remove old requests
                    while (requestTimes.Count > 0 && requestTimes.Peek() < windowStart)
                    {
                        requestTimes.Dequeue();
                    }

                    // Check rate limit
                    if (requestTimes.Count >= maxRequests)
                    {
                        return EventResult.CreateFailure(
                            evt.GetType().Name,
                            $"Rate limit exceeded for user {userId}",
                            precisionScore: 0
                        );
                    }

                    requestTimes.Enqueue(now);
                }

                return next(evt, ctx);
            };
        }

        /// <summary>
        /// Creates rate limiting middleware with different limits per event type.
        /// </summary>
        /// <param name="limits">Dictionary of event type names to (max requests, time window seconds)</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> CreatePerEventType(
            Dictionary<string, (int maxRequests, int timeWindowSeconds)> limits)
        {
            var requestTimesPerType = new Dictionary<string, Queue<DateTime>>();
            var lockObject = new object();

            return next => (evt, ctx) =>
            {
                var eventTypeName = evt.GetType().Name;

                if (!limits.TryGetValue(eventTypeName, out var limit))
                {
                    // No limit for this event type
                    return next(evt, ctx);
                }

                lock (lockObject)
                {
                    if (!requestTimesPerType.ContainsKey(eventTypeName))
                    {
                        requestTimesPerType[eventTypeName] = new Queue<DateTime>();
                    }

                    var requestTimes = requestTimesPerType[eventTypeName];
                    var now = DateTime.UtcNow;
                    var windowStart = now.AddSeconds(-limit.timeWindowSeconds);

                    // Remove old requests
                    while (requestTimes.Count > 0 && requestTimes.Peek() < windowStart)
                    {
                        requestTimes.Dequeue();
                    }

                    // Check rate limit
                    if (requestTimes.Count >= limit.maxRequests)
                    {
                        return EventResult.CreateFailure(
                            eventTypeName,
                            $"Rate limit exceeded: {limit.maxRequests} requests per {limit.timeWindowSeconds} seconds",
                            precisionScore: 0
                        );
                    }

                    requestTimes.Enqueue(now);
                }

                return next(evt, ctx);
            };
        }
    }
}