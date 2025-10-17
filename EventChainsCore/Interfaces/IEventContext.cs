namespace EventChainsCore
{
    /// <summary>
    /// Defines the contract for a shared context object that enables data communication
    /// between events in a chain with enhanced type safety and convenience methods.
    /// </summary>
    public interface IEventContext
    {
        /// <summary>
        /// Retrieves a strongly-typed value from the context using the specified key.
        /// </summary>
        T Get<T>(string key);

        /// <summary>
        /// Stores a strongly-typed value in the context with the specified key.
        /// </summary>
        void Set<T>(string key, T value);

        /// <summary>
        /// Attempts to retrieve a strongly-typed value from the context.
        /// Returns true if successful, false otherwise.
        /// </summary>
        bool TryGet<T>(string key, out T value);

        /// <summary>
        /// Checks whether the context contains a value for the specified key.
        /// </summary>
        bool ContainsKey(string key);

        /// <summary>
        /// Gets a value from the context, or returns a default value if the key doesn't exist.
        /// </summary>
        T GetOrDefault<T>(string key, T defaultValue = default);

        /// <summary>
        /// Increments a numeric value in the context. Creates the key with initialValue if it doesn't exist.
        /// Useful for tracking scores, counters, etc. in graduated precision systems.
        /// </summary>
        void Increment<T>(string key, T amount, T initialValue = default) where T : struct, IComparable<T>;

        /// <summary>
        /// Appends an item to a list in the context. Creates the list if it doesn't exist.
        /// Useful for collecting results from multiple events.
        /// </summary>
        void Append<T>(string key, T item);

        /// <summary>
        /// Updates a value only if the new value is better according to the comparer.
        /// Useful for tracking "best" results in graduated precision systems.
        /// </summary>
        void UpdateIfBetter<T>(string key, T value, IComparer<T>? comparer = null);
    }
}
