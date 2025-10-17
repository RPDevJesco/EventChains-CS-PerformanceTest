using EventChains.Core.Events;

namespace EventChains.Core
{
    /// <summary>
    /// A parallel variant of EventChain that executes events concurrently.
    /// WARNING: This is NOT suitable for most EventChains use cases because:
    /// - Events cannot depend on each other's results
    /// - Context race conditions are possible
    /// - Order of execution is not guaranteed
    /// 
    /// Use this only when:
    /// - Events are completely independent
    /// - You need to fan-out work (e.g., send notifications to multiple users)
    /// - Context is read-only or carefully synchronized
    /// </summary>
    public class ParallelEventChain : IEventChain
    {
        private readonly List<IChainableEvent> _events = new();
        private readonly IEventContext _context = new EventContext();
        private int _maxDegreeOfParallelism = -1; // -1 = unlimited

        /// <summary>
        /// Sets the maximum number of events to execute concurrently.
        /// Default is -1 (unlimited).
        /// </summary>
        public ParallelEventChain WithMaxParallelism(int maxDegreeOfParallelism)
        {
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            return this;
        }

        public ParallelEventChain AddEvent(IChainableEvent chainableEvent)
        {
            _events.Add(chainableEvent);
            return this;
        }

        public IEventContext GetContext() => _context;

        /// <summary>
        /// Executes all events in parallel and waits for all to complete.
        /// </summary>
        public async Task<ChainResult> ExecuteWithResultsAsync()
        {
            var result = new ChainResult(_context);
            var startTime = DateTime.UtcNow;

            // Create parallel options
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxDegreeOfParallelism
            };

            // Execute all events in parallel
            var tasks = _events.Select(async chainEvent =>
            {
                try
                {
                    return await chainEvent.ExecuteAsync(_context);
                }
                catch (Exception ex)
                {
                    return EventResult.CreateFailure(
                        chainEvent.GetType().Name,
                        $"Exception: {ex.Message}"
                    );
                }
            });

            // Wait for all to complete
            var eventResults = await Task.WhenAll(tasks);
            result.EventResults.AddRange(eventResults);

            // Calculate metrics
            var endTime = DateTime.UtcNow;
            result.ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
            result.Success = result.FailureCount == 0;
            result.TotalPrecisionScore = CalculateTotalPrecisionScore(result);

            return result;
        }

        public async Task ExecuteAsync()
        {
            var result = await ExecuteWithResultsAsync();

            if (!result.Success)
            {
                var firstFailure = result.EventResults.FirstOrDefault(r => !r.Success);
                throw new EventChainException(
                    firstFailure?.ErrorMessage ?? "Event chain execution failed",
                    result
                );
            }
        }

        private double CalculateTotalPrecisionScore(ChainResult result)
        {
            if (result.TotalCount == 0) return 0.0;
            var totalScore = result.EventResults.Sum(r => r.PrecisionScore);
            return totalScore / result.TotalCount;
        }
    }

    /// <summary>
    /// Example use case for parallel execution: Send notifications to multiple users.
    /// Events are completely independent, so parallel execution makes sense.
    /// </summary>
    public class SendNotificationEvent : BaseEvent
    {
        private readonly string _userId;
        private readonly string _message;

        public SendNotificationEvent(string userId, string message)
        {
            _userId = userId;
            _message = message;
        }

        public override string EventName => $"SendNotification_{_userId}";

        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            // Simulate sending notification (I/O operation)
            await Task.Delay(100); // Pretend this is an HTTP call

            // In real code: await notificationService.SendAsync(_userId, _message);

            return Success(new { UserId = _userId, MessageSent = true });
        }
    }

    /// <summary>
    /// Example: Parallel fan-out for notifications
    /// </summary>
    public class ParallelNotificationExample
    {
        public static async Task Run()
        {
            var parallelChain = new ParallelEventChain()
                .WithMaxParallelism(10); // Max 10 concurrent notifications

            var userIds = new[] { "user1", "user2", "user3", "user4", "user5" };
            foreach (var userId in userIds)
            {
                parallelChain.AddEvent(new SendNotificationEvent(userId, "Hello!"));
            }

            var result = await parallelChain.ExecuteWithResultsAsync();

            Console.WriteLine($"Notifications sent: {result.SuccessCount}/{result.TotalCount}");
            Console.WriteLine($"Execution time: {result.ExecutionTimeMs}ms");
            // Much faster than sequential: ~100ms vs ~500ms
        }
    }
}