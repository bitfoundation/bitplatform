using Bit.BlazeDb;
using Bit.BlazeDb.Querying;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class StreamingQueryTests
{
    private static async Task<(BlazeDbDatabase Db, BlazeDbTable<Guid, TodoItem> Todos)> OpenAsync(int rows)
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(TodoItem.Table));
        var todos = db.GetTable(TodoItem.Table);
        for (var i = 0; i < rows; i++)
        {
            todos.Insert(new TodoItem
            {
                Id = Guid.NewGuid(),
                Title = "item " + i,
                Done = i % 2 == 0,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i),
            });
        }
        return (db, todos);
    }

    [TestMethod]
    public async Task Streams_Every_Row_Of_A_Full_Scan()
    {
        var (db, todos) = await OpenAsync(1000);
        await using var owned = db;

        var seen = 0;
        await foreach (var _ in BlazeDbQuery<Guid, TodoItem>.From(todos).ExecuteAsync(batchSize: 64))
        {
            seen++;
        }

        Assert.AreEqual(1000, seen);
    }

    [TestMethod]
    public async Task Streaming_Applies_Filters_Ordering_And_Paging()
    {
        var (db, todos) = await OpenAsync(100);
        await using var owned = db;

        var titles = await BlazeDbQuery<Guid, TodoItem>.From(todos)
            .Where(t => t.Done)
            .OrderBy(t => t.CreatedAt)
            .Skip(2)
            .Take(3)
            .ToListAsync(batchSize: 2);

        Assert.AreSequenceEqual(["item 4", "item 6", "item 8"], titles.Select(t => t.Title));
    }

    [TestMethod]
    public async Task Streaming_Projects_Without_Materializing_Rows()
    {
        var (db, todos) = await OpenAsync(10);
        await using var owned = db;

        var titles = new List<string>();
        await foreach (var title in BlazeDbQuery<Guid, TodoItem>.From(todos)
                           .Where(t => !t.Done)
                           .ExecuteAsync(t => t.Title, batchSize: 2))
        {
            titles.Add(title);
        }

        Assert.AreEqual(5, titles.Count);
        foreach (var t in titles) Assert.StartsWith("item ", t);
    }

    [TestMethod]
    public async Task Streaming_Yields_Control_Between_Batches()
    {
        var (db, todos) = await OpenAsync(50);
        await using var owned = db;

        // A batch size of 1 forces a yield per row, so a competing continuation gets to run
        // before the stream finishes - the property that keeps the UI responsive.
        var interleaved = false;
        var enumeration = Task.Run(async () =>
        {
            var index = 0;
            await foreach (var row in BlazeDbQuery<Guid, TodoItem>.From(todos).ExecuteAsync(batchSize: 1))
            {
                if (index++ == 10)
                {
                    _ = Task.Run(() => interleaved = true);
                    await Task.Delay(20);
                }
            }
        });

        await enumeration;
        Assert.IsTrue(interleaved);
    }

    [TestMethod]
    public async Task Streaming_Honors_Cancellation()
    {
        var (db, todos) = await OpenAsync(500);
        await using var owned = db;

        using var cts = new CancellationTokenSource();
        var seen = 0;

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in BlazeDbQuery<Guid, TodoItem>.From(todos).ExecuteAsync(batchSize: 8, cts.Token))
            {
                if (++seen == 20)
                {
                    cts.Cancel();
                }
            }
        });

        Assert.AreEqual(20, seen);
    }

    [TestMethod]
    public async Task Streaming_Rejects_A_Non_Positive_Batch_Size()
    {
        var (db, todos) = await OpenAsync(1);
        await using var owned = db;

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
        {
            await foreach (var _ in BlazeDbQuery<Guid, TodoItem>.From(todos).ExecuteAsync(batchSize: 0))
            {
            }
        });
    }
}
