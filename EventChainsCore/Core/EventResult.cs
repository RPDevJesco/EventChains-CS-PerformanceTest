namespace EventChainsCore
{
    /// <summary>
    /// Represents the result of an event execution with detailed information
    /// about success, failures, and graduated precision scoring.
    /// </summary>
    public class EventResult
    {
        /// <summary>
        /// Indicates whether the event executed successfully.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Optional error message if the event failed.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Optional data payload from the event execution.
        /// Can contain precision scores, intermediate results, etc.
        /// </summary>
        public object? Data { get; private set; }

        /// <summary>
        /// Precision score for graduated success scenarios (0-100).
        /// 100 = perfect execution, 0 = complete failure.
        /// Used in layered precision systems like QTEs.
        /// </summary>
        public double PrecisionScore { get; private set; }

        /// <summary>
        /// The name of the event that produced this result.
        /// Used for debugging and graduated success tracking.
        /// </summary>
        public string EventName { get; private set; }

        private EventResult(string eventName)
        {
            EventName = eventName;
        }

        /// <summary>
        /// Creates a successful result with optional data and precision score.
        /// </summary>
        public static EventResult CreateSuccess(string eventName, object? data = null, double precisionScore = 100.0)
        {
            return new EventResult(eventName)
            {
                Success = true,
                Data = data,
                PrecisionScore = Math.Clamp(precisionScore, 0, 100)
            };
        }

        /// <summary>
        /// Creates a failure result with an error message and optional precision score.
        /// Even failures can have partial precision scores in graduated systems.
        /// </summary>
        public static EventResult CreateFailure(string eventName, string errorMessage, double precisionScore = 0.0)
        {
            return new EventResult(eventName)
            {
                Success = false,
                ErrorMessage = errorMessage,
                PrecisionScore = Math.Clamp(precisionScore, 0, 100)
            };
        }

        /// <summary>
        /// Creates a partial success result - event didn't fully succeed but made progress.
        /// Useful for graduated precision systems where hitting outer rings counts.
        /// </summary>
        public static EventResult CreatePartialSuccess(string eventName, string message, double precisionScore, object? data = null)
        {
            return new EventResult(eventName)
            {
                Success = true,
                ErrorMessage = message,
                Data = data,
                PrecisionScore = Math.Clamp(precisionScore, 0, 100)
            };
        }
    }
}