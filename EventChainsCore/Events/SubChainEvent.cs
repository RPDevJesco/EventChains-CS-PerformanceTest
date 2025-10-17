namespace EventChainsCore
{
    /// <summary>
    /// Executes a sub-chain as a single event.
    /// Useful for composing complex behaviors from simpler chains.
    /// </summary>
    public class SubChainEvent : BaseEvent
    {
        private readonly EventChain _subChain;

        public SubChainEvent(EventChain subChain, string? name = null)
        {
            _subChain = subChain;
            if (name != null)
            {
                _subChainName = name;
            }
        }

        private string? _subChainName;
        public override string EventName => _subChainName ?? base.EventName;

        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            var result = await _subChain.ExecuteWithResultsAsync();

            if (result.Success)
            {
                return Success(new
                {
                    SubChainResult = result,
                    EventsExecuted = result.TotalCount,
                    Precision = result.TotalPrecisionScore
                }, result.TotalPrecisionScore);
            }

            return Failure($"Sub-chain failed: {result.FailureCount} failures", result.TotalPrecisionScore);
        }
    }
}
