namespace Boilerplate.Tests.Features.Configuration;

[TestClass, TestCategory("IntegrationTest")]
public partial class HttpClientDefaultsTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The default client asked for HTTP/2 or nothing, and Kestrel speaks HTTP/1.1 only on plain http, which is how
    /// Server.Web reaches a standalone api inside Compose: its serverApi check retried until its 5 seconds ran out, on
    /// every run. The same policy failed on https servers without HTTP/2, such as appleid.apple.com.
    /// </summary>
    [TestMethod]
    public async Task TheDefaultHttpClient_Should_Reach_APlainHttpServer()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        Assert.AreEqual(Uri.UriSchemeHttp, server.ApiAppAddress.Scheme);

        var client = server.WebApp.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        using var response = await client.GetAsync(new Uri(server.ApiAppAddress, ".well-known/oauth-authorization-server"), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(HttpVersion.Version11, response.Version);
    }
}
