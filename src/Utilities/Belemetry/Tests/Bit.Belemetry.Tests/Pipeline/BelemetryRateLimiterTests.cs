namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetryRateLimiterTests
{
    [TestMethod]
    public void AllowsABurst_ThenRefillsOverThePeriod()
    {
        long now = 1_000;
        var limiter = new BelemetryRateLimiter(10, TimeSpan.FromSeconds(10), () => now);

        Assert.AreEqual(10, limiter.Take(25), "Starts full.");
        Assert.AreEqual(0, limiter.Take(1));

        now += 3_000; // 3 of the 10 seconds: 3 tokens back
        Assert.AreEqual(3, limiter.Take(5));

        now += 60_000; // long idle: refills to the capacity, never past it
        Assert.AreEqual(10, limiter.Take(100));
    }

    [TestMethod]
    public void IgnoresAClockGoingBackwards_AndNonPositiveRequests()
    {
        long now = 10_000;
        var limiter = new BelemetryRateLimiter(2, TimeSpan.FromSeconds(1), () => now);

        Assert.AreEqual(0, limiter.Take(0));
        Assert.AreEqual(0, limiter.Take(-3));
        Assert.AreEqual(2, limiter.Take(2));

        now -= 5_000;
        Assert.AreEqual(0, limiter.Take(1), "No tokens out of a clock that went back.");
    }
}
