namespace Bit.Bheadless;

/// <summary>Generates unique, HTML-safe element ids.</summary>
public static class BitBhId
{
    private static long _counter;

    /// <summary>
    /// Returns a new id such as <c>bh-1a2b3c</c>. Ids are unique per process and
    /// safe to use in CSS selectors and IDREF attributes.
    /// </summary>
    public static string New(string prefix = "bit-bh")
    {
        var n = Interlocked.Increment(ref _counter);
        // Mix in a random component so ids from different processes (prerender
        // vs. interactive render) don't collide when markup is persisted.
        var salt = Random.Shared.Next(0x1000, 0xFFFF);
        return $"{prefix}-{salt:x4}{n:x}";
    }
}
