using Microsoft.AspNetCore.SignalR.Client;
using Boilerplate.Client.Core.Infrastructure.Services;

namespace Boilerplate.Tests.Features.SignalR;

/// <summary>
/// The policy used to return a flat 1 second forever, justified by a comment saying <c>RetryDelegatingHandler</c>
/// already backs the negotiate request off. That handler now returns immediately unless the app runs in a browser or in
/// Blazor Hybrid, so on Blazor Server - the shipped default render mode - there was no backoff at all: every circuit
/// re-negotiated once a second for the whole outage, on the same grid, and so came back in the same second on recovery.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class SignalRRetryPolicyTests
{
    [TestMethod]
    public void TheDelay_Should_GrowWithTheRetryCountAndStayCapped()
    {
        var policy = new SignalRInfiniteRetryPolicy();

        var first = policy.NextRetryDelay(new Microsoft.AspNetCore.SignalR.Client.RetryContext { PreviousRetryCount = 0 });
        var fifth = policy.NextRetryDelay(new Microsoft.AspNetCore.SignalR.Client.RetryContext { PreviousRetryCount = 4 });
        var thousandth = policy.NextRetryDelay(new Microsoft.AspNetCore.SignalR.Client.RetryContext { PreviousRetryCount = 1_000 });

        Assert.IsNotNull(first, "A null delay ends the connection permanently, which is the opposite of this policy's job.");
        Assert.IsNotNull(fifth);
        Assert.IsNotNull(thousandth);

        // The first retry stays about a second, so a blip still recovers immediately.
        Assert.IsTrue(first.Value > TimeSpan.FromMilliseconds(700) && first.Value < TimeSpan.FromMilliseconds(1300), $"First delay was {first}.");

        Assert.IsTrue(fifth.Value > first.Value, "The delay does not grow with the retry count, so an outage is met with a fixed-rate request storm.");

        Assert.IsTrue(thousandth.Value <= TimeSpan.FromSeconds(12),
            $"The delay is not capped ({thousandth}), so a long outage leaves the user offline for far longer than the stateful-reconnect window.");
    }

    [TestMethod]
    public void TheDelay_Should_BeJittered()
    {
        var policy = new SignalRInfiniteRetryPolicy();

        // Same input, many draws: without jitter every client dropped by one event returns in the same second.
        var delays = Enumerable.Range(0, 50)
                               .Select(_ => policy.NextRetryDelay(new Microsoft.AspNetCore.SignalR.Client.RetryContext { PreviousRetryCount = 3 }))
                               .Distinct()
                               .ToArray();

        Assert.IsTrue(delays.Length > 1, "Every draw produced the same delay, so the reconnect cadence is not jittered.");
    }
}
