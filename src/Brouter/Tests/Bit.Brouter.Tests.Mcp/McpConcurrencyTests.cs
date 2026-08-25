using System.Collections.Concurrent;
using ModelContextProtocol.Client;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The server under the traffic it is built for: several clients, several sessions, all reading at
/// once.
/// <para>
/// Almost everything here is a lazily built, process-wide catalog - the guide sections, the API
/// reference, the search index, the rendered pages - so the first request for each is also the one
/// that builds it. Under one caller that is invisible; under several it is where a torn or
/// half-built catalog would show up, and a documentation server that answers differently depending
/// on who asked first is worse than one that is slow.
/// </para>
/// </summary>
[TestClass]
public class McpConcurrencyTests
{
    [TestMethod]
    public async Task The_same_question_asked_many_times_at_once_gets_the_same_answer()
    {
        (string Tool, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("GetBrouterGuideSection", new() { ["heading"] = "Data loader" }),
            ("GetBrouterApi", new() { ["typeName"] = "BrouterOptions" }),
            ("GetBrouterDocsPage", new() { ["slug"] = "route-templates" }),
            ("SearchBrouter", new() { ["query"] = "loader cache", ["limit"] = 5 }),
            ("InspectBrouterRouteTemplates", new() { ["templates"] = "/users/{id:int}" }),
            ("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/AppRouter.razor" }),
        ];

        var answers = new ConcurrentDictionary<string, ConcurrentBag<string>>(StringComparer.Ordinal);

        // Each call is made from several places at once, and the whole set is interleaved, so no
        // catalog gets to be built quietly by one caller before the others arrive.
        await Task.WhenAll(Enumerable.Range(0, 6).SelectMany(_ => calls.Select(async call =>
        {
            var text = await McpCall.TextAsync(call.Tool, call.Arguments);

            answers.GetOrAdd(call.Tool, _ => []).Add(text);
        })));

        foreach (var (tool, texts) in answers)
        {
            Assert.AreEqual(6, texts.Count);
            Assert.AreEqual(1, texts.Distinct(StringComparer.Ordinal).Count(), $"'{tool}' answered the same question differently under load.");
        }
    }

    [TestMethod]
    public async Task Several_clients_can_hold_their_own_sessions_at_the_same_time()
    {
        // A client per session, each doing its own handshake against the running server - which is
        // what a shared documentation server actually looks like.
        var clients = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => ConnectAsync()));

        try
        {
            var indexes = await Task.WhenAll(clients.Select(async client =>
            {
                var result = await client.CallToolAsync("GetBrouterApi");

                return string.Join('\n', result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Select(block => block.Text));
            }));

            Assert.AreEqual(1, indexes.Distinct(StringComparer.Ordinal).Count(), "Two sessions were served different API indexes.");

            foreach (var client in clients)
            {
                Assert.AreEqual("bit-brouter", client.ServerInfo.Name);
                Assert.AreEqual(McpToolSurfaceTests.ExpectedTools.Length, (await client.ListToolsAsync()).Count);
            }
        }
        finally
        {
            foreach (var client in clients) await client.DisposeAsync();
        }
    }

    private static async Task<McpClient> ConnectAsync()
    {
        return await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            EnableStandaloneGetStream = false,
        }, McpTestHost.CreateHttpClient(), loggerFactory: null, ownsHttpClient: true));
    }
}
