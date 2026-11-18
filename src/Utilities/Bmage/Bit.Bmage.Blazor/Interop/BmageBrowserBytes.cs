namespace Bit.Bmage;

/// <summary>Bytes returned from the browser together with their descriptor. Dispose to return the buffer to the pool.</summary>
public readonly struct BmageBrowserBytes<TInfo> : IDisposable where TInfo : class
{
    public BmageBrowserBytes(TInfo? info, BmagePooledBytes bytes)
    {
        Info = info;
        Bytes = bytes;
    }

    /// <summary>An empty result, used when the browser had nothing to return (e.g. an empty clipboard).</summary>
    public static BmageBrowserBytes<TInfo> Empty => new(null, default);

    /// <summary>The metadata descriptor, or null when the result was empty.</summary>
    public TInfo? Info { get; }

    /// <summary>The payload.</summary>
    public BmagePooledBytes Bytes { get; }

    /// <summary>True when the browser returned a result.</summary>
    public bool HasValue => Info is not null;

    public void Dispose() => Bytes.Dispose();
}
