using System.Net;
using System.Text;
using System.Text.Json;
using Bit.Belemetry.Tests.Harness;
using Bit.Belemetry.Tests.Harness.Web.Collector;
using Bit.Belemetry.Tests.Hosting.Infrastructure;
using Modes = Bit.Belemetry.Tests.Harness.Web.HarnessRenderModes;

namespace Bit.Belemetry.Tests.Hosting;

/// <summary>
/// The capturing receiver's Sentry endpoint, which the E2E suites trust to tell a well-formed envelope from one that is
/// not, as Relay does.
/// </summary>
[TestClass]
public class HarnessCollectorTests
{
    private const string EventId = "0123456789abcdef0123456789abcdef";

    [TestMethod]
    public void AddSentry_TakesOneEvent_WithOrWithoutTheTrailingNewline()
    {
        var collector = new HarnessCollector();

        Assert.AreEqual(EventId, collector.AddSentry("/api/1/envelope/", "key", null, null, null, Envelope("\n"))?.EventId);
        Assert.AreEqual(EventId, collector.AddSentry("/api/1/envelope/", "key", null, null, null, Envelope(string.Empty))?.EventId);
        Assert.HasCount(2, collector.Snapshot().SentryEvents);
    }

    [TestMethod]
    public void AddSentry_RefusesAnythingAfterTheEvent()
    {
        var collector = new HarnessCollector();

        Assert.IsNull(collector.AddSentry("/api/1/envelope/", "key", null, null, null, Envelope("\n{not an item header\n")));
        Assert.IsNull(collector.AddSentry("/api/1/envelope/", "key", null, null, null, Envelope("\n{\"type\":\"attachment\",\"length\":3}\nabc\n")));

        var received = collector.Snapshot();
        Assert.IsEmpty(received.SentryEvents);
        Assert.IsTrue(received.Requests.All(r => r.Decoded is false));
    }

    [TestMethod]
    public void AddSentry_RefusesABodyThatIsNotGzip_AsGzip()
    {
        var collector = new HarnessCollector();

        Assert.IsNull(collector.AddSentry("/api/1/envelope/", "key", null, "gzip", null, Envelope("\n")));
        Assert.IsFalse(collector.Snapshot().Requests.Single().Decoded);
    }

    [TestMethod]
    public void AddSentry_KeepsAnObjectInExtra_AsItsJson()
    {
        var collected = new HarnessCollector().AddSentry("/api/1/envelope/", "key", null, null, null, Envelope("\n", ""","extra":{"o":{"a":1},"s":"text"}"""));

        Assert.AreEqual("""{"a":1}""", collected?.Extra.GetValueOrDefault("o"));
        Assert.AreEqual("text", collected?.Extra.GetValueOrDefault("s"));
    }

    [TestMethod]
    public async Task The_envelope_endpoint_answers_with_the_events_id_and_a_400_for_what_it_cannot_read()
    {
        await using var factory = new HarnessHostFactory(Modes.Ssr);
        var client = factory.CreateClient();
        var url = HarnessCollectorFilter.CollectorPath + "/api/1/envelope/?sentry_key=key&sentry_version=7";

        using (var accepted = await client.PostAsync(url, new ByteArrayContent(Envelope("\n"))))
        {
            Assert.AreEqual(HttpStatusCode.OK, accepted.StatusCode);
            using var answer = JsonDocument.Parse(await accepted.Content.ReadAsStringAsync());
            Assert.AreEqual(EventId, answer.RootElement.GetProperty("id").GetString());
        }

        var notGzip = new ByteArrayContent(Envelope("\n"));
        notGzip.Headers.ContentEncoding.Add("gzip");
        using var refused = await client.PostAsync(url, notGzip);
        Assert.AreEqual(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    private static byte[] Envelope(string trailer, string eventProperties = "")
    {
        var item = $$"""{"event_id":"{{EventId}}","platform":"csharp","level":"error"{{eventProperties}}}""";
        return Encoding.UTF8.GetBytes($$"""{"event_id":"{{EventId}}"}""" + "\n"
                                      + $$"""{"type":"event","length":{{Encoding.UTF8.GetByteCount(item)}}}""" + "\n"
                                      + item + trailer);
    }
}
