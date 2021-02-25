using System;
using System.Collections.Generic;
using PtrJsn.FastCache.ReplacementPolicies;

namespace PtrJsn.FastCache
{
    /// <summary>
    /// Represents an N-way set associative in-memory cache.
    /// </summary>
    /// <typeparam name="TKey">The type of the key that uniquely identifies the data being cached.</typeparam>
    /// <typeparam name="TValue">The type of data being cached.</typeparam>
    public sealed class MemoryCache<TKey, TValue> : IFastCache<TKey, TValue>
    {
        /// <summary>
        /// The maximum allowed size of the cache, i.e. the maximum valid value of <see cref="CacheSize"/>.
        /// </summary>
        /// <remarks>
        /// <para>This is a limitation of .NET Core data structures.</para>
        /// </remarks>
        public const int MaximumCacheSize = 200_000_000;

        /// <summary>
        /// The actual cache of data. Each set is a dictionary, and all sets are collected in an array.
        /// </summary>
        private Dictionary<TKey, CacheItem<TKey, TValue>>[] _cache;
        private bool _initialized;

        /// <summary>
        /// Gets or sets the total number of items that can be cached.
        /// </summary>
        /// <remarks>
        /// <para>The default is 500.</para>
        /// <para>CacheSize divided by SetSize, rounded down, is the number of sets in the cache.</para>
        /// <para>Note that this is the size of the cache in items. It does not guarantee a limit on memory
        /// usage, which varies with <typeparamref name="TKey"/> and <typeparamref name="TValue"/>.</para>
        /// </remarks>
        public int CacheSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items that can be cached in each set in the cache.
        /// </summary>
        /// <remarks>
        /// <para>The default is 10.</para>
        /// <para>CacheSize divided by SetSize, rounded down, is the number of sets in the cache.</para>
        /// </remarks>
        public int SetSize { get; set; }

        /// <summary>
        /// Gets or sets the replacement policy to use.
        /// </summary>
        /// <remarks>
        /// <para>The default is <see cref="LeastRecentlyUsedReplacementPolicy{TKey,TValue}"/>.</para>
        /// <para>Note that this is the size of the cache in items. It does not guarantee a limit on memory usage.</para>
        /// </remarks>
        public IReplacementPolicy<TKey, TValue> ReplacementPolicy { get; set; }

        /// <summary>
        /// Gets or sets whether cache sets should be created during initialization.
        /// </summary>
        /// <remarks>
        /// <para>The default is <see langword="false"/>.</para>
        /// <para>By setting this to <see langword="true"/>, all the internal cache set data structures will be
        /// created when <see cref="Initialize"/> is called; otherwise, they will be created when needed by
        /// <see cref="Store"/>. Setting this may add time and memory usage to <see cref="Initialize"/>,
        /// possibly allocating memory that isn't needed, but <see cref="Store"/> calls will be faster.</para>
        /// <para>Consider setting this option when <see cref="CacheSize"/> is well within the amount of
        /// memory available to the cache.</para>
        /// </remarks>
        public bool CreateCacheSetsOnInitialize { get; set; }

        /// <summary>
        /// Initializes the cache using the configured properties, or defaults if not specified.
        /// </summary>
        public void Initialize()
        {
            // Initialize properties. This is done here instead of on class instantiation to give consumers
            // a chance to set the properties before they are set automatically.

            if (CacheSize == 0)
            {
                CacheSize = 500;
            }
            else if (CacheSize < 0 || CacheSize > MaximumCacheSize)
            {
                throw new InvalidOperationException($"CacheSize invalid. Valid values are 1 to {MaximumCacheSize}.");
            }

            if (SetSize == 0)
            {
                SetSize = 10;
            }
            else if (SetSize < 0 || SetSize > CacheSize)
            {
                throw new InvalidOperationException("CacheSize invalid. Valid values are 1 to CacheSize.");
            }

            InitializeCache();

            ReplacementPolicy ??= new LeastRecentlyUsedReplacementPolicy<TKey, TValue>();

            _initialized = true;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key,
        /// if the key is found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns><see langword="true"/> if the cache contains an element with the specified key;
        /// otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The cache hasn't been initialized.</exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (!_initialized)
            {
                throw new InvalidOperationException("The cache must be initialized before use.");
            }

            // The key's hash code determines in which set in the cache the value gets stored
            int hashCode = key.GetHashCode();
            Dictionary<TKey, CacheItem<TKey, TValue>> setDictionary = GetCacheSet(hashCode);

            bool keyFound = setDictionary.TryGetValue(key, out CacheItem<TKey, TValue> cacheItem);

            if (keyFound)
            {
                // The key was found, so update the item's metadata & return its value
                cacheItem.LastAccessedDate = DateTime.Now;
                cacheItem.HitCount++;

                value = cacheItem.Value;
            }
            else
            {
                value = default(TValue);
            }

            return keyFound;
        }

        /// <summary>
        /// Stores the specified key and value in the cache.
        /// </summary>
        /// <param name="key">The key of the value to store.</param>
        /// <param name="value">The value to store.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The cache hasn't been initialized.</exception>
        /// <remarks>
        /// <para><paramref name="value"/> is stored in the set associated with <paramref name="key"/>.
        /// If <paramref name="key"/> is already stored, it's replaced with <paramref name="value"/>.
        /// Otherwise, if there's an open slot in the set, it's stored in that slot. If it's not already
        /// stored and the set is full, <see cref="ReplacementPolicy"/> is applied and data stored in
        /// an existing slot is evicted and replaced with <paramref name="value"/>.</para>
        /// </remarks>
        public void Store(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (!_initialized)
            {
                throw new InvalidOperationException("The cache must be initialized before use.");
            }

            CacheItem<TKey, TValue> newCacheItem = new()
            {
                Key = key,
                Value = value,
                AddedDate = DateTime.Now,
                LastAccessedDate = DateTime.Now,
                HitCount = 0
            };

            // The key's hash code determines in which set in the cache the value gets stored
            int hashCode = key.GetHashCode();
            Dictionary<TKey, CacheItem<TKey, TValue>> setDictionary = GetCacheSet(hashCode);

            if (setDictionary.ContainsKey(key))
            {
                // We've already stored this key, so just update its value
                setDictionary[key] = newCacheItem;
            }
            else if (setDictionary.Count >= SetSize)
            {
                // This set is already full, so apply the replacement policy to determine
                // which key gets evicted.
                // Note that the setDictionary.Count should never exceed SetSize, but this is
                // cheap protection in case a bug is introduced in the future.
                TKey keyToReplace = ReplacementPolicy.GetItemToReplace(setDictionary.Values);
                setDictionary.Remove(keyToReplace);
                setDictionary[key] = newCacheItem;
            }
            else
            {
                // The key isn't already stored, and its set isn't full, so add it
                setDictionary[key] = newCacheItem;
            }
        }

        /// <summary>
        /// Initializes <see cref="_cache"/> to store the specified amount of data in sets.
        /// </summary>
        private void InitializeCache()
        {
            int setCount = GetSetCount();

            _cache = new Dictionary<TKey, CacheItem<TKey, TValue>>[setCount];

            if (CreateCacheSetsOnInitialize)
            {
                // Initialize each set in the cache
                for (int setIndex = 0; setIndex < setCount; setIndex++)
                {
                    InitializeCacheSet(setIndex);
                }
            }
        }

        /// <summary>
        /// Initializes a particular set in the cache.
        /// </summary>
        /// <param name="setIndex">The index in <see cref="_cache"/> of the set to initialize.</param>
        /// <returns>The initialized set.</returns>
        private Dictionary<TKey, CacheItem<TKey, TValue>> InitializeCacheSet(int setIndex)
        {
            _cache[setIndex] = new Dictionary<TKey, CacheItem<TKey, TValue>>(SetSize);

            return _cache[setIndex];
        }

        /// <summary>
        /// Gets the set where data associated with a hash code would be slotted.
        /// </summary>
        /// <param name="hashCode">The hash code for which a set is sought.</param>
        /// <returns>The cache set for <paramref name="hashCode"/>.</returns>
        private Dictionary<TKey, CacheItem<TKey, TValue>> GetCacheSet(int hashCode)
        {
            int setIndex = 0;
            int setCount = GetSetCount();

            // If there's only one set, we've already got the right index, so no need to do anything else
            if (setCount > 1)
            {
                bool setIdentifierFound = false;

                while (!setIdentifierFound)
                {
                    // See if hashCode belongs in the set corresponding to setIndex
                    int upperLimit = GetSetIdentifierUpperLimit(setIndex);

                    if (hashCode <= upperLimit)
                    {
                        // hashCode belongs in this set, so we're done
                        setIdentifierFound = true;
                    }
                    else
                    {
                        // The current setIndex isn't the set where hashCode belongs, so check the next set
                        setIndex++;
                    }
                }
            }

            // InitializeCacheSet should only be hit when CreateCacheSetsOnInitialize is false
            Dictionary<TKey, CacheItem<TKey, TValue>> cacheSet = _cache[setIndex] ?? InitializeCacheSet(setIndex);

            return cacheSet;
        }

        /// <summary>
        /// Calculates the upper limit of hash code values associated with a given set.
        /// </summary>
        /// <param name="setIndex">The array index of the set in <see cref="_cache"/> for which
        /// an upper limit is sought.</param>
        /// <returns>The highest hash code that can be stored in the set identified by <paramref name="setIndex"/>.</returns>
        private int GetSetIdentifierUpperLimit(int setIndex)
        {
            int setCount = GetSetCount();
            int upperLimit;

            if (setIndex == setCount - 1)
            {
                // We're looking at the last set, so return MaxValue. We can't use the
                // multiplier, as MaxValue won't be evenly divisible by any setCount,
                // so due to rounding, a hash code of MaxValue wouldn't get slotted.
                // If setCount is low, this would make the last set slightly larger than
                // the other sets.
                upperLimit = int.MaxValue;
            }
            else
            {
                int multiplier = int.MaxValue / setCount;

                upperLimit = (setIndex + 1) * multiplier;
            }

            return upperLimit;
        }

        /// <summary>
        /// Gets the total number of sets in the cache.
        /// </summary>
        private int GetSetCount()
        {
            return CacheSize / SetSize;
        }
    }
}
