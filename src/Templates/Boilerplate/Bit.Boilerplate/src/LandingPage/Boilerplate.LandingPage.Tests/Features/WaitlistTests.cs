using System.Reflection;
using FluentEmail.Core.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Boilerplate.LandingPage.Tests.Features;

[TestClass]
public class WaitlistTests
{
    private static readonly RecordingEmailSender emails = new();
    private static readonly TestClock clock = new(new DateTimeOffset(2027, 3, 1, 9, 0, 0, TimeSpan.Zero));
    private static LandingTestServer server = default!;
    private static HttpClient client = default!;
    private static int visitors;

    private static readonly LandingSite? site = LandingSites.All.FirstOrDefault(candidate => typeof(LandingSites).Assembly.GetTypes()
        .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
        .Any(route => route.Template == $"{candidate.Path}/waitlist/confirm"));

    [ClassInitialize]
    public static async Task Start(TestContext testContext)
    {
        if (site is null)
            return;

        server = await new LandingTestServer().Start(
            before: builder => builder.Services.AddSingleton<TimeProvider>(clock),
            after: services =>
            {
                services.AddFluentEmail("noreply@example.com");
                services.AddSingleton<ISender>(emails);
            });
        client = server.CreateClient();
    }

    [ClassCleanup]
    public static async Task Stop()
    {
        if (site is null)
            return;

        client.Dispose();
        await server.DisposeAsync();
    }

    [TestInitialize]
    public void RequireAWaitlist()
    {
        if (site is null)
        {
            Assert.Inconclusive("No sample collects a waitlist.");
        }
    }

    [TestMethod]
    public async Task AVisitor_Should_JoinConfirmAndLeave()
    {
        using var joined = await Join("ana@example.com");
        var result = await joined.Content.ReadFromJsonAsync(LandingJsonContext.Default.WaitlistJoinResult);

        Assert.AreEqual(HttpStatusCode.OK, joined.StatusCode);
        Assert.IsTrue(result!.ConfirmationSent);

        var email = emails.To("ana@example.com").Single();
        var confirm = Regex.Match(email.Body, "href=\"([^\"]+/waitlist/confirm\\?token=([0-9a-f]+))\"");
        Assert.Contains("/waitlist/unsubscribe?token=", email.Body);
        Assert.IsTrue(confirm.Success, email.Body);

        using var page = await client.GetAsync(new Uri(confirm.Groups[1].Value).PathAndQuery);
        Assert.AreEqual(HttpStatusCode.OK, page.StatusCode, "The link in the email opens a page.");

        var token = confirm.Groups[2].Value;
        using var confirmed = await Post("api/waitlist/confirm", new WaitlistTokenRequest(token));
        Assert.IsTrue((await confirmed.Content.ReadFromJsonAsync(LandingJsonContext.Default.WaitlistConfirmResult))!.Confirmed);

        using var left = await Post("api/waitlist/unsubscribe", new WaitlistTokenRequest(token));
        Assert.AreEqual(HttpStatusCode.NoContent, left.StatusCode);
    }

    [TestMethod]
    public async Task JoiningAgain_Should_SendTheConfirmationAtMostEveryTenMinutes()
    {
        (await Join("ben@example.com")).Dispose();
        (await Join("ben@example.com")).Dispose();
        Assert.HasCount(1, emails.To("ben@example.com"));

        clock.Now += TimeSpan.FromMinutes(11);
        (await Join("ben@example.com")).Dispose();
        Assert.HasCount(2, emails.To("ben@example.com"));
    }

    [TestMethod]
    public async Task ABot_Should_SeeASuccessButGetNoEmail()
    {
        using var honeypot = await Join("bot@example.com", website: "https://spam.example");
        using var tooFast = await Join("fast@example.com", elapsedMilliseconds: 300);

        Assert.IsTrue((await honeypot.Content.ReadFromJsonAsync(LandingJsonContext.Default.WaitlistJoinResult))!.ConfirmationSent);
        Assert.IsTrue((await tooFast.Content.ReadFromJsonAsync(LandingJsonContext.Default.WaitlistJoinResult))!.ConfirmationSent);
        Assert.IsEmpty(emails.To("bot@example.com"));
        Assert.IsEmpty(emails.To("fast@example.com"));
    }

    [TestMethod]
    [DataRow("not-an-email")]
    [DataRow("someone@localhost")]
    public async Task AnInvalidAddress_Should_GetA400(string address)
    {
        using var response = await Join(address);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task AnUnknownToken_Should_GetA404()
    {
        using var response = await Post("api/waitlist/confirm", new WaitlistTokenRequest("00ff00ff"));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task OneVisitor_Should_GetFiveTriesInTenMinutes()
    {
        var visitor = NextVisitor();

        for (var i = 0; i < 5; i++)
        {
            using var allowed = await Join($"try{i}@example.com", visitor);
            Assert.AreEqual(HttpStatusCode.OK, allowed.StatusCode, $"try {i + 1}");
        }

        using var refused = await Join("try5@example.com", visitor);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    private static Task<HttpResponseMessage> Join(string address, string? visitor = null, string? website = null, long elapsedMilliseconds = 4000)
    {
        var request = new WaitlistJoinRequest { Site = site!.Key, Email = address, Culture = "en-US", Website = website, ElapsedMilliseconds = elapsedMilliseconds };

        return client.SendAsync(LandingTestServer.From(visitor ?? NextVisitor(), HttpMethod.Post, "api/waitlist", JsonContent.Create(request, LandingJsonContext.Default.WaitlistJoinRequest)));
    }

    private static Task<HttpResponseMessage> Post(string url, WaitlistTokenRequest request)
    {
        return client.SendAsync(LandingTestServer.From(NextVisitor(), HttpMethod.Post, url, JsonContent.Create(request, LandingJsonContext.Default.WaitlistTokenRequest)));
    }

    private static string NextVisitor() => $"203.0.113.{Interlocked.Increment(ref visitors)}";
}
