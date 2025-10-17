namespace EventChainsCore
{
    /// <summary>
    /// Represents the aggregated results of an entire event chain execution.
    /// Contains individual event results, total precision score, and execution metadata.
    /// </summary>
    public class ChainResult
    {
        /// <summary>
        /// Indicates whether the entire chain executed successfully.
        /// Definition of "success" depends on FaultToleranceMode.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Collection of individual event results in execution order.
        /// </summary>
        public List<EventResult> EventResults { get; set; } = new();

        /// <summary>
        /// Aggregated precision score across all events (0-100).
        /// Calculated as weighted average of individual event precision scores.
        /// Useful for graduated success systems.
        /// </summary>
        public double TotalPrecisionScore { get; set; }

        /// <summary>
        /// Number of events that succeeded.
        /// </summary>
        public int SuccessCount => EventResults.Count(r => r.Success);

        /// <summary>
        /// Number of events that failed.
        /// </summary>
        public int FailureCount => EventResults.Count(r => !r.Success);

        /// <summary>
        /// Total number of events attempted.
        /// </summary>
        public int TotalCount => EventResults.Count;

        /// <summary>
        /// The final context state after chain execution.
        /// Contains all accumulated data from events.
        /// </summary>
        public IEventContext Context { get; set; }

        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Calculates a grade based on total precision score.
        /// Useful for player feedback in game systems.
        /// </summary>
        public string GetGrade()
        {
            return TotalPrecisionScore switch
            {
                >= 95 => "S",
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
        }

        public ChainResult(IEventContext context)
        {
            Context = context;
        }
    }
}