using System.Collections.Generic;

namespace PtrJsn.FastCache.ReplacementPolicies
{
    /// <summary>
    /// Identifies a cache replacement policy.
    /// </summary>
    /// <typeparam name="TKey">The type of the key that uniquely identifies the data being cached.</typeparam>
    /// <typeparam name="TValue">The type of data being cached.</typeparam>
    public interface IReplacementPolicy<TKey, TValue>
    {
        /// <summary>
        /// Determines which item is the next to be evicted.
        /// </summary>
        /// <param name="cacheSet">The cache set to examine.</param>
        /// <returns>The key of the item to evict next.</returns>
        /// <remarks>
        /// <para>This method should not make any changes to <paramref name="cacheSet"/>.</para>
        /// </remarks>
        TKey GetItemToReplace(ICollection<CacheItem<TKey, TValue>> cacheSet);
    }
}