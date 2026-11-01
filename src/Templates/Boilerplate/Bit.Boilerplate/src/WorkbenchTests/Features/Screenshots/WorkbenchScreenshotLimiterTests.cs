namespace Boilerplate.WorkbenchTests.Features.Screenshots;

/// <summary>
/// Screenshots run a browser on the developer's machine, so a remote visitor gets a share of them per minute and the
/// developer none at all.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchScreenshotLimiterTests
{
    private static WorkbenchScreenshotLimiter Limiter(int permitsPerMinute) => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Screenshots:PermitsPerMinutePerClient"] = permitsPerMinute.ToString(CultureInfo.InvariantCulture) })
        .Build());

    private static DefaultHttpContext Visitor(string? cloudflareClient = null, string? forwardedFor = null, IPAddress? from = null)
    {
        var context = new DefaultHttpContext();

        context.Connection.RemoteIpAddress = from ?? IPAddress.Loopback;
        context.Request.Host = new("abc.trycloudflare.com");

        if (cloudflareClient is not null)
        {
            context.Request.Headers["Cf-Connecting-Ip"] = cloudflareClient;
        }

        if (forwardedFor is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        return context;
    }

    [TestMethod]
    public void AVisitor_Should_RunOutOfScreenshotsForTheMinute()
    {
        using var limiter = Limiter(3);
        var visitor = Visitor(cloudflareClient: "203.0.113.7");

        Assert.IsTrue(limiter.TryAcquire(visitor));
        Assert.IsTrue(limiter.TryAcquire(visitor, 2));
        Assert.IsFalse(limiter.TryAcquire(visitor));
    }

    [TestMethod]
    public void AnArchive_Should_CountOneScreenshotPerView()
    {
        using var limiter = Limiter(5);
        var visitor = Visitor(cloudflareClient: "203.0.113.7");

        Assert.IsTrue(limiter.TryAcquire(visitor, 4));
        Assert.IsFalse(limiter.TryAcquire(visitor, 2));
        Assert.IsTrue(limiter.TryAcquire(visitor, 1));
    }

    [TestMethod]
    public void EveryVisitor_Should_GetAShareOfTheirOwn()
    {
        using var limiter = Limiter(1);

        Assert.IsTrue(limiter.TryAcquire(Visitor(cloudflareClient: "203.0.113.7")));
        Assert.IsFalse(limiter.TryAcquire(Visitor(cloudflareClient: "203.0.113.7")));
        Assert.IsTrue(limiter.TryAcquire(Visitor(cloudflareClient: "203.0.113.8")));
        Assert.IsTrue(limiter.TryAcquire(Visitor(forwardedFor: "198.51.100.1, 203.0.113.9")), "The proxy's own entry, the last one, names the visitor.");
        Assert.IsTrue(limiter.TryAcquire(Visitor(forwardedFor: "198.51.100.1, 203.0.113.10")), "A spoofed first entry doesn't make two visitors one.");
        Assert.IsTrue(limiter.TryAcquire(Visitor(from: IPAddress.Parse("192.0.2.1"))));
        Assert.IsFalse(limiter.TryAcquire(Visitor(from: IPAddress.Parse("192.0.2.1"))));
    }

    [TestMethod]
    public void TheDeveloper_Should_NeverBeLimited()
    {
        using var limiter = Limiter(1);
        var local = new DefaultHttpContext();

        local.Connection.RemoteIpAddress = IPAddress.Loopback;
        local.Request.Host = new("localhost", 6030);

        for (var i = 0; i < 100; i++)
        {
            Assert.IsTrue(limiter.TryAcquire(local, 24));
        }
    }
}
