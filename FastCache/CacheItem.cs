using System;

namespace PtrJsn.FastCache
{
    /// <summary>
    /// Data to be cached, its key, and its metadata.
    /// </summary>
    /// <typeparam name="TKey">The type of the key that uniquely identifies the data being cached.</typeparam>
    /// <typeparam name="TValue">The type of data being cached.</typeparam>
    public sealed class CacheItem<TKey, TValue>
    {
        /// <summary>
        /// The unique identifier of the data being cached.
        /// </summary>
        public TKey Key { get; set; }

        /// <summary>
        /// The data being cached.
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// The date and time this item was stored in the cache.
        /// </summary>
        public DateTime AddedDate { get; set; }

        /// <summary>
        /// The date and time this item was last retrieved from the cache.
        /// </summary>
        public DateTime LastAccessedDate { get; set; }

        /// <summary>
        /// The number of times this item has been retrieved from the cache.
        /// </summary>
        public int HitCount { get; set; }
    }
}