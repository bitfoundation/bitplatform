namespace Bit.Belemetry.Tests.Hosting.Infrastructure;

public static class Eventually
{
    /// <summary>
    /// Polls <paramref name="probe"/> until it returns a value, or fails the test after <paramref name="timeout"/> with
    /// <paramref name="describe"/>'s account of what was there instead.
    /// </summary>
    public static async Task<T> WaitAsync<T>(Func<T?> probe, TimeSpan timeout, Func<string> describe) where T : class
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            if (probe() is { } value) return value;
            if (DateTime.UtcNow > deadline) break;
            await Task.Delay(100);
        }

        Assert.Fail($"Nothing matched within {timeout}. {describe()}");
        throw new InvalidOperationException("Unreachable.");
    }
}
