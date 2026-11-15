using Boilerplate.LandingPage.Server.Features.StoreListing;
using Microsoft.Extensions.Logging;

namespace Boilerplate.LandingPage.Tests.Features;

[TestClass]
public class ServiceTests
{
    private static readonly FakeAppStore appStore = new();
    private static LandingTestServer server = default!;
    private static HttpClient client = default!;

    [ClassInitialize]
    public static async Task Start(TestContext testContext)
    {
        server = await new LandingTestServer().Start(after: services => services.AddHttpClient<AppStoreListingService>().ConfigurePrimaryHttpMessageHandler(() => appStore));
        client = server.CreateClient();
    }

    [ClassCleanup]
    public static async Task Stop()
    {
        client.Dispose();
        await server.DisposeAsync();
    }

    [TestMethod]
    public async Task TheRating_Should_BeLookedUpOnlyForTheSamplesOwnApps()
    {
        using var unknown = await client.GetAsync("api/store-listing/1234567890");

        Assert.AreEqual(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.DoesNotContain(uri => uri.Query.Contains("1234567890", StringComparison.Ordinal), appStore.Requests, "An id no sample has never reaches Apple.");

        if (LandingSites.All.Select(site => site.AppleAppId).FirstOrDefault(id => id is not null) is not { } appleAppId)
        {
            Assert.Inconclusive("No sample has an App Store id.");
            return;
        }

        var first = await client.GetFromJsonAsync($"api/store-listing/{appleAppId}", LandingJsonContext.Default.StoreListing);
        var second = await client.GetFromJsonAsync($"api/store-listing/{appleAppId}", LandingJsonContext.Default.StoreListing);

        Assert.AreEqual(1280, first!.RatingCount);
        Assert.AreEqual(4.7, second!.AverageRating);
        Assert.ContainsSingle(uri => uri.Query.Contains(appleAppId, StringComparison.Ordinal), appStore.Requests, "The lookup is cached.");
    }

    [TestMethod]
    [DataRow(2, "2 ratings</span>")]
    [DataRow(1, "1 rating</span>")]
    public async Task AFewRatings_Should_ShowTheRatingInsteadOfTheVersion(int ratingCount, string expectedCount)
    {
        var html = await SampleWithRatings(ratingCount);

        Assert.Contains("4.7 on the App Store", html);
        Assert.Contains(expectedCount, html);
        Assert.DoesNotContain("3.2.0", html);
    }

    [TestMethod]
    public async Task AnAppWithoutRatings_Should_ShowNoRating()
    {
        var html = await SampleWithRatings(0);

        Assert.DoesNotContain("lp-rating", html);
        Assert.DoesNotContain("3.2.0", html);
    }

    [TestMethod]
    public async Task TwoRatingsOnOnePage_Should_BothKeepTheirListingForTheBrowser()
    {
        var log = new RecordingErrorLog();

        await SampleWithRatings(2, builder => builder.Logging.AddProvider(log));

        Assert.IsEmpty(log.Errors, string.Join(Environment.NewLine, log.Errors));
    }

    [TestMethod]
    public async Task AQuestion_Should_BeAnsweredInStreamedEvents()
    {
        var events = await Ask(LandingSites.All[0].Key, "What does it cost?");

        Assert.IsInstanceOfType<BitAiMessageStartedEvent>(events[0]);
        Assert.IsInstanceOfType<BitAiMessageCompletedEvent>(events[^1]);
        Assert.IsGreaterThan(1, events.OfType<BitAiTextDeltaEvent>().Count(), "The answer streams in pieces.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(string.Concat(events.OfType<BitAiTextDeltaEvent>().Select(delta => delta.Delta))));
    }

    [TestMethod]
    public async Task TheGallerysChat_Should_AnswerToo()
    {
        var events = await Ask(LandingChatContext.Gallery, "Which sample suits a café?");

        Assert.IsInstanceOfType<BitAiMessageCompletedEvent>(events[^1]);
        Assert.IsNotEmpty(events.OfType<BitAiTextDeltaEvent>());
    }

    [TestMethod]
    [DataRow("ecommerce", 1001)]
    [DataRow("no-such-sample", 10)]
    public async Task AQuestionTheServerWontTake_Should_GetAnErrorTheVisitorCanRead(string site, int length)
    {
        var events = await Ask(site, new string('a', length));

        var error = (BitAiErrorEvent)events.Single();
        Assert.IsFalse(error.Error.IsRetryable, "Asking the same again gets the same answer.");
        Assert.AreEqual("That question is too long to answer here. Please ask it more briefly.", error.Error.Message);
    }

    [TestMethod]
    public async Task AChatFromAnotherSite_Should_BeRefused()
    {
        var refused = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => Ask(LandingSites.All[0].Key, "Hi", origin: "https://attacker.example"));
        Assert.AreEqual(HttpStatusCode.Forbidden, refused.StatusCode);

        using var plain = await client.GetAsync("api/chat");
        Assert.AreEqual(HttpStatusCode.BadRequest, plain.StatusCode, "The chat only speaks WebSocket.");
    }

    [TestMethod]
    public async Task UnderBlazorServer_TheChat_Should_BeAnsweredWithoutASocket()
    {
        await using var scope = server.Services.CreateAsyncScope();
        var backend = scope.ServiceProvider.GetRequiredService<ILandingChatBackend>();

        var request = BitAiWireSerializer.SerializeRequest(BitAiWireSerializer.ToSnapshot(Question(LandingSites.All[0].Key, "What does it cost?")));
        List<BitAiEvent> events = [];

        await foreach (var item in backend.StreamAsync(request, CancellationToken.None))
        {
            events.Add(BitAiWireSerializer.Deserialize(item)!);
        }

        Assert.IsInstanceOfType<BitAiMessageCompletedEvent>(events[^1]);
        Assert.IsNotEmpty(events.OfType<BitAiTextDeltaEvent>());
    }

    [TestMethod]
    [DataRow("robots.txt")]
    [DataRow("sitemap.xml")]
    public async Task AForgedHost_Should_GetA400InsteadOfLinksToIt(string url)
    {
        var log = new RecordingErrorLog();
        await using var production = await new LandingTestServer().Start(builder => builder.Logging.AddProvider(log), environment: "Production");
        using var productionClient = production.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Host = "evil.example";

        using var response = await productionClient.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("evil.example", await response.Content.ReadAsStringAsync());
        Assert.IsEmpty(log.Errors, string.Join(Environment.NewLine, log.Errors));
    }

    private static async Task<string> SampleWithRatings(int ratingCount, Action<WebApplicationBuilder>? before = null)
    {
        if (LandingSites.Find("ecommerce") is not { } sample)
        {
            Assert.Inconclusive("The sample that shows the rating is gone.");
            return string.Empty;
        }

        await using var store = await new LandingTestServer().Start(before, services => services.AddHttpClient<AppStoreListingService>().ConfigurePrimaryHttpMessageHandler(() => new FakeAppStore { RatingCount = ratingCount }));
        using var storeClient = store.CreateClient();

        return await storeClient.GetStringAsync(sample.Path.Length is 0 ? LandingRoutes.Home : sample.Path);
    }

    private static BitAiRequest Question(string site, string question)
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, question);

        return new(conversation.Messages)
        {
            Context = new Dictionary<string, object?> { [LandingChatContext.Site] = site, [LandingChatContext.Culture] = "en-US" }
        };
    }

    /// <summary>Asks over the chat's WebSocket, the way the page does (See LandingAiTransport).</summary>
    private static async Task<List<BitAiEvent>> Ask(string site, string question, string? origin = null)
    {
        var transport = new BitAiWebSocketTransport(new Uri(server.Address, "api/chat"), new()
        {
            ConfigureSocket = (options, _) =>
            {
                options.SetRequestHeader("Origin", origin ?? server.Address.GetLeftPart(UriPartial.Authority));
                return ValueTask.CompletedTask;
            }
        });

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        List<BitAiEvent> events = [];

        await foreach (var evt in transport.StreamAsync(Question(site, question), timeout.Token))
        {
            events.Add(evt);
        }

        return events;
    }
}
