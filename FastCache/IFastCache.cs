using PtrJsn.FastCache.ReplacementPolicies;

namespace PtrJsn.FastCache
{
    /// <summary>
    /// Represents an N-way set associative cache.
    /// </summary>
    /// <typeparam name="TKey">The type of the key that uniquely identifies the data being cached.</typeparam>
    /// <typeparam name="TValue">The type of data being cached.</typeparam>
    public interface IFastCache<TKey, TValue>
    {
        /// <summary>
        /// Gets or sets the total number of items that can be cached.
        /// </summary>
        /// <remarks>
        /// <para>The default is 500.</para>
        /// <para>Note that this is the size of the cache in items. It does not guarantee a limit on memory
        /// usage, which varies with <typeparamref name="TKey"/> and <typeparamref name="TValue"/>.</para>
        /// </remarks>
        int CacheSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items that can be cached in each set in the cache.
        /// </summary>
        /// <remarks>
        /// <para>The default is 10.</para>
        /// </remarks>
        int SetSize { get; set; }

        /// <summary>
        /// Gets or sets the replacement policy to use.
        /// </summary>
        /// <remarks>
        /// <para>The default is <see cref="LeastRecentlyUsedReplacementPolicy{TKey,TValue}"/>.</para>
        /// <para>Note that this is the size of the cache in items. It does not guarantee a limit on memory usage.</para>
        /// </remarks>
        IReplacementPolicy<TKey, TValue> ReplacementPolicy { get; set; }

        /// <summary>
        /// Initializes the cache using the configured properties, or defaults if not specified.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key,
        /// if the key is found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns><see langword="true" /> if the cache contains an element with the specified key;
        /// otherwise, <see langword="false" />.</returns>
        bool TryGetValue(TKey key, out TValue value);

        /// <summary>
        /// Stores the specified key and value in the cache.
        /// </summary>
        /// <param name="key">The key of the value to store.</param>
        /// <param name="value">The value to store.</param>
        void Store(TKey key, TValue value);
    }
}