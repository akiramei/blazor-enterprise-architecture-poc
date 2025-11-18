using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Domain.Idempotency;
using Shared.Infrastructure.Platform.Stores;

namespace Shared.Infrastructure.UnitTests;

/// <summary>
/// InMemoryIdempotencyStore の単体テスト
/// 特に、2つの異なるインターフェース経由のメソッド呼び出しで
/// 一貫性が保たれることを検証します
/// </summary>
public class InMemoryIdempotencyStoreTests
{
    private readonly InMemoryIdempotencyStore _store;
    private readonly ILogger<InMemoryIdempotencyStore> _logger;

    public InMemoryIdempotencyStoreTests()
    {
        _logger = Substitute.For<ILogger<InMemoryIdempotencyStore>>();
        _store = new InMemoryIdempotencyStore(_logger);
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task IsProcessedAsync_ShouldReturnFalse_WhenRequestNotProcessed()
    {
        // Arrange
        var requestId = "test-request-1";

        // Act
        var result = await _store.IsProcessedAsync(requestId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldMarkRequestAsProcessed()
    {
        // Arrange
        var requestId = "test-request-2";
        var result = new { Status = "Success", Value = 42 };

        // Act
        await _store.MarkAsProcessedAsync(requestId, result);
        var isProcessed = await _store.IsProcessedAsync(requestId);

        // Assert
        isProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task GetResultAsync_ShouldReturnNull_WhenRequestNotProcessed()
    {
        // Arrange
        var requestId = "test-request-3";

        // Act
        var result = await _store.GetResultAsync(requestId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetResultAsync_ShouldReturnResult_WhenRequestProcessed()
    {
        // Arrange
        var requestId = "test-request-4";
        var expectedResult = new { Status = "Success", Value = 100 };

        // Act
        await _store.MarkAsProcessedAsync(requestId, expectedResult);
        var result = await _store.GetResultAsync(requestId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>(); // Result is stored as JSON string
    }

    [Fact]
    public async Task SaveAsync_ShouldSaveIdempotencyRecord()
    {
        // Arrange
        var key = "test-key-1";
        var record = IdempotencyRecord.Create(key, "TestCommand", new { Data = "test" });

        // Act
        await _store.SaveAsync(record);
        var retrieved = await _store.GetAsync(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Key.Should().Be(key);
        retrieved.CommandType.Should().Be("TestCommand");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyNotFound()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var result = await _store.GetAsync(key);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Mixed Interface Call Ordering Tests (Consistency Verification)

    [Fact]
    public async Task MixedCalls_SaveAsync_Then_IsProcessedAsync_ShouldBeConsistent()
    {
        // Arrange
        var key = "mixed-test-1";
        var record = IdempotencyRecord.Create(key, "Command1", new { Result = "data" });

        // Act - Save via Shared.Application.Interfaces.IIdempotencyStore
        await _store.SaveAsync(record);

        // Assert - Check via Shared.Abstractions.Platform.IIdempotencyStore
        var isProcessed = await _store.IsProcessedAsync(key);
        isProcessed.Should().BeTrue("SaveAsync should make the key visible to IsProcessedAsync");
    }

    [Fact]
    public async Task MixedCalls_MarkAsProcessedAsync_Then_GetAsync_ShouldBeConsistent()
    {
        // Arrange
        var key = "mixed-test-2";
        var result = new { Status = "Completed", Id = 123 };

        // Act - Mark via Shared.Abstractions.Platform.IIdempotencyStore
        await _store.MarkAsProcessedAsync(key, result);

        // Assert - Get via Shared.Application.Interfaces.IIdempotencyStore
        var record = await _store.GetAsync(key);
        record.Should().NotBeNull("MarkAsProcessedAsync should make the record visible to GetAsync");
        record!.Key.Should().Be(key);
        record.CommandType.Should().Be("Legacy", "MarkAsProcessedAsync uses Legacy as default CommandType");
    }

    [Fact]
    public async Task MixedCalls_SaveAsync_Then_GetResultAsync_ShouldBeConsistent()
    {
        // Arrange
        var key = "mixed-test-3";
        var testData = new { Value = "test-data", Count = 42 };
        var record = IdempotencyRecord.Create(key, "TestCommand", testData);

        // Act - Save via Shared.Application.Interfaces.IIdempotencyStore
        await _store.SaveAsync(record);

        // Assert - Get result via Shared.Abstractions.Platform.IIdempotencyStore
        var result = await _store.GetResultAsync(key);
        result.Should().NotBeNull("SaveAsync should make the result visible to GetResultAsync");
        result.Should().BeOfType<string>("Result is stored as JSON string");
    }

    [Fact]
    public async Task MixedCalls_MarkAsProcessedAsync_Then_IsProcessedAsync_ShouldBeConsistent()
    {
        // Arrange
        var key = "mixed-test-4";

        // Act - Mark via Shared.Abstractions.Platform.IIdempotencyStore
        await _store.MarkAsProcessedAsync(key, null);

        // Assert - Check via same interface
        var isProcessed = await _store.IsProcessedAsync(key);
        isProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task MixedCalls_MultipleOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var key = "mixed-test-5";
        var initialData = new { Phase = "Initial", Value = 1 };
        var updatedData = new { Phase = "Updated", Value = 2 };

        // Act & Assert - Multiple operations in sequence

        // 1. Save via Application interface
        var record1 = IdempotencyRecord.Create(key, "Command1", initialData);
        await _store.SaveAsync(record1);

        // 2. Verify via Platform interface
        var isProcessed1 = await _store.IsProcessedAsync(key);
        isProcessed1.Should().BeTrue();

        // 3. Get via Application interface
        var retrieved1 = await _store.GetAsync(key);
        retrieved1.Should().NotBeNull();
        retrieved1!.CommandType.Should().Be("Command1");

        // 4. Update via Platform interface
        await _store.MarkAsProcessedAsync(key, updatedData);

        // 5. Verify update via Application interface
        var retrieved2 = await _store.GetAsync(key);
        retrieved2.Should().NotBeNull();
        retrieved2!.CommandType.Should().Be("Legacy", "MarkAsProcessedAsync overwrites with Legacy CommandType");

        // 6. Get result via Platform interface
        var result = await _store.GetResultAsync(key);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task MixedCalls_SameKey_DifferentMethods_ShouldAccessSameRecord()
    {
        // Arrange
        var key = "consistency-test-1";
        var testResult = new { Message = "test", Code = 200 };

        // Act
        await _store.MarkAsProcessedAsync(key, testResult);

        // Assert - All methods should see the same record
        var isProcessed = await _store.IsProcessedAsync(key);
        var result = await _store.GetResultAsync(key);
        var record = await _store.GetAsync(key);

        isProcessed.Should().BeTrue();
        result.Should().NotBeNull();
        record.Should().NotBeNull();
        record!.Key.Should().Be(key);
    }

    [Fact]
    public async Task MixedCalls_SaveThenMark_ShouldOverwriteWithSameKey()
    {
        // Arrange
        var key = "overwrite-test-1";
        var initialRecord = IdempotencyRecord.Create(key, "InitialCommand", new { Value = 1 });
        var newResult = new { Value = 2 };

        // Act
        await _store.SaveAsync(initialRecord);
        await _store.MarkAsProcessedAsync(key, newResult);

        // Assert - The latest write should win
        var record = await _store.GetAsync(key);
        record.Should().NotBeNull();
        record!.CommandType.Should().Be("Legacy", "MarkAsProcessedAsync should have overwritten the record");
    }

    #endregion

    #region CleanupExpiredAsync Tests

    [Fact]
    public async Task CleanupExpiredAsync_ShouldRemoveExpiredRecords()
    {
        // Arrange
        var oldKey = "old-record";
        var newKey = "new-record";

        // Create an old record by manipulating the creation time
        // Note: Since IdempotencyRecord.Create sets CreatedAt to DateTime.UtcNow,
        // we need to save it first, then cleanup with very short expiration
        var oldRecord = IdempotencyRecord.Create(oldKey, "OldCommand", new { Data = "old" });
        await _store.SaveAsync(oldRecord);

        // Wait a bit to ensure time difference
        await Task.Delay(100);

        var newRecord = IdempotencyRecord.Create(newKey, "NewCommand", new { Data = "new" });
        await _store.SaveAsync(newRecord);

        // Act - Cleanup with very short expiration (should remove old record)
        await _store.CleanupExpiredAsync(TimeSpan.FromMilliseconds(50));

        // Assert
        var oldRetrieved = await _store.GetAsync(oldKey);
        var newRetrieved = await _store.GetAsync(newKey);

        oldRetrieved.Should().BeNull("Old record should be removed");
        newRetrieved.Should().NotBeNull("New record should still exist");
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldNotRemoveRecentRecords()
    {
        // Arrange
        var key = "recent-record";
        var record = IdempotencyRecord.Create(key, "RecentCommand", new { Data = "recent" });
        await _store.SaveAsync(record);

        // Act - Cleanup with long expiration (should not remove anything)
        await _store.CleanupExpiredAsync(TimeSpan.FromHours(1));

        // Assert
        var retrieved = await _store.GetAsync(key);
        retrieved.Should().NotBeNull("Recent record should not be removed");
    }

    [Fact]
    public async Task CleanupExpiredAsync_WithMixedRecordSources_ShouldCleanupBoth()
    {
        // Arrange
        var key1 = "platform-record";
        var key2 = "application-record";

        // Save via different interfaces
        await _store.MarkAsProcessedAsync(key1, new { Data = 1 });
        await Task.Delay(100);

        var record2 = IdempotencyRecord.Create(key2, "Command", new { Data = 2 });
        await _store.SaveAsync(record2);

        // Act - Cleanup with short expiration
        await _store.CleanupExpiredAsync(TimeSpan.FromMilliseconds(50));

        // Assert - Old record should be removed regardless of which interface was used
        var retrieved1 = await _store.GetAsync(key1);
        var retrieved2 = await _store.GetAsync(key2);

        retrieved1.Should().BeNull("Old platform record should be removed");
        retrieved2.Should().NotBeNull("Recent application record should remain");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task MarkAsProcessedAsync_WithNullResult_ShouldStoreSuccessfully()
    {
        // Arrange
        var key = "null-result-test";

        // Act
        await _store.MarkAsProcessedAsync(key, null);

        // Assert
        var isProcessed = await _store.IsProcessedAsync(key);
        var record = await _store.GetAsync(key);

        isProcessed.Should().BeTrue();
        record.Should().NotBeNull();
        record!.ResultJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ParallelAccess_ShouldMaintainConsistency()
    {
        // Arrange
        var tasks = new List<Task>();
        var keyPrefix = "parallel-test-";

        // Act - Simulate parallel access
        for (int i = 0; i < 10; i++)
        {
            var key = $"{keyPrefix}{i}";
            var record = IdempotencyRecord.Create(key, $"Command{i}", new { Index = i });

            tasks.Add(Task.Run(async () =>
            {
                await _store.SaveAsync(record);
                await _store.IsProcessedAsync(key);
                await _store.GetAsync(key);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All records should be accessible
        for (int i = 0; i < 10; i++)
        {
            var key = $"{keyPrefix}{i}";
            var isProcessed = await _store.IsProcessedAsync(key);
            var retrieved = await _store.GetAsync(key);

            isProcessed.Should().BeTrue();
            retrieved.Should().NotBeNull();
            retrieved!.CommandType.Should().Be($"Command{i}");
        }
    }

    #endregion
}
