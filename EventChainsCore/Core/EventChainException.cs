namespace EventChainsCore
{
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