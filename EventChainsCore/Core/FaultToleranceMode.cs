namespace EventChainsCore
{
    /// <summary>
    /// Defines fault tolerance modes for event chain execution.
    /// These modes determine how the chain handles event failures.
    /// </summary>
    public enum FaultToleranceMode
    {
        /// <summary>
        /// STRICT: Any event failure stops the chain immediately.
        /// Use for critical workflows where partial completion is unacceptable.
        /// Example: Financial transactions, authentication flows.
        /// </summary>
        Strict,

        /// <summary>
        /// LENIENT: Non-critical failures are logged but chain continues.
        /// Use for workflows where some steps are optional.
        /// Example: Analytics tracking in an order process.
        /// </summary>
        Lenient,

        /// <summary>
        /// BEST_EFFORT: All events are attempted, failures are collected.
        /// Use for scenarios where graduated success matters (e.g., layered precision QTE).
        /// Example: QTE with nested precision rings, batch notifications.
        /// </summary>
        BestEffort,

        /// <summary>
        /// CUSTOM: User-defined logic determines whether to continue after each failure.
        /// Use for complex scenarios with nuanced error handling.
        /// Example: Multi-tenant processing where some tenant failures are acceptable.
        /// </summary>
        Custom
    }
}