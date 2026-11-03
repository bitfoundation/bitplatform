using System.Collections.Concurrent;

namespace Boilerplate.LandingPage.Tests.Infrastructure;

public sealed class FakeAppStore : HttpMessageHandler
{
    public ConcurrentQueue<Uri> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request.RequestUri!);

        var json = """
            {"resultCount":1,"results":[{"trackName":"Sample","averageUserRating":4.66,"userRatingCount":1280,"version":"3.2.0","currentVersionReleaseDate":"2026-10-01T08:00:00Z","fileSizeBytes":"48220160","minimumOsVersion":"16.0","trackViewUrl":"https://apps.apple.com/app/id1"}]}
            """;

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
    }
}
