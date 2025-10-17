namespace EventChains.Core
{
    /// <summary>
    /// Provides a concrete implementation of the event context with enhanced features
    /// for graduated precision systems and game mechanics.
    /// </summary>
    public class EventContext : IEventContext
    {
        private readonly Dictionary<string, object> _data = new();

        public T Get<T>(string key) => (T)_data[key];

        public void Set<T>(string key, T value) => _data[key] = value!;

        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var obj))
            {
                try
                {
                    value = (T)obj;
                    return true;
                }
                catch (InvalidCastException)
                {
                    value = default!;
                    return false;
                }
            }
            value = default!;
            return false;
        }

        public bool ContainsKey(string key) => _data.ContainsKey(key);

        public T GetOrDefault<T>(string key, T defaultValue = default!)
        {
            if (TryGet<T>(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Increments a numeric value in the context.
        /// Creates the key with initialValue if it doesn't exist.
        /// </summary>
        public void Increment<T>(string key, T amount, T initialValue = default) where T : struct, IComparable<T>
        {
            if (!_data.ContainsKey(key))
            {
                _data[key] = initialValue;
            }

            dynamic current = _data[key];
            dynamic delta = amount;
            _data[key] = current + delta;
        }

        /// <summary>
        /// Appends an item to a list in the context.
        /// Creates the list if it doesn't exist.
        /// </summary>
        public void Append<T>(string key, T item)
        {
            if (!_data.ContainsKey(key))
            {
                _data[key] = new List<T>();
            }

            if (_data[key] is List<T> list)
            {
                list.Add(item);
            }
            else
            {
                throw new InvalidOperationException($"Key '{key}' exists but is not a List<{typeof(T).Name}>");
            }
        }

        /// <summary>
        /// Updates a value only if the new value is better according to the comparer.
        /// If no comparer is provided, uses default comparison.
        /// </summary>
        public void UpdateIfBetter<T>(string key, T value, IComparer<T>? comparer = null)
        {
            comparer ??= Comparer<T>.Default;

            if (!_data.ContainsKey(key))
            {
                _data[key] = value!;
                return;
            }

            var current = (T)_data[key];
            if (comparer.Compare(value, current) > 0)
            {
                _data[key] = value!;
            }
        }

        /// <summary>
        /// Gets all keys currently stored in the context.
        /// Useful for debugging and inspection.
        /// </summary>
        public IEnumerable<string> GetAllKeys() => _data.Keys;

        /// <summary>
        /// Clears all data from the context.
        /// </summary>
        public void Clear() => _data.Clear();

        /// <summary>
        /// Creates a shallow copy of the current context.
        /// Useful for branching chains or saving state.
        /// </summary>
        public EventContext Clone()
        {
            var clone = new EventContext();
            foreach (var kvp in _data)
            {
                clone._data[kvp.Key] = kvp.Value;
            }
            return clone;
        }
    }
}
