using System.Collections.Concurrent;

using Filin.Internal;

namespace Filin.Tests;

public class LRUCacheTests
{
    [Fact]
    public void Set_And_GetOrUpdate_ShouldReturnCachedValue()
    {
        // Arrange
        var cache = new LRUCache<string, string>(capacity: 3, ttl: TimeSpan.FromMinutes(5));

        // Act
        cache.Set("key1", "value1");
        var result = cache.GetOrUpdate("key1", _ => "new_value", (_, oldVal) => oldVal);

        // Assert
        Assert.Equal("value1", result);
    }

    [Fact]
    public void CapacityExceeded_ShouldEvictLeastRecentlyUsedItem()
    {
        // Arrange - емкость 2 элемента
        var cache = new LRUCache<int, string>(capacity: 2, ttl: TimeSpan.FromMinutes(5));
        cache.Set(1, "one");
        cache.Set(2, "two");

        // Act - вытесняет наименее используемый элемент (1)
        cache.Set(3, "three");

        // Assert
        var val1 = cache.GetOrUpdate(1, _ => "recreated_one", (_, oldVal) => oldVal);
        Assert.Equal("recreated_one", val1);
    }

    [Fact]
    public void GetOrUpdate_WhenKeyDoesNotExist_ShouldInvokeAddValue()
    {
        // Arrange - Setup cache with capacity of 5 and 1-minute TTL
        var cache = new LRUCache<string, string>(capacity: 5, ttl: TimeSpan.FromMinutes(1));
        var addValueInvoked = false;

        // Act - Call GetOrUpdate for a new key
        var result = cache.GetOrUpdate(
            key: "key1",
            addValue: k =>
            {
                addValueInvoked = true;
                return "value1";
            },
            updateValue: (k, oldVal) => "updatedValue"
        );

        // Assert - Verify that addValue was called and result stored
        Assert.True(addValueInvoked);
        Assert.Equal("value1", result);
    }

    [Fact]
    public void GetOrUpdate_WhenKeyExistsAndNotExpired_ShouldInvokeUpdateValueAndTouch()
    {
        // Arrange - Populate key in cache
        var cache = new LRUCache<string, string>(capacity: 5, ttl: TimeSpan.FromMinutes(1));
        cache.Set("key1", "initialValue");

        var updateValueInvoked = false;

        // Act - Request existing key before TTL expires
        var result = cache.GetOrUpdate(
            key: "key1",
            addValue: k => "newValue",
            updateValue: (k, oldVal) =>
            {
                updateValueInvoked = true;
                return oldVal + "_updated";
            }
        );

        // Assert - Verify that updateValue was executed instead of addValue
        Assert.True(updateValueInvoked);
        Assert.Equal("initialValue_updated", result);
    }

    [Fact]
    public void CapacityLimit_WhenExceeded_ShouldEvictLeastRecentlyUsedItem()
    {
        // Arrange - Create cache with capacity of 2 items
        var cache = new LRUCache<int, string>(capacity: 2, ttl: TimeSpan.FromMinutes(1));

        cache.Set(1, "one");
        cache.Set(2, "two");

        // Act - Touch key 1 to make key 2 the least recently used
        _ = cache.GetOrUpdate(1, k => "new", (k, v) => v);

        // Add key 3, which must evict key 2
        cache.Set(3, "three");

        // Assert - Key 2 should be evicted (addValue will be invoked for key 2 if requested)
        var key2Readded = false;
        var val2 = cache.GetOrUpdate(2, k =>
        {
            key2Readded = true;
            return "readded_two";
        }, (k, v) => v);

        Assert.True(key2Readded);
        Assert.Equal("readded_two", val2);
    }

    [Fact]
    public async Task TTL_WhenExpired_ShouldReinvokeAddValue()
    {
        // Arrange - Cache with short TTL (50 milliseconds)
        var cache = new LRUCache<string, string>(capacity: 5, ttl: TimeSpan.FromMilliseconds(50));
        cache.Set("key1", "initialValue");

        // Wait for TTL expiration
        await Task.Delay(100);

        var addValueInvoked = false;

        // Act - Request expired key
        var result = cache.GetOrUpdate(
            key: "key1",
            addValue: k =>
            {
                addValueInvoked = true;
                return "freshValue";
            },
            updateValue: (k, oldVal) => "staleUpdate"
        );

        // Assert - Verify addValue was invoked due to TTL expiration
        Assert.True(addValueInvoked);
        Assert.Equal("freshValue", result);
    }

    [Fact]
    public void Set_ShouldInsertOrUpdateValue()
    {
        // Arrange
        var cache = new LRUCache<string, int>(capacity: 3, ttl: TimeSpan.FromMinutes(1));

        // Act - Insert and update
        cache.Set("counter", 1);
        cache.Set("counter", 2);

        var result = cache.GetOrUpdate("counter", k => 0, (k, old) => old);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task Concurrency_ParallelAccess_ShouldBeThreadSafe()
    {
        // Arrange - Create small capacity cache accessed concurrently by multiple threads
        var cache = new LRUCache<int, int>(capacity: 50, ttl: TimeSpan.FromMinutes(1));
        var exceptions = new ConcurrentBag<Exception>();

        // Act - Run 100 parallel operations
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            try
            {
                var key = i % 20;
                cache.Set(key, i);
                _ = cache.GetOrUpdate(key, k => k * 10, (k, v) => v + 1);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert - Ensure no race conditions or corrupted internal lists occurred
        Assert.Empty(exceptions);
    }
}
