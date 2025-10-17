namespace EventChainsCore
{
    /// <summary>
    /// Defines the contract for an event chain that orchestrates sequential execution
    /// of multiple chainable events with support for graduated precision tracking.
    /// </summary>
    public interface IEventChain
    {
        /// <summary>
        /// Executes all events in the chain sequentially through the configured middleware pipeline.
        /// Returns detailed ChainResult with individual event results and aggregated metrics.
        /// </summary>
        Task<ChainResult> ExecuteWithResultsAsync();

        /// <summary>
        /// Legacy method for backwards compatibility. Executes the chain and may throw on failure.
        /// </summary>
        Task ExecuteAsync();
    }
}