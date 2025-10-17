namespace EventChainsCore
{
    /// <summary>
    /// Defines the contract for an event that can be chained together with other events
    /// in an event processing pipeline. Now returns EventResult for graduated precision.
    /// </summary>
    public interface IChainableEvent
    {
        /// <summary>
        /// Executes the event's logic asynchronously with access to the shared event context.
        /// Returns an EventResult that can indicate graduated success with precision scoring.
        /// </summary>
        /// <param name="context">
        /// The shared context object that allows this event to retrieve data set by previous
        /// events in the chain and to set data for subsequent events to consume.
        /// </param>
        /// <returns>
        /// An EventResult indicating success/failure, precision score, and optional data.
        /// This enables graduated success systems where events can partially succeed.
        /// </returns>
        Task<EventResult> ExecuteAsync(IEventContext context);
    }
}
