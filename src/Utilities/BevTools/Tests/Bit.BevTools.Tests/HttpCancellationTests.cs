using System.Net;

namespace Bit.BevTools.Tests;

/// <summary>
/// A cancelled request is not a failed one: inside the handler pipeline a caller cancelling and HttpClient.Timeout
/// elapsing look the same, so BevTools calls it cancelled and says both are possible, instead of a failure.
/// </summary>
[TestClass]
public class HttpCancellationTests
{
    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    [TestMethod]
    public async Task A_cancelled_request_is_labelled_cancelled_and_not_counted_as_a_failure()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            using var client = new HttpClient(new BevToolsHttpMessageHandler(session) { InnerHandler = new HangingHandler() });
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetAsync("https://example.test/api/search?q=a", cts.Token));

            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.IsTrue(record.IsCancelled);
            Assert.IsFalse(record.IsFailed);
            Assert.AreEqual(0, session.Http.FailedCount);
            Assert.IsEmpty(session.Errors.Snapshot(), "a cancellation is the caller's decision, not an error");
            var evt = session.Timeline.Find(record.TimelineEventId)!;
            Assert.AreEqual(BevToolsSeverity.Warning, evt.Severity);
            StringAssert.Contains(evt.Detail, "cancelled after");
            StringAssert.Contains(BevSessionExporter.ToJson(session, new BevSessionExportOptions { Indented = false }), "\"cancelled\":true");
        }
    }

    [TestMethod]
    public async Task An_HttpClient_timeout_is_recorded_as_cancelled_with_both_causes_named()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            using var client = new HttpClient(new BevToolsHttpMessageHandler(session) { InnerHandler = new HangingHandler() }) { Timeout = TimeSpan.FromMilliseconds(50) };

            // HttpClient turns its own timeout into a TaskCanceledException wrapping a TimeoutException, above the handlers.
            var thrown = await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetAsync("https://example.test/slow"));
            Assert.IsInstanceOfType<TimeoutException>(thrown.InnerException);

            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.IsTrue(record.IsCancelled);
            StringAssert.Contains(record.Error, "HttpClient.Timeout");
        }
    }

    private sealed class ConnectTimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new TaskCanceledException("The operation was canceled.", new TimeoutException("A connection could not be established within the configured ConnectTimeout."));
    }

    [TestMethod]
    public void A_cancellation_the_caller_did_not_ask_for_is_a_failure_with_its_reason()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            using var client = new HttpClient(new BevToolsHttpMessageHandler(session) { InnerHandler = new ConnectTimeoutHandler() });

            Assert.ThrowsExactly<TaskCanceledException>(() => client.GetAsync("https://shop.test/api/orders").GetAwaiter().GetResult());

            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.IsFalse(record.IsCancelled, "the request's own token was never cancelled");
            Assert.IsTrue(record.IsFailed);
            StringAssert.Contains(record.Error, "TimeoutException: A connection could not be established");
            var error = Assert.ContainsSingle(session.Errors.Snapshot());
            Assert.AreEqual("http", error.Source);
        }
    }

    [TestMethod]
    public void An_endpoint_is_the_path_and_the_names_of_its_query_parameters()
    {
        Assert.AreEqual("GET /api/products?page&q", BevHttpEndpoints.Of("GET", "/api/products?q=shoes&page=2"));
        Assert.AreEqual("GET /api/products?page&q", BevHttpEndpoints.Of("GET", "/api/products?page=3&q=hats#top"));
        Assert.AreEqual("POST https://x.test/orders", BevHttpEndpoints.Of("POST", "https://x.test/orders"));
        Assert.AreEqual("GET /a?flag", BevHttpEndpoints.Of("GET", "/a?flag"));
    }
}
