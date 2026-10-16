using System.Net;

namespace Bit.Belemetry.Tests;

/// <summary>Answers requests from a queue of responses and records what was sent.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public List<CapturedRequest> Requests { get; } = [];

    /// <summary>The response once the queue is empty.</summary>
    public HttpStatusCode Fallback { get; set; } = HttpStatusCode.OK;

    public FakeHttpMessageHandler Enqueue(HttpStatusCode status, Action<HttpResponseMessage>? configure = null)
    {
        _responses.Enqueue(() =>
        {
            var response = new HttpResponseMessage(status) { Content = new ByteArrayContent([]) };
            configure?.Invoke(response);
            return response;
        });
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        Requests.Add(new CapturedRequest(request, body, DateTime.UtcNow));
        return _responses.Count > 0 ? _responses.Dequeue()() : new HttpResponseMessage(Fallback) { Content = new ByteArrayContent([]) };
    }

    public sealed record CapturedRequest(HttpRequestMessage Message, byte[] Body, DateTime Time);
}
