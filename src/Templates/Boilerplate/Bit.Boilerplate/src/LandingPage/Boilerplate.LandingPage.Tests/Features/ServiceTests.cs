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
    public async Task AQuestion_Should_BeAnsweredInStreamedFrames()
    {
        using var socket = await Connect(server.Address.GetLeftPart(UriPartial.Authority));

        await Send(socket, new LandingChatAsk(LandingSites.All[0].Key, "en-US", [new(true, "What does it cost?")]));
        var frames = await ReceiveAnswer(socket);

        Assert.IsTrue(frames[^1].Done, frames[^1].Error);
        Assert.IsGreaterThan(1, frames.Count(frame => frame.Delta is not null), "The answer streams in pieces.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(string.Concat(frames.Select(frame => frame.Delta))));
    }

    [TestMethod]
    public async Task ATooLongQuestion_Should_GetAnError()
    {
        using var socket = await Connect(server.Address.GetLeftPart(UriPartial.Authority));

        await Send(socket, new LandingChatAsk(LandingSites.All[0].Key, "en-US", [new(true, new string('a', 1001))]));
        var frames = await ReceiveAnswer(socket);

        Assert.IsNotNull(frames.Single().Error);
    }

    [TestMethod]
    public async Task AChatFromAnotherSite_Should_BeRefused()
    {
        await Assert.ThrowsExactlyAsync<WebSocketException>(() => Connect("https://attacker.example"));

        using var plain = await client.GetAsync("api/chat");
        Assert.AreEqual(HttpStatusCode.BadRequest, plain.StatusCode, "The chat only speaks WebSocket.");
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

    private static async Task<ClientWebSocket> Connect(string origin)
    {
        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Origin", origin);

        try
        {
            await socket.ConnectAsync(new UriBuilder(new Uri(server.Address, "api/chat")) { Scheme = "ws" }.Uri, CancellationToken.None);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static Task Send(ClientWebSocket socket, LandingChatAsk ask)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(ask, LandingJsonContext.Default.LandingChatAsk);

        return socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<List<LandingChatFrame>> ReceiveAnswer(ClientWebSocket socket)
    {
        List<LandingChatFrame> frames = [];
        var buffer = new byte[16 * 1024];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        while (true)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
                message.Write(buffer, 0, result.Count);
            }
            while (result.EndOfMessage is false);

            var frame = JsonSerializer.Deserialize(message.ToArray(), LandingJsonContext.Default.LandingChatFrame)!;
            frames.Add(frame);

            if (frame.Done || frame.Error is not null)
                return frames;
        }
    }
}
