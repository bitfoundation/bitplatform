using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Pipeline;

/// <summary>Error-flood protection and the value length limit (the same rules as <c>limits.ts</c>).</summary>
[TestClass]
public class BelemetryLimitsTests
{
    private static BelemetryLogRecord Error(string message, string? stack = null, string scope = "MyApp.Orders")
    {
        var log = new BelemetryLogRecord { Body = message, Feature = BelemetryFeatures.Errors, Severity = BelemetrySeverity.Error, Scope = new BelemetryScope(scope) };
        log.Attributes.Set("exception.type", "System.InvalidOperationException")
                      .Set("exception.message", message)
                      .Set("exception.stacktrace", stack ?? $"System.InvalidOperationException: {message}\n   at Loop()\n   at Tick()");
        return log;
    }

    [TestMethod]
    public void RecordsAnErrorOncePerDuplicateWindow()
    {
        long now = 0;
        var limiter = new BelemetryErrorLimiter(new BelemetryErrorOptions(), () => now);

        Assert.IsTrue(limiter.ShouldRecord(Error("boom")));
        now += 4_999;
        Assert.IsFalse(limiter.ShouldRecord(Error("boom")));
        Assert.IsTrue(limiter.ShouldRecord(Error("other")));
        now += 1; // the window counts from the last recorded occurrence
        Assert.IsTrue(limiter.ShouldRecord(Error("boom")));
    }

    [TestMethod]
    public void TellsErrorsApartByTheirFirstFrame_NotByTheRestOfTheStack()
    {
        var limiter = new BelemetryErrorLimiter(new BelemetryErrorOptions(), () => 0);

        Assert.IsTrue(limiter.ShouldRecord(Error("boom", "X: boom\n   at A()\n   at Caller1()")));
        Assert.IsFalse(limiter.ShouldRecord(Error("boom", "X: boom\n   at A()\n   at Caller2()")));
        Assert.IsTrue(limiter.ShouldRecord(Error("boom", "X: boom\n   at B()")));
        Assert.IsTrue(limiter.ShouldRecord(Error("boom", scope: "MyApp.Other")), "Another category is another error.");
    }

    [TestMethod]
    public void CapsErrorsPerMinute_AndCanBeSwitchedOff()
    {
        long now = 0;
        var limiter = new BelemetryErrorLimiter(new BelemetryErrorOptions { DuplicateWindow = TimeSpan.Zero, MaxPerMinute = 3 }, () => now);
        CollectionAssert.AreEqual(new[] { true, true, true, false }, Enumerable.Range(1, 4).Select(i => limiter.ShouldRecord(Error("e" + i))).ToArray());
        now += 20_000; // a third of the minute: one more
        CollectionAssert.AreEqual(new[] { true, false }, Enumerable.Range(5, 2).Select(i => limiter.ShouldRecord(Error("e" + i))).ToArray());

        var off = new BelemetryErrorLimiter(new BelemetryErrorOptions { DuplicateWindow = TimeSpan.Zero, MaxPerMinute = 0 }, () => 0);
        Assert.IsTrue(Enumerable.Range(0, 500).All(_ => off.ShouldRecord(Error("boom"))));
    }

    [TestMethod]
    public void AnErrorTheCapDropped_DoesNotOpenADuplicateWindow()
    {
        // Otherwise an error first seen while the bucket is empty stays invisible for its whole window.
        long now = 0;
        var limiter = new BelemetryErrorLimiter(new BelemetryErrorOptions { DuplicateWindow = TimeSpan.FromMinutes(2), MaxPerMinute = 1 }, () => now);

        Assert.IsTrue(limiter.ShouldRecord(Error("first")));
        now += 1_000;
        Assert.IsFalse(limiter.ShouldRecord(Error("second")), "The cap is spent.");
        now += 61_000; // a token is back, still within two minutes of the dropped occurrence
        Assert.IsTrue(limiter.ShouldRecord(Error("second")));
        Assert.IsFalse(limiter.ShouldRecord(Error("second")), "Now it is a duplicate.");
    }

    [TestMethod]
    public void SignsAnErrorLikeTheScriptDoes()
    {
        // The same expectation as limits.test.js, so both sides group the same occurrences.
        var log = Error("boom", "TypeError: boom\r\n   at A()\r\n   at B()", "bit.belemetry.errors");
        log.Attributes.Set("exception.type", "TypeError");
        Assert.AreEqual(string.Join('\u0001', "bit.belemetry.errors", "TypeError", "boom", "TypeError: boom\r\n   at A()"), BelemetryErrorLimiter.Signature(log));

        var console = new BelemetryLogRecord { Body = "failed", Scope = new BelemetryScope("bit.belemetry.console") };
        Assert.AreEqual(string.Join('\u0001', "bit.belemetry.console", "", "failed", ""), BelemetryErrorLimiter.Signature(console));
    }

    [TestMethod]
    public async Task ALoopOfIdenticalErrors_IsRecordedOnce_EveryDistinctOneAlways()
    {
        await using var host = TestHost.Create();
        var logger = host.StartLogging().CreateLogger("MyApp.Loop");

        for (var i = 0; i < 50; i++)
        {
            try
            {
                throw new InvalidOperationException("boom");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Tick failed");
            }
        }
        logger.LogError("Something else failed");
        logger.LogInformation("Logs are not errors");
        logger.LogInformation("Logs are not errors");
        await host.FlushAsync();

        Assert.AreEqual(1, host.Memory.Logs.Count(l => l.Body == "Tick failed"));
        Assert.AreEqual(1, host.Memory.Logs.Count(l => l.Body == "Something else failed"));
        Assert.AreEqual(2, host.Memory.Logs.Count(l => l.Body == "Logs are not errors"), "Only errors are deduplicated.");
    }

    [TestMethod]
    public async Task TheServerPipeline_IsNotCapped()
    {
        // It serves every user at once: a cap there would hide real failures under load.
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Server.CollectDotnetTelemetry = true;
        });
        var logger = host.StartLogging().CreateLogger("MyApp.Server");

        for (var i = 0; i < 5; i++) logger.LogError("Request failed");
        await host.FlushAsync();

        Assert.AreEqual(5, host.Memory.Logs.Count);
    }

    [TestMethod]
    public void Truncate_NeverSplitsASurrogatePair()
    {
        Assert.AreEqual("abcd", BelemetryValueLimit.Truncate("abcdef", 4));
        Assert.AreEqual("abc", BelemetryValueLimit.Truncate("abc😀", 4));
        Assert.AreEqual("abc", BelemetryValueLimit.Truncate("abc", 0));
    }

    [TestMethod]
    public async Task RunawayStrings_AreCut_AfterBeingScrubbed()
    {
        await using var host = TestHost.Create(o => o.MaxValueLength = 20);

        var log = Records.Log("jane@example.com " + new string('x', 100));
        log.Attributes.Set("payload", "jane@example.com " + new string('y', 100)).Set("list", new[] { new string('z', 30), "ok" }).Set("n", 12345678901L);
        host.Pipeline.Enqueue(log);

        var span = Records.Span();
        span.StatusMessage = new string('m', 30);
        span.Events.Add(new BelemetrySpanEvent { Name = "e", Attributes = new BelemetryAttributes().Set("a", new string('a', 30)) });
        host.Pipeline.Enqueue(span);
        await host.FlushAsync();

        var exported = host.Memory.Logs.Single();
        Assert.AreEqual("[email] " + new string('x', 12), exported.Body, "A cut before scrubbing would have left 'jane@exa...' behind.");
        Assert.AreEqual("[email] " + new string('y', 12), exported.Attributes["payload"]);
        CollectionAssert.AreEqual(new[] { new string('z', 20), "ok" }, (string[])exported.Attributes["list"]!);
        Assert.AreEqual(12345678901L, exported.Attributes["n"]);

        var exportedSpan = host.Memory.Spans.Single();
        Assert.AreEqual(new string('m', 20), exportedSpan.StatusMessage);
        Assert.AreEqual(new string('a', 20), exportedSpan.Events[0].Attributes["a"]);
    }

    [TestMethod]
    public async Task AStringListWithNulls_IsCutWithoutThrowing()
    {
        await using var host = TestHost.Create(o => o.MaxValueLength = 4);
        var log = Records.Log();
        log.Attributes.Set("tags", new List<string?> { null, "abcdefgh" });

        host.Pipeline.Enqueue(log);
        await host.FlushAsync();

        CollectionAssert.AreEqual(new[] { null, "abcd" }, (string?[])host.Memory.Logs.Single().Attributes["tags"]!);
    }

    [TestMethod]
    public async Task ValueLength_CanBeSwitchedOff()
    {
        await using var host = TestHost.Create(o => o.MaxValueLength = 0);
        host.Pipeline.Enqueue(Records.Log(new string('x', 100_000)));
        await host.FlushAsync();
        Assert.AreEqual(100_000, host.Memory.Logs.Single().Body!.Length);
    }
}
