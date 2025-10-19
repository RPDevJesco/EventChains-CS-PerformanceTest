namespace EventChainsCore.Middleware
{
    /// <summary>
    /// Middleware that caches event results to avoid redundant execution.
    /// </summary>
    public static class CachingMiddleware
    {
        /// <summary>
        /// Creates caching middleware with a simple in-memory cache.
        /// </summary>
        /// <param name="cacheKeyGenerator">Function to generate cache key from event and context</param>
        /// <param name="cacheStore">Dictionary to use as cache store</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> Create(
            Func<IChainableEvent, IEventContext, string> cacheKeyGenerator,
            Dictionary<string, EventResult> cacheStore)
        {
            return next => (evt, ctx) =>
            {
                var cacheKey = cacheKeyGenerator(evt, ctx);

                // Check cache
                if (cacheStore.TryGetValue(cacheKey, out var cachedResult))
                {
                    return cachedResult;
                }

                // Execute and cache
                var result = next(evt, ctx);
                cacheStore[cacheKey] = result;

                return result;
            };
        }

        /// <summary>
        /// Creates caching middleware with time-based expiration.
        /// </summary>
        /// <param name="cacheKeyGenerator">Function to generate cache key</param>
        /// <param name="expirationSeconds">Cache expiration time in seconds</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> CreateWithExpiration(
            Func<IChainableEvent, IEventContext, string> cacheKeyGenerator,
            int expirationSeconds = 300)
        {
            var cache = new Dictionary<string, (EventResult result, DateTime expiration)>();

            return next => (evt, ctx) =>
            {
                var cacheKey = cacheKeyGenerator(evt, ctx);
                var now = DateTime.UtcNow;

                // Check cache and expiration
                if (cache.TryGetValue(cacheKey, out var cached) && cached.expiration > now)
                {
                    return cached.result;
                }

                // Execute and cache with expiration
                var result = next(evt, ctx);
                cache[cacheKey] = (result, now.AddSeconds(expirationSeconds));

                return result;
            };
        }

        /// <summary>
        /// Creates caching middleware that only caches successful results.
        /// </summary>
        /// <param name="cacheKeyGenerator">Function to generate cache key</param>
        /// <param name="cacheStore">Dictionary to use as cache store</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> CreateSuccessOnly(
            Func<IChainableEvent, IEventContext, string> cacheKeyGenerator,
            Dictionary<string, EventResult> cacheStore)
        {
            return next => (evt, ctx) =>
            {
                var cacheKey = cacheKeyGenerator(evt, ctx);

                // Check cache
                if (cacheStore.TryGetValue(cacheKey, out var cachedResult))
                {
                    return cachedResult;
                }

                // Execute
                var result = next(evt, ctx);

                // Only cache successful results
                if (result.Success)
                {
                    cacheStore[cacheKey] = result;
                }

                return result;
            };
        }

        /// <summary>
        /// Simple cache key generator based on event type name.
        /// </summary>
        public static string SimpleKeyGenerator(IChainableEvent evt, IEventContext ctx)
        {
            return evt.GetType().Name;
        }

        /// <summary>
        /// Cache key generator that includes a context value.
        /// </summary>
        public static Func<IChainableEvent, IEventContext, string> ContextKeyGenerator(string contextKey)
        {
            return (evt, ctx) =>
            {
                var contextValue = ctx.TryGet<object>(contextKey, out var value) ? value?.ToString() : "null";
                return $"{evt.GetType().Name}:{contextValue}";
            };
        }
    }
}
