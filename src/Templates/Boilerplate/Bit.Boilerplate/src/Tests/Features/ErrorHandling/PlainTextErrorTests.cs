using System.Text;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

namespace Boilerplate.Tests.Features.ErrorHandling;

/// <summary>
/// Not every error the api writes is a problem details json: an action may answer with a plain localized message,
/// such as <c>AttachmentController</c>'s image validation errors, and with whatever status code fits it. The client's
/// <c>ExceptionDelegatingHandler</c> shows such a message to the user only when the response carries the Request-Id
/// header our server stamps on every response (See <c>UseRequestIdHeader</c>), because a plain text body written by
/// something in front of it - a stopped IIS site, a reverse proxy or a CDN - is never meant for the user.
/// <para>
/// The responses below are served by a stub transport under the app's real handler chain, as the status codes and
/// headers they need are not ones the api can be made to produce on demand.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class PlainTextErrorTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.Conflict)]
    [DataRow(HttpStatusCode.UnprocessableEntity)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task APlainTextErrorWrittenByOurServer_Should_ReachTheUser_WhateverItsStatusCode(HttpStatusCode statusCode)
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();

        const string message = "A message the server wrote for the user.";

        using var httpClient = CreateHttpClient(client, statusCode, message, requestId: Guid.NewGuid().ToString());

        var exception = await Assert.ThrowsExactlyAsync<BadRequestException>(() => httpClient.GetAsync("api/v1/Test", TestContext.CancellationToken));

        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public async Task APlainTextErrorWrittenBySomethingInFrontOfOurServer_Should_NotReachTheUser()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();

        const string message = "The service is unavailable. IIS is in maintenance mode.";

        using var httpClient = CreateHttpClient(client, HttpStatusCode.BadRequest, message, requestId: null);

        var exception = await Assert.ThrowsAsync<Exception>(() => httpClient.GetAsync("api/v1/Test", TestContext.CancellationToken));

        Assert.DoesNotContain(message, exception.Message, exception.ToString());
    }

    /// <summary>
    /// The header the client relies on above: it must be on every response of the api, not only on the problem
    /// details ones, as a plain text error never goes through <c>ApiServerExceptionHandler</c>.
    /// </summary>
    [TestMethod]
    public async Task EveryApiResponse_Should_CarryARequestId()
    {
        await using var server = await StartServer();
        using var httpClient = server.CreateRawHttpClient();

        using var response = await httpClient.GetAsync(".well-known/openid-configuration", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(response.Headers.TryGetValues("Request-Id", out var requestId) && string.IsNullOrWhiteSpace(requestId.Single()) is false,
            "A response without a Request-Id reads to the client as one written by a proxy in front of the api.");
    }

    private static HttpClient CreateHttpClient(AppClient client, HttpStatusCode statusCode, string body, string? requestId)
    {
        var transportHandler = new StubHandler(() =>
        {
            var response = new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "text/plain") };
            if (requestId is not null)
            {
                response.Headers.Add("Request-Id", requestId);
            }
            return response;
        });

        return new HttpClient(client.Services.GetRequiredService<HttpMessageHandlersChainFactory>().Invoke(transportHandler))
        {
            BaseAddress = client.HttpClient.BaseAddress
        };
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        return server;
    }

    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(respond());
        }
    }
}
