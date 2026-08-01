using System.Net;
using System.Text;

namespace Boilerplate.Tests.Features.ErrorHandling;

/// <summary>
/// Every 4xx and 5xx the API produces is written by <c>ApiServerExceptionHandler.WriteAsync</c> as an
/// <c>AppProblemDetails</c>, so this is the one wire format the whole product shares - and the template publishes an
/// OpenAPI/Scalar surface aimed at consumers that are not this client.
/// <para>
/// It is also a format the template's own client cannot police: System.Text.Json deserialization is <b>last-wins</b>,
/// so a body carrying the same member twice binds correctly here and is broken everywhere else - <c>JsonNode</c>
/// throws on the first property access, a schema validator or first-wins parser reads the <i>other</i> copy, and on a
/// validation failure that means reading an empty <c>payload</c> and losing every field-level message. The bug that
/// caused it (declared properties plus <c>[JsonExtensionData]</c> entries of the same name, which STJ emits without a
/// collision check) shipped on 100% of error responses and nothing noticed, because nothing pinned the format.
/// </para>
/// <para>
/// The requests below are deliberately issued with a bare <see cref="HttpClient"/> rather than the DI one: the client
/// pipeline turns an error response into a typed exception, which is exactly the layer that hides this.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ProblemDetailsWireContractTests
{
    /// <summary>
    /// A <c>KnownException</c> (404 from an anonymous endpoint). No member may appear twice, and <c>key</c> - the
    /// member the client maps back onto an exception type - must be present with the exception's own key rather than
    /// null.
    /// </summary>
    [TestMethod]
    public async Task AKnownExceptionResponse_Should_CarryEveryMemberExactlyOnce()
    {
        await using var server = await StartServer();
        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        // Anonymous, and nothing is stored under a random attachment id, so this is a deterministic ResourceNotFoundException.
        using var response = await httpClient.GetAsync($"api/v1/Attachment/GetAttachment/{Guid.NewGuid()}/UserProfileImageSmall", TestContext.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, $"Unexpected status. Body: {body}");

        AssertNoDuplicateMembers(body);

        var problemDetails = JsonDocument.Parse(body).RootElement;

        Assert.AreEqual(nameof(ResourceNotFoundException), problemDetails.GetProperty("key").GetString(),
            $"`key` is what the client turns back into an exception type. Body: {body}");
        Assert.AreEqual(404, problemDetails.GetProperty("status").GetInt32(), $"Body: {body}");
    }

    /// <summary>
    /// A <c>ResourceValidationException</c>, the case with the most to lose: its <c>payload</c> carries every
    /// field-level message. A duplicate <c>payload</c> meant a first-wins consumer read the empty copy and reported a
    /// validation failure with no reason attached.
    /// </summary>
    [TestMethod]
    public async Task AValidationFailureResponse_Should_CarryExactlyOnePopulatedPayload()
    {
        await using var server = await StartServer();
        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        // Neither a password nor an otp: SignInRequestDto.Validate rejects it, so InvalidModelStateResponseFactory
        // throws a ResourceValidationException. A JSON content type also satisfies AutoCsrfProtectionFilter.
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync("api/v1/Identity/SignIn", content, TestContext.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Unexpected status. Body: {body}");

        AssertNoDuplicateMembers(body);

        var problemDetails = JsonDocument.Parse(body).RootElement;

        Assert.IsTrue(problemDetails.TryGetProperty("payload", out var payload), $"A validation failure must carry its payload. Body: {body}");
        Assert.IsGreaterThan(0, payload.GetProperty("details").GetArrayLength(),
            $"The payload must hold the real field-level errors, not an empty placeholder. Body: {body}");
    }


    /// <summary>
    /// <see cref="JsonDocument"/> silently keeps the last of two same-named members, so the duplicate has to be found
    /// with a raw reader - a parse-and-compare test would have passed against the broken format.
    /// </summary>
    private static void AssertNoDuplicateMembers(string json)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(json), "The error response has no body at all.");

        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        List<string> memberNames = [];
        var depth = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject or JsonTokenType.StartArray:
                    depth++;
                    break;
                case JsonTokenType.EndObject or JsonTokenType.EndArray:
                    depth--;
                    break;
                case JsonTokenType.PropertyName when depth is 1:
                    memberNames.Add(reader.GetString()!);
                    break;
            }
        }

        var duplicates = memberNames.GroupBy(name => name, StringComparer.Ordinal)
                                    .Where(group => group.Count() > 1)
                                    .Select(group => $"'{group.Key}' x{group.Count()}")
                                    .ToArray();

        Assert.IsEmpty(duplicates,
            $"The error body repeats {string.Join(", ", duplicates)}. STJ deserialization is last-wins so this client " +
            $"survives it, but JsonNode throws and a first-wins parser reads the wrong copy. Body: {json}");
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    public TestContext TestContext { get; set; } = default!;
}
