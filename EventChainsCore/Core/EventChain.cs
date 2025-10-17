namespace EventChainsCore
{
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

                FaultToleranceMode.Lenient => result.TotalCount == 0 || result.SuccessCount > 0,

                FaultToleranceMode.BestEffort => result.TotalCount > 0,
                FaultToleranceMode.Custom => _context.GetOrDefault("CustomSuccessCriteria", true),
                _ => result.FailureCount == 0
            };
        }

        private double CalculateTotalPrecisionScore(ChainResult result)
        {
            if (result.TotalCount == 0) return 100.0;

            // Weighted average of individual precision scores
            var totalScore = result.EventResults.Sum(r => r.PrecisionScore);
            return totalScore / result.TotalCount;
        }
    }
}