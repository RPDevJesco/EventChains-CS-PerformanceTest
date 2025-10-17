namespace EventChains.Core
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

    /// <summary>
    /// Implements an event chain that orchestrates sequential execution of chainable events
    /// through a configurable middleware pipeline with support for graduated precision and
    /// flexible fault tolerance modes.
    /// </summary>
    public class EventChain : IEventChain
    {
        private readonly List<IChainableEvent> _events = new();
        private readonly List<Func<ExecuteDelegate, ExecuteDelegate>> _middlewares = new();
        private readonly IEventContext _context = new EventContext();
        private FaultToleranceMode _faultToleranceMode = FaultToleranceMode.Strict;
        private Func<EventResult, IEventContext, bool>? _customContinuationLogic;

        /// <summary>
        /// Delegate for the execution pipeline.
        /// </summary>
        public delegate Task<EventResult> ExecuteDelegate(IChainableEvent evt, IEventContext ctx);

        /// <summary>
        /// Gets or sets the fault tolerance mode for this chain.
        /// </summary>
        public FaultToleranceMode FaultToleranceMode
        {
            get => _faultToleranceMode;
            set => _faultToleranceMode = value;
        }

        /// <summary>
        /// Creates a new event chain with STRICT fault tolerance (default).
        /// Any event failure stops the chain immediately.
        /// </summary>
        public static EventChain Strict()
        {
            return new EventChain { FaultToleranceMode = FaultToleranceMode.Strict };
        }

        /// <summary>
        /// Creates a new event chain with LENIENT fault tolerance.
        /// Non-critical failures are logged but chain continues.
        /// </summary>
        public static EventChain Lenient()
        {
            return new EventChain { FaultToleranceMode = FaultToleranceMode.Lenient };
        }

        /// <summary>
        /// Creates a new event chain with BEST_EFFORT fault tolerance.
        /// All events are attempted, failures are collected.
        /// Perfect for graduated precision systems like layered QTEs.
        /// </summary>
        public static EventChain BestEffort()
        {
            return new EventChain { FaultToleranceMode = FaultToleranceMode.BestEffort };
        }

        /// <summary>
        /// Creates a new event chain with CUSTOM fault tolerance.
        /// Continuation logic is provided via callback.
        /// </summary>
        public static EventChain Custom(Func<EventResult, IEventContext, bool> continuationLogic)
        {
            return new EventChain
            {
                FaultToleranceMode = FaultToleranceMode.Custom,
                _customContinuationLogic = continuationLogic
            };
        }

        /// <summary>
        /// Adds a chainable event to the end of the event chain.
        /// </summary>
        public EventChain AddEvent(IChainableEvent chainableEvent)
        {
            _events.Add(chainableEvent);
            return this;
        }

        /// <summary>
        /// Registers middleware to the chain's execution pipeline.
        /// Middleware executes in reverse order of registration (LIFO).
        /// </summary>
        public EventChain UseMiddleware(Func<ExecuteDelegate, ExecuteDelegate> middleware)
        {
            _middlewares.Add(middleware);
            return this;
        }

        /// <summary>
        /// Retrieves the shared context instance for this event chain.
        /// </summary>
        public IEventContext GetContext()
        {
            return _context;
        }

        /// <summary>
        /// Executes all events in the chain with graduated precision tracking.
        /// Returns a ChainResult containing individual event results and aggregated metrics.
        /// </summary>
        public async Task<ChainResult> ExecuteWithResultsAsync()
        {
            var result = new ChainResult(_context);
            var startTime = DateTime.UtcNow;

            // Build the middleware pipeline
            ExecuteDelegate pipeline = async (evt, ctx) =>
            {
                return await evt.ExecuteAsync(ctx);
            };

            for (int i = _middlewares.Count - 1; i >= 0; i--)
            {
                pipeline = _middlewares[i](pipeline);
            }

            // Execute each event through the pipeline
            bool shouldContinue = true;
            foreach (var chainEvent in _events)
            {
                if (!shouldContinue) break;

                try
                {
                    var eventResult = await pipeline(chainEvent, _context);
                    result.EventResults.Add(eventResult);

                    // Determine if we should continue based on fault tolerance mode
                    if (!eventResult.Success)
                    {
                        shouldContinue = ShouldContinueAfterFailure(eventResult);
                    }
                }
                catch (Exception ex)
                {
                    var errorResult = EventResult.CreateFailure(
                        chainEvent.GetType().Name,
                        $"Exception: {ex.Message}"
                    );
                    result.EventResults.Add(errorResult);

                    shouldContinue = ShouldContinueAfterFailure(errorResult);
                }
            }

            // Calculate aggregated metrics
            var endTime = DateTime.UtcNow;
            result.ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
            result.Success = DetermineOverallSuccess(result);
            result.TotalPrecisionScore = CalculateTotalPrecisionScore(result);

            return result;
        }

        /// <summary>
        /// Legacy method for backwards compatibility.
        /// Executes the chain and throws on failure in STRICT mode.
        /// </summary>
        public async Task ExecuteAsync()
        {
            var result = await ExecuteWithResultsAsync();

            if (!result.Success && _faultToleranceMode == FaultToleranceMode.Strict)
            {
                var firstFailure = result.EventResults.FirstOrDefault(r => !r.Success);
                throw new EventChainException(
                    firstFailure?.ErrorMessage ?? "Event chain execution failed",
                    result
                );
            }
        }

        private bool ShouldContinueAfterFailure(EventResult eventResult)
        {
            return _faultToleranceMode switch
            {
                FaultToleranceMode.Strict => false,
                FaultToleranceMode.Lenient => true,
                FaultToleranceMode.BestEffort => true,
                FaultToleranceMode.Custom => _customContinuationLogic?.Invoke(eventResult, _context) ?? false,
                _ => false
            };
        }

        private bool DetermineOverallSuccess(ChainResult result)
        {
            return _faultToleranceMode switch
            {
                FaultToleranceMode.Strict => result.FailureCount == 0,
                FaultToleranceMode.Lenient => result.SuccessCount > 0,
                FaultToleranceMode.BestEffort => result.TotalCount > 0, // Always true if any events executed
                FaultToleranceMode.Custom => _context.GetOrDefault("CustomSuccessCriteria", true),
                _ => result.FailureCount == 0
            };
        }

        private double CalculateTotalPrecisionScore(ChainResult result)
        {
            if (result.TotalCount == 0) return 0.0;

            // Weighted average of individual precision scores
            var totalScore = result.EventResults.Sum(r => r.PrecisionScore);
            return totalScore / result.TotalCount;
        }
    }

    /// <summary>
    /// Exception thrown when an event chain execution fails.
    /// Contains the full ChainResult for detailed error analysis.
    /// </summary>
    public class EventChainException : Exception
    {
        public ChainResult Result { get; }

        public EventChainException(string message, ChainResult result)
            : base(message)
        {
            Result = result;
        }
    }
}
