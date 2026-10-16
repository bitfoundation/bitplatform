using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

[TestClass]
public class HttpTrackingTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new HttpRequestException("connection refused");
    }

    [TestMethod]
    public async Task Successful_request_is_recorded_with_status_duration_and_redacted_headers()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var handler = new BevToolsHttpMessageHandler(session)
            {
                InnerHandler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}", System.Text.Encoding.UTF8, "application/json") }),
            };
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "secret");
            client.DefaultRequestHeaders.Add("X-Trace", "abc");

            var response = await client.GetAsync("https://example.com/api/items?token=abc&page=1");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.AreEqual("GET", record.Method);
            Assert.AreEqual(200, record.StatusCode);
            Assert.IsNotNull(record.DurationMs);
            Assert.Contains("token=" + BevRedactor.RedactedValue, record.Url);
            Assert.Contains("page=1", record.Url);
            Assert.AreEqual(BevRedactor.RedactedValue, record.RequestHeaders!.Single(h => h.Key == "Authorization").Value);
            Assert.AreEqual("abc", record.RequestHeaders!.Single(h => h.Key == "X-Trace").Value);
            Assert.AreEqual(11, record.ResponseBytes);

            var evt = session.Timeline.Find(record.TimelineEventId)!;
            Assert.AreEqual(BevToolsEventKind.Http, evt.Kind);
            Assert.AreEqual("GET /api/items?token=" + BevRedactor.RedactedValue + "&page=1", evt.Title);
            Assert.StartsWith("200", evt.Detail);
        }
    }

    [TestMethod]
    public async Task Failed_status_creates_an_error_record()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var handler = new BevToolsHttpMessageHandler(session) { InnerHandler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)) };
            using var client = new HttpClient(handler);
            await client.GetAsync("https://example.com/api/flaky");

            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.IsTrue(record.IsFailed);
            Assert.AreEqual(1, session.Http.FailedCount);
            var error = Assert.ContainsSingle(session.Errors.Snapshot());
            Assert.AreEqual("http", error.Source);
            Assert.Contains("500", error.Message);
        }
    }

    [TestMethod]
    public async Task Exceptions_are_recorded_and_rethrown()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var handler = new BevToolsHttpMessageHandler(session) { InnerHandler = new ThrowingHandler() };
            using var client = new HttpClient(handler);
            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => client.GetAsync("https://example.com/down"));

            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.Contains("connection refused", record.Error!);
            Assert.AreEqual(BevToolsSeverity.Error, session.Timeline.Find(record.TimelineEventId)!.Severity);
        }
    }

    [TestMethod]
    public async Task Requests_from_http_client_factory_are_tracked_through_the_filter()
    {
        var (session, scope) = TestHelpers.CreateSession(services: s => s.AddHttpClient("api").ConfigurePrimaryHttpMessageHandler(() => new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent))));
        using (scope)
        using (session.UseAmbient()) // no renderer here: attribute the async flow explicitly, as background work would
        {
            var factory = (IHttpClientFactory)scope.ServiceProvider.GetService(typeof(IHttpClientFactory))!;
            using var client = factory.CreateClient("api");
            await client.GetAsync("https://example.com/via-factory");
            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.AreEqual(204, record.StatusCode);
        }
    }

    [TestMethod]
    public async Task Track_http_false_leaves_factory_requests_uninstrumented()
    {
        var (session, scope) = TestHelpers.CreateSession(
            options => options.TrackHttp = false,
            services => services.AddHttpClient("api").ConfigurePrimaryHttpMessageHandler(() =>
                new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent))));
        using (scope)
        using (session.UseAmbient())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            using var client = factory.CreateClient("api");
            await client.GetAsync("https://example.com/untracked");

            Assert.IsEmpty(session.Http.Snapshot());
        }
    }
}
