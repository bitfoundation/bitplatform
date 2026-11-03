using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Bit.BevTools.Tests;

/// <summary>
/// Bodies, only when asked for: text, bounded, redacted, and never at the cost of the request - the server receives what
/// the app sent, and the app reads what the server sent.
/// </summary>
[TestClass]
public class HttpBodiesTests
{
    /// <summary>Echoes what it received, as the server would see it, and answers with a configured response.</summary>
    private sealed class EchoHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        public string? Received { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Received = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond();
        }
    }

    /// <summary>A stream that can be read once, front to back, and does not know its length.</summary>
    private sealed class ForwardOnly(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static (HttpClient Client, EchoHandler Echo) Client(BevToolsSession session, Func<HttpResponseMessage> respond)
    {
        var echo = new EchoHandler(respond);
        return (new HttpClient(new BevToolsHttpMessageHandler(session) { InnerHandler = echo }) { BaseAddress = new Uri("https://api.test/") }, echo);
    }

    [TestMethod]
    public async Task Bodies_are_not_captured_unless_asked_for()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var (client, _) = Client(session, () => Json("{\"id\":1}"));
            await client.PostAsync("orders", new StringContent("{\"sku\":\"A\"}", Encoding.UTF8, "application/json"));
            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.IsNull(record.RequestBody);
            Assert.IsNull(record.ResponseBody);
            Assert.IsNull(record.RequestBodyNote);
            Assert.AreEqual(BevCapabilityStatus.NotAvailable, session.Capabilities.Single(c => c.Name == "Request/response bodies").Status);
        }
    }

    [TestMethod]
    public async Task Json_bodies_are_captured_redacted_and_the_request_and_response_are_untouched()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.CaptureHttpBodies = true);
        using (scope)
        {
            const string answer = "{\"id\":7,\"accessToken\":\"abc\",\"owner\":{\"name\":\"Ada\",\"password\":\"hunter2\"}}";
            var (client, echo) = Client(session, () => Json(answer));
            var response = await client.PostAsJsonAsync("signin", new { user = "ada", password = "hunter2", note = "Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJlLXZhbHVl" });

            Assert.AreEqual("{\"user\":\"ada\",\"password\":\"hunter2\",\"note\":\"Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJlLXZhbHVl\"}", echo.Received, "the server received what the app sent");
            Assert.AreEqual(answer, await response.Content.ReadAsStringAsync(), "the app reads what the server sent");

            var record = Assert.ContainsSingle(session.Http.Snapshot());
            StringAssert.Contains(record.RequestBody, "\"user\": \"ada\"");
            StringAssert.Contains(record.RequestBody, "\"password\": \"«redacted»\"");
            Assert.DoesNotContain("hunter2", record.RequestBody!, StringComparison.Ordinal);
            Assert.DoesNotContain("eyJ", record.RequestBody!, StringComparison.Ordinal);
            StringAssert.Contains(record.ResponseBody, "\"accessToken\": \"«redacted»\"");
            StringAssert.Contains(record.ResponseBody, "\"name\": \"Ada\"");
            Assert.DoesNotContain("hunter2", record.ResponseBody!, StringComparison.Ordinal);
            Assert.AreEqual(BevCapabilityStatus.Available, session.Capabilities.Single(c => c.Name == "Request/response bodies").Status);
            StringAssert.Contains(BevSessionExporter.ToJson(session, new BevSessionExportOptions { Indented = false, Sections = new HashSet<string> { "http" } }), "\"requestBody\":");
        }
    }

    [TestMethod]
    public async Task A_body_bevtools_cannot_read_never_costs_the_app_its_request()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.CaptureHttpBodies = true);
        using (scope)
        {
            // A repeated name is JSON a node tree refuses; the app reads the response all the same.
            const string answer = "{\"a\":1,\"password\":\"hunter2\",\"password\":\"hunter3\"}";
            var (client, echo) = Client(session, () => Json(answer));
            var response = await client.PostAsync("pairs", new StringContent("{\"token\":\"x1\",\"token\":\"x2\",\"b\":2}", Encoding.UTF8, "application/json"));
            Assert.AreEqual(answer, await response.Content.ReadAsStringAsync());
            Assert.IsNotNull(echo.Received, "the request was sent");

            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.IsNull(record.Error);
            Assert.DoesNotContain("hunter", record.ResponseBody!, StringComparison.Ordinal);
            Assert.AreEqual(2, record.ResponseBody!.Split("\"password\": \"«redacted»\"").Length - 1, "each occurrence redacted by its name");
            Assert.DoesNotContain("x1", record.RequestBody!, StringComparison.Ordinal);
            Assert.DoesNotContain("x2", record.RequestBody!, StringComparison.Ordinal);
            StringAssert.Contains(record.RequestBody, "\"b\": 2");

            // A charset nothing can decode: not read, and not called too large either.
            var unknown = new StringContent("short", Encoding.UTF8, "text/plain");
            unknown.Headers.ContentType!.CharSet = "bogus";
            // The echo decodes what it received, and cannot either; what BevTools noted before the send is what matters.
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsync("notes", unknown));
            StringAssert.StartsWith(session.Http.Snapshot()[^1].RequestBodyNote, "could not be read: ");
        }
    }

    [TestMethod]
    public async Task A_stream_and_a_response_of_unknown_length_are_never_read()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.CaptureHttpBodies = true);
        using (scope)
        {
            // A body whose length nobody knows until it ends, as a chunked response's.
            var chunked = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new ForwardOnly(Encoding.UTF8.GetBytes("[1,2,3]"))) };
            chunked.Content.Headers.ContentType = new("application/json");
            var (client, echo) = Client(session, () => chunked);
            var upload = new StreamContent(new ForwardOnly(Encoding.UTF8.GetBytes("{\"big\":true}")));
            upload.Headers.ContentType = new("application/json");
            var response = await client.PostAsync("upload", upload);

            Assert.AreEqual("{\"big\":true}", echo.Received, "the stream reached the server whole");
            Assert.AreEqual("[1,2,3]", await response.Content.ReadAsStringAsync(), "the app read the response whole");
            var record = Assert.ContainsSingle(session.Http.Snapshot());
            Assert.IsNull(record.RequestBody);
            StringAssert.StartsWith(record.RequestBodyNote, "a stream is read once");
            Assert.IsNull(record.ResponseBody);
            StringAssert.StartsWith(record.ResponseBodyNote, "no length (chunked)");
        }
    }

    [TestMethod]
    public async Task Limits_text_only_forms_and_where_it_is_honoured()
    {
        var (session, scope) = TestHelpers.CreateSession(o =>
        {
            o.CaptureHttpBodies = true;
            o.MaxHttpBodyBytes = 300;
        });
        using (scope)
        {
            var (client, _) = Client(session, () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[64]) { Headers = { ContentType = new("image/png") } } });
            await client.PostAsync("login", new FormUrlEncodedContent([new("user", "ada"), new("password", "hunter2")]));
            await client.PostAsync("notes", new StringContent(new string('x', 1000), Encoding.UTF8, "text/plain"));
            var records = session.Http.Snapshot();
            Assert.AreEqual("user=ada&password=«redacted»", records[0].RequestBody);
            StringAssert.StartsWith(records[0].ResponseBodyNote, "not text (image/png)");
            StringAssert.StartsWith(records[1].RequestBodyNote, "larger than 300 B");
        }

        (session, scope) = TestHelpers.CreateSession(o =>
        {
            o.CaptureHttpBodies = true;
            o.ValueAccess = BevValueAccess.Shallow;
        });
        using (scope)
        {
            var (client, _) = Client(session, () => Json("{}"));
            await client.PostAsync("orders", new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.IsNull(Assert.ContainsSingle(session.Http.Snapshot()).RequestBodyNote, "not at Shallow: nothing was looked at");
            StringAssert.Contains(session.Capabilities.Single(c => c.Name == "Request/response bodies").Explanation, "needs ValueAccess Full");
        }
    }

    [TestMethod]
    public async Task Json_content_is_serialized_again_for_the_copy_and_sent_as_it_would_have_been()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.CaptureHttpBodies = true);
        using (scope)
        {
            long? sentLength = -1;
            var echo = new CapturingHandler(request => sentLength = request.Content?.Headers.ContentLength);
            using var client = new HttpClient(new BevToolsHttpMessageHandler(session) { InnerHandler = echo }) { BaseAddress = new Uri("https://api.test/") };
            await client.PostAsJsonAsync("orders", new { sku = "A", count = 2 });
            Assert.IsNull(sentLength, "not buffered by BevTools: a JSON body without a length is sent as one");
            StringAssert.Contains(Assert.ContainsSingle(session.Http.Snapshot()).RequestBody, "\"sku\": \"A\"");

            // A sequence is enumerated once, by the send.
            await client.PostAsJsonAsync("stream", Numbers());
            StringAssert.StartsWith(session.Http.Snapshot()[^1].RequestBodyNote, "an async sequence is enumerated once");
        }

        static async IAsyncEnumerable<int> Numbers()
        {
            await Task.Yield();
            yield return 1;
        }
    }

    [TestMethod]
    public async Task Xml_and_json_that_does_not_parse_are_redacted_by_name_and_a_small_limit_is_the_limit()
    {
        var (session, scope) = TestHelpers.CreateSession(o =>
        {
            o.CaptureHttpBodies = true;
            o.MaxHttpBodyBytes = 100;
            o.SensitiveNamePatterns.Add("pin");
        });
        using (scope)
        {
            var (client, _) = Client(session, () => Json("{}"));
            await client.PostAsync("soap", new StringContent("<login><user>ada</user><ns:password a=\"1\">x1</ns:password><pin><![CDATA[42]]></pin></login>", Encoding.UTF8, "application/xml"));
            await client.PostAsync("broken", new StringContent("{\"user\":\"ada\",\"pin\":\"42\",\"token\":\"ab\", oops", Encoding.UTF8, "application/json"));
            await client.PostAsync("big", new StringContent(new string('x', 150), Encoding.UTF8, "text/plain"));

            var records = session.Http.Snapshot();
            Assert.AreEqual("<login><user>ada</user><ns:password a=\"1\">«redacted»</ns:password><pin>«redacted»</pin></login>", records[0].RequestBody);
            Assert.AreEqual("{\"user\":\"ada\",\"pin\":\"«redacted»\",\"token\":\"«redacted»\", oops", records[1].RequestBody, "by name, however short the value");
            StringAssert.StartsWith(records[2].RequestBodyNote, "larger than 100 B", "the limit set, not a floor under it");
        }
    }

    /// <summary>Notes the request as the transport gets it, and answers with nothing.</summary>
    private sealed class CapturingHandler(Action<HttpRequestMessage> seen) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            seen(request);
            if (request.Content is not null)
            {
                await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }

    [TestMethod]
    public async Task Grpc_server_sent_events_and_graphql_are_told_apart()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.CaptureHttpBodies = true);
        using (scope)
        {
            var responses = new Queue<HttpResponseMessage>([
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0, 0, 0, 0, 0]) { Headers = { ContentType = new("application/grpc-web+proto") } } },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data: 1\n\n", Encoding.UTF8, "text/event-stream") },
                Json("{\"data\":{}}"),
            ]);
            var (client, _) = Client(session, responses.Dequeue);
            await client.PostAsync("shop.Orders/List", new ByteArrayContent([0]) { Headers = { ContentType = new("application/grpc-web+proto") } });
            await client.GetAsync("events");
            await client.PostAsJsonAsync("graphql", new { operationName = "RecentOrders", query = "query RecentOrders { orders { id } }" });

            var records = session.Http.Snapshot();
            Assert.AreEqual("gRPC", records[0].Protocol);
            Assert.AreEqual("server-sent events", records[1].Protocol);
            StringAssert.StartsWith(records[1].ResponseBodyNote, "a stream of server-sent events");
            Assert.AreEqual("GraphQL", records[2].Protocol);
            Assert.AreEqual("RecentOrders", records[2].GraphQlOperation);
        }
    }
}
