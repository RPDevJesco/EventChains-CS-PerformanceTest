namespace EventChainsCore.Middleware
{
    /// <summary>
    /// Middleware that enforces authentication and authorization checks.
    /// </summary>
    public static class AuthorizationMiddleware
    {
        /// <summary>
        /// Creates authorization middleware that checks if a user has required permissions.
        /// </summary>
        /// <param name="permissionChecker">Function to check permissions (context, event type) -> has permission</param>
        /// <param name="unauthorizedMessage">Message to return on authorization failure</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> Create(
            Func<IEventContext, Type, bool> permissionChecker,
            string unauthorizedMessage = "Unauthorized access")
        {
            return next => (evt, ctx) =>
            {
                var eventType = evt.GetType();

                if (!permissionChecker(ctx, eventType))
                {
                    return EventResult.CreateFailure(
                        eventType.Name,
                        unauthorizedMessage,
                        precisionScore: 0
                    );
                }

                return next(evt, ctx);
            };
        }

        /// <summary>
        /// Creates authentication middleware that ensures a user is authenticated.
        /// </summary>
        /// <param name="isAuthenticatedCheck">Function to check if user is authenticated</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> RequireAuthentication(
            Func<IEventContext, bool> isAuthenticatedCheck)
        {
            return next => (evt, ctx) =>
            {
                if (!isAuthenticatedCheck(ctx))
                {
                    return EventResult.CreateFailure(
                        evt.GetType().Name,
                        "Authentication required",
                        precisionScore: 0
                    );
                }

                return next(evt, ctx);
            };
        }

        /// <summary>
        /// Creates authorization middleware with role-based access control.
        /// </summary>
        /// <param name="requiredRole">The role required to execute events</param>
        /// <param name="contextRoleKey">The context key where the user's role is stored</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> RequireRole(
            string requiredRole,
            string contextRoleKey = "user_role")
        {
            return next => (evt, ctx) =>
            {
                if (!ctx.TryGet<string>(contextRoleKey, out var userRole) || userRole != requiredRole)
                {
                    return EventResult.CreateFailure(
                        evt.GetType().Name,
                        $"Requires role: {requiredRole}",
                        precisionScore: 0
                    );
                }

                return next(evt, ctx);
            };
        }

        /// <summary>
        /// Creates authorization middleware that allows events with specific attributes.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to check for</typeparam>
        /// <param name="validator">Function to validate the attribute</param>
        public static Func<EventChain.ExecuteDelegate, EventChain.ExecuteDelegate> RequireAttribute<TAttribute>(
            Func<TAttribute, IEventContext, bool> validator)
            where TAttribute : Attribute
        {
            return next => (evt, ctx) =>
            {
                var eventType = evt.GetType();
                var attribute = eventType.GetCustomAttributes(typeof(TAttribute), true)
                    .FirstOrDefault() as TAttribute;

                if (attribute == null)
                {
                    // No attribute means event doesn't require this authorization
                    return next(evt, ctx);
                }

                if (!validator(attribute, ctx))
                {
                    return EventResult.CreateFailure(
                        eventType.Name,
                        "Authorization check failed",
                        precisionScore: 0
                    );
                }

                return next(evt, ctx);
            };
        }
    }
}
