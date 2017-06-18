using System;
using System.Reflection;
using System.Threading;
using PtrJsn.FastCache;
using PtrJsn.FastCache.ReplacementPolicies;

namespace PtrJsn.FastCacheConsumer
{
    /// <summary>
    /// Sample class that illustrates consumption of FastCache.
    /// </summary>
    internal class Program
    {
        private static void Main()
        {
            RunTest(RunInt32StringTest);
            RunTest(RunStringStringTest);
            RunTest(RunLruTest);
            RunTest(RunMruTest);
            RunTest(RunSetSize1Test);
            RunTest(RunSetCount1Test);
            RunTest(RunCacheSizeMaxSetSize1Test);

            Console.ReadLine();
        }

        private static void RunTest(Action testMethod)
        {
            Console.WriteLine(testMethod.GetMethodInfo().Name);
            Console.WriteLine();
            testMethod();
            Console.WriteLine();
        }

        private static void RunInt32StringTest()
        {
            // Use the default configuration
            IFastCache<int, string> cache = new MemoryCache<int, string>();

            cache.Initialize();

            // Try to get a value from an empty cache
            bool valueFound = cache.TryGetValue(1, out string value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");

            cache.Store(1, "Hartnell");

            // Try to get the value we just stored
            valueFound = cache.TryGetValue(1, out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
        }

        private static void RunStringStringTest()
        {
            MemoryCache<string, string> cache = new MemoryCache<string, string>
            {
                CacheSize = 10,
                SetSize = 2,
                CreateCacheSetsOnInitialize = true
            };

            cache.Initialize();

            // Try to get a value from an empty cache
            bool valueFound = cache.TryGetValue("Hartnell", out string value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");

            cache.Store("Hartnell", "Bill");

            // Try to get the value we just stored
            valueFound = cache.TryGetValue("Hartnell", out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
        }

        private static void RunLruTest()
        {
            IFastCache<int, string> cache = new MemoryCache<int, string>
            {
                CacheSize = 4,
                SetSize = 2
            };

            cache.Initialize();

            // Add more items than the cache can hold, making sure they span sets
            StoreAndGetMultipleValues(cache);
        }

        private static void RunMruTest()
        {
            IFastCache<int, string> cache = new MemoryCache<int, string>
            {
                CacheSize = 4,
                SetSize = 2,
                ReplacementPolicy = new MostRecentlyUsedReplacementPolicy<int, string>()
            };

            cache.Initialize();

            // Add more items than the cache can hold, making sure they span sets
            StoreAndGetMultipleValues(cache);
        }

        private static void RunSetSize1Test()
        {
            IFastCache<int, string> cache = new MemoryCache<int, string>
            {
                CacheSize = 4,
                SetSize = 1
            };

            cache.Initialize();

            StoreAndGetMultipleValues(cache);
        }

        private static void RunSetCount1Test()
        {
            IFastCache<int, string> cache = new MemoryCache<int, string>
            {
                CacheSize = 3,
                SetSize = 3
            };

            cache.Initialize();

            StoreAndGetMultipleValues(cache);
        }

        private static void RunCacheSizeMaxSetSize1Test()
        {
            IFastCache<int, string> cache = new MemoryCache<int, string>
            {
                CacheSize = MemoryCache<int, string>.MaximumCacheSize,
                SetSize = 1
            };

            cache.Initialize();

            cache.Store(10000, "Hartnell");
            cache.Store(20000, "Troughton");
            cache.Store(30000, "Pertwee");
            cache.Store(40000, "Baker");
            cache.Store(50000, "Davison");

            // Try to get each item stored to see what got evicted
            bool valueFound = cache.TryGetValue(10000, out string value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
            valueFound = cache.TryGetValue(20000, out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
            valueFound = cache.TryGetValue(30000, out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
            valueFound = cache.TryGetValue(40000, out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
            valueFound = cache.TryGetValue(50000, out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
        }

        private static void StoreAndGetMultipleValues(IFastCache<int, string> cache)
        {
            // Store items with delays to ensure different timestamps
            cache.Store(1, "Hartnell");
            Thread.Sleep(50);
            cache.Store(2, "Troughton");
            Thread.Sleep(50);
            cache.Store(1073742003, "Pertwee");
            Thread.Sleep(50);
            cache.Store(1073742004, "Baker");
            Thread.Sleep(50);
            cache.Store(5, "Davison");

            // Try to get each item stored to see what got evicted
            bool valueFound = cache.TryGetValue(1, out string value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
            valueFound = cache.TryGetValue(2, out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
            valueFound = cache.TryGetValue(1073742003, out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
            valueFound = cache.TryGetValue(1073742004, out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
            valueFound = cache.TryGetValue(5, out value);
            Console.WriteLine($"valueFound = [{valueFound}], value = [{value}]");
        }
    }
}