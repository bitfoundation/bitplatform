namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Turns a rejected API call into an exception carrying the server's own answer. The generated typed controllers never
/// look at the status code - in the app that is the client's ExceptionDelegatingHandler's job, and <see cref="TestHost"/>
/// does not build that chain - so without this a 400 quietly deserializes the problem details into a DTO full of nulls
/// and the test fails several asserts later on something unrelated.
/// </summary>
public sealed class ThrowOnApiErrorHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return response;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        response.Dispose();

        throw new HttpRequestException($"{request.Method} {request.RequestUri} answered {(int)response.StatusCode} {response.StatusCode}: {body}");
    }
}
