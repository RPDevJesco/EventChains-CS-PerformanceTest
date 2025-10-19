namespace EventChainsCore.Middleware
{
    /// <summary>
    /// Middleware that performs validation checks before event execution.
    /// </summary>
    public static class ValidationMiddleware
    {
        /// <summary>
        /// Creates validation middleware with a custom validator function.
        /// </summary>
        /// <param name="validator">Function to validate (event, context) -> (is valid, error message)</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> Create(
            Func<IChainableEvent, IEventContext, (bool isValid, string? errorMessage)> validator)
        {
            return next => (evt, ctx) =>
            {
                var (isValid, errorMessage) = validator(evt, ctx);

                if (!isValid)
                {
                    return EventResult.CreateFailure(
                        evt.GetType().Name,
                        errorMessage ?? "Validation failed",
                        precisionScore: 0
                    );
                }

                return next(evt, ctx);
            };
        }

        /// <summary>
        /// Creates validation middleware that checks for required context values.
        /// </summary>
        /// <param name="requiredKeys">Keys that must exist in context</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> RequireContextKeys(
            params string[] requiredKeys)
        {
            return next => (evt, ctx) =>
            {
                var missingKeys = requiredKeys.Where(key => !ctx.ContainsKey(key)).ToList();

                if (missingKeys.Any())
                {
                    return EventResult.CreateFailure(
                        evt.GetType().Name,
                        $"Missing required context keys: {string.Join(", ", missingKeys)}",
                        precisionScore: 0
                    );
                }

                return next(evt, ctx);
            };
        }

        /// <summary>
        /// Creates validation middleware that checks context value types.
        /// </summary>
        /// <param name="typeRequirements">Dictionary of context keys to required types</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> ValidateContextTypes(
            Dictionary<string, Type> typeRequirements)
        {
            return next => (evt, ctx) =>
            {
                foreach (var requirement in typeRequirements)
                {
                    if (!ctx.TryGet<object>(requirement.Key, out var value))
                    {
                        return EventResult.CreateFailure(
                            evt.GetType().Name,
                            $"Missing required context key: {requirement.Key}",
                            precisionScore: 0
                        );
                    }

                    if (value != null && !requirement.Value.IsAssignableFrom(value.GetType()))
                    {
                        return EventResult.CreateFailure(
                            evt.GetType().Name,
                            $"Context key '{requirement.Key}' has wrong type. Expected: {requirement.Value.Name}, Got: {value.GetType().Name}",
                            precisionScore: 0
                        );
                    }
                }

                return next(evt, ctx);
            };
        }

        /// <summary>
        /// Creates validation middleware that enforces invariants before and after execution.
        /// </summary>
        /// <param name="preCondition">Condition that must be true before execution</param>
        /// <param name="postCondition">Condition that must be true after execution</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> EnforceInvariants(
            Func<IEventContext, (bool isValid, string? errorMessage)> preCondition,
            Func<IEventContext, EventResult, (bool isValid, string? errorMessage)>? postCondition = null)
        {
            return next => (evt, ctx) =>
            {
                // Check pre-condition
                var (preValid, preError) = preCondition(ctx);
                if (!preValid)
                {
                    return EventResult.CreateFailure(
                        evt.GetType().Name,
                        $"Pre-condition failed: {preError}",
                        precisionScore: 0
                    );
                }

                // Execute event
                var result = next(evt, ctx);

                // Check post-condition if provided
                if (postCondition != null)
                {
                    var (postValid, postError) = postCondition(ctx, result);
                    if (!postValid)
                    {
                        return EventResult.CreateFailure(
                            evt.GetType().Name,
                            $"Post-condition failed: {postError}",
                            precisionScore: 0
                        );
                    }
                }

                return result;
            };
        }

        /// <summary>
        /// Creates validation middleware that checks business rules.
        /// </summary>
        /// <param name="rules">List of business rule validators</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> ValidateBusinessRules(
            params Func<IEventContext, (bool isValid, string? errorMessage)>[] rules)
        {
            return next => (evt, ctx) =>
            {
                foreach (var rule in rules)
                {
                    var (isValid, errorMessage) = rule(ctx);
                    if (!isValid)
                    {
                        return EventResult.CreateFailure(
                            evt.GetType().Name,
                            errorMessage ?? "Business rule validation failed",
                            precisionScore: 0
                        );
                    }
                }

                return next(evt, ctx);
            };
        }
    }
}