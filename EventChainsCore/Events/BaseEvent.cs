namespace EventChainsCore
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
}
