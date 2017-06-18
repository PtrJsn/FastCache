using System.Collections.Generic;

namespace PtrJsn.FastCache.ReplacementPolicies
{
    /// <summary>
    /// Defines a least recently used (LRU) cache replacement policy.
    /// </summary>
    /// <typeparam name="TKey">The type of the key that uniquely identifies the data being cached.</typeparam>
    /// <typeparam name="TValue">The type of data being cached.</typeparam>
    public sealed class LeastRecentlyUsedReplacementPolicy<TKey, TValue> : IReplacementPolicy<TKey, TValue>
    {
        /// <summary>
        /// Determines which item is the next to be evicted.
        /// </summary>
        /// <param name="cacheSet">The cache set to examine.</param>
        /// <returns>The key of the item to evict next, or <see langword="null"/> if
        /// <paramref name="cacheSet"/> was null or empty.</returns>
        /// <remarks>
        /// <para>This method applies the policy to return the item with the earliest
        /// <see cref="CacheItem{TKey,TValue}.LastAccessedDate"/>. If two items were
        /// last accessed at the same time, and that is the earliest time of the items
        /// in <paramref name="cacheSet"/>, the first one found gets returned.
        /// Consumers shouldn't make any assumptions about which item this is; for
        /// example, it may not correspond to the first one stored in the cache.</para>
        /// </remarks>
        public TKey GetItemToReplace(ICollection<CacheItem<TKey, TValue>> cacheSet)
        {
            CacheItem<TKey, TValue> replaceCandidate = null;

            if (cacheSet != null)
            {
                foreach (CacheItem<TKey, TValue> item in cacheSet)
                {
                    if (replaceCandidate == null)
                    {
                        // No candidate yet, so use the first one to start
                        replaceCandidate = item;
                    }
                    else if (item.LastAccessedDate < replaceCandidate.LastAccessedDate)
                    {
                        // The current item was used less recently than the candidate,
                        // so it becomes the new candidate
                        replaceCandidate = item;
                    }
                }
            }

            return replaceCandidate == null ? default(TKey) : replaceCandidate.Key;
        }
    }
}