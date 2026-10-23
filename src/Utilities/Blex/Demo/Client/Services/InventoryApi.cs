namespace Bit.Blex.Demo.Client.Services;

/// <summary>One product, as the simulated inventory server returns it.</summary>
public sealed record Product(int Id, string Name, int Stock);

/// <summary>
/// A stand-in for a remote inventory API, so the queries page has a server to cache: every call takes a
/// moment, counts itself, and can be told to fail.
/// </summary>
public sealed class InventoryApi
{
    private static readonly TimeSpan Latency = TimeSpan.FromMilliseconds(700);

    private readonly List<Product> _products =
    [
        new(1, "Keyboard", 12),
        new(2, "Mouse", 30),
        new(3, "Monitor", 4),
    ];

    private int _nextId = 4;

    /// <summary>How many requests reached the server, reads and writes alike.</summary>
    public int Requests { get; private set; }

    /// <summary>When set, the next read fails.</summary>
    public bool FailNextRead { get; set; }

    /// <summary>When set, writes are rejected.</summary>
    public bool RejectWrites { get; set; }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        Requests++;
        await Task.Delay(Latency, cancellationToken);
        if (FailNextRead)
        {
            FailNextRead = false;
            throw new HttpRequestException("The inventory server is unavailable (simulated).");
        }

        return [.. _products];
    }

    public async Task<Product> AddProductAsync(string name, CancellationToken cancellationToken)
    {
        Requests++;
        await Task.Delay(Latency, cancellationToken);
        if (RejectWrites)
            throw new HttpRequestException($"The server rejected \"{name}\" (simulated).");

        var product = new Product(_nextId++, name, 0);
        _products.Add(product);
        return product;
    }

    /// <summary>A change made by "someone else": the cache does not know until it refetches.</summary>
    public void RestockElsewhere()
    {
        for (var i = 0; i < _products.Count; i++)
            _products[i] = _products[i] with { Stock = _products[i].Stock + 5 };
    }
}
