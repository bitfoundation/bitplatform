using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Tests.Features.SignalR;

/// <summary>RetryDelegatingHandler backs off only the attempts that reach it, so a reconnect that keeps failing fast backs off itself.</summary>
[TestClass, TestCategory("UnitTest")]
public class SignalRInfiniteRetryPolicyTests
{
    [TestMethod]
    public void AReconnectThatKeepsFailingFast_Should_WaitLongerEachTime_UpToHalfAMinute()
    {
        var policy = new SignalRInfiniteRetryPolicy();
        var elapsed = TimeSpan.Zero;

        for (var retry = 0; retry < 8; retry++)
        {
            var delay = policy.NextRetryDelay(new() { PreviousRetryCount = retry, ElapsedTime = elapsed })!.Value;

            AssertAbout(TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retry), 30)), delay);

            elapsed += delay + TimeSpan.FromMilliseconds(50); // The attempt fails at once.
        }
    }

    [TestMethod]
    [DataRow(false, DisplayName = "After an attempt RetryDelegatingHandler backed off")]
    [DataRow(true, DisplayName = "When a new reconnect starts")]
    public void TheWait_Should_BeShortAgain(bool newReconnect)
    {
        var policy = new SignalRInfiniteRetryPolicy();
        var elapsed = TimeSpan.Zero;

        for (var retry = 0; retry < 4; retry++)
        {
            elapsed += policy.NextRetryDelay(new() { PreviousRetryCount = retry, ElapsedTime = elapsed })!.Value + TimeSpan.FromMilliseconds(50);
        }

        var delay = newReconnect
            ? policy.NextRetryDelay(new() { PreviousRetryCount = 0, ElapsedTime = TimeSpan.Zero })!.Value
            : policy.NextRetryDelay(new() { PreviousRetryCount = 4, ElapsedTime = elapsed + TimeSpan.FromSeconds(9) })!.Value;

        AssertAbout(TimeSpan.FromSeconds(1), delay);
    }

    /// <summary>The policy adds up to 20% jitter either way.</summary>
    private static void AssertAbout(TimeSpan expected, TimeSpan actual)
    {
        Assert.IsGreaterThanOrEqualTo(expected * 0.79, actual);
        Assert.IsLessThanOrEqualTo(expected * 1.21, actual);
    }
}
