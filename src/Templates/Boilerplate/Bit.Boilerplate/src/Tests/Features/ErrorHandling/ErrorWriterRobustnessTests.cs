using Boilerplate.Server.Api.Infrastructure.Services;

namespace Boilerplate.Tests.Features.ErrorHandling;

/// <summary>
/// <c>ApiServerExceptionHandler.WriteAsync</c> is the one component that must never be able to fail: it is what
/// turns every exception into a status code, a localized message and a Request-Id. When it throws, ASP.NET Core's
/// <c>ExceptionHandlerMiddleware</c> swallows the secondary failure and rethrows the original, Kestrel resets the
/// headers, and the caller receives <c>500</c> with <c>Content-Length: 0</c> and no content type. The client's
/// <c>ExceptionDelegatingHandler</c> then cannot build an <c>AppProblemDetails</c> from it, so a 400 or a 404 with a
/// perfectly good message surfaces as "Response status code does not indicate success: 500".
/// <para>
/// Both inputs below were reachable from outside: a repeated request header, and an extension-data value whose
/// runtime type has no <c>JsonTypeInfo</c> in <c>AppJsonContext</c>.
/// </para>
/// <para>
/// The handler is exercised directly rather than over HTTP because <see cref="HttpClient"/> cannot send a repeated
/// custom header - it comma-joins the values onto one line, which is not what Kestrel hands the handler.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ErrorWriterRobustnessTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Kestrel represents a repeated header as a <c>StringValues</c> with more than one entry. Reading it with
    /// <c>Single()</c> - which is what the diagnostics block used to do - threw before the logging block ran, so the
    /// request lost both its response body and its enriched log scope.
    /// </summary>
    [TestMethod]
    public async Task ADuplicatedClientVersionHeader_Should_NotAbortTheErrorResponse()
    {
        await using var server = await StartServer();

        var httpContext = BuildHttpContext(server);
        httpContext.Request.Headers["X-App-Version"] = new(["1.0.0", "1.0.0"]);
        httpContext.Request.Headers["X-App-Platform"] = new(["Web", "Android"]);

        var body = await Write(server, httpContext, new ResourceNotFoundException());

        Assert.AreEqual(404, httpContext.Response.StatusCode, $"Body: {body}");

        var problemDetails = JsonDocument.Parse(body).RootElement;

        Assert.AreEqual(nameof(ResourceNotFoundException), problemDetails.GetProperty("key").GetString(),
            $"A malformed header cost the caller the whole error body. Body: {body}");
    }

    /// <summary>
    /// <c>WithExtensionData</c> takes an <c>object?</c> and the value is serialized by its RUNTIME type, so a
    /// source generated resolver throws a <c>NotSupportedException</c> for any type it has no <c>JsonTypeInfo</c>
    /// for - <c>DateTime</c> being the one a template user reaches for first - and the response is aborted
    /// mid-write. That is why the problem details writer serializes with a reflection based resolver instead.
    /// </summary>
    [TestMethod]
    [DataRow("NextRestockDate", "2026-08-03T10:20:30Z", DisplayName = "DateTime, which AppJsonContext does not declare")]
    [DataRow("TryAgainIn", "00:01:00", DisplayName = "TimeSpan, which it does")]
    public async Task ExtensionData_Should_ReachTheClient_WhateverItsRuntimeType(string key, string rawValue)
    {
        await using var server = await StartServer();

        var httpContext = BuildHttpContext(server);

        object value = key is "NextRestockDate" ? DateTime.Parse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
                                                : TimeSpan.Parse(rawValue, CultureInfo.InvariantCulture);

        var body = await Write(server, httpContext, new ConflictException().WithExtensionData(key, value));

        Assert.AreEqual(409, httpContext.Response.StatusCode, $"Body: {body}");

        var problemDetails = JsonDocument.Parse(body).RootElement;

        Assert.AreEqual(nameof(ConflictException), problemDetails.GetProperty("key").GetString(),
            $"The response must still identify the exception the caller has to handle. Body: {body}");

        Assert.IsTrue(problemDetails.TryGetProperty(key, out _),
            $"WithExtensionData is the sanctioned way of sending structured error data to clients. Body: {body}");

        Assert.IsTrue(problemDetails.TryGetProperty("traceId", out _),
            $"The trace id is what an operator uses to find the server-side log of this error. Body: {body}");
    }

    private static DefaultHttpContext BuildHttpContext(AppTestServer server)
    {
        return new DefaultHttpContext
        {
            RequestServices = server.WebApp.Services,
            Request =
            {
                Method = HttpMethod.Get.Method,
                Scheme = Uri.UriSchemeHttps,
                Host = new HostString("localhost"),
                Path = "/api/v1/Test"
            },
            Response = { Body = new MemoryStream() }
        };
    }

    private async Task<string> Write(AppTestServer server, DefaultHttpContext httpContext, Exception exception)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        // The scope's own IHttpContextAccessor is what the handler's [AutoInject] fields resolve against.
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var handler = scope.ServiceProvider.GetRequiredService<ApiServerExceptionHandler>();

        await handler.WriteAsync(new ProblemDetailsContext { HttpContext = httpContext, Exception = exception });

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

        return await new StreamReader(httpContext.Response.Body).ReadToEndAsync(TestContext.CancellationToken);
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }
}
