namespace Boilerplate.Tests.E2E.Features.OpenApi;

/// <summary>
/// The OpenAPI document every deployment publishes (Server.Api's AddOpenApi, and Server.Web's MapOpenApi when the API
/// is integrated): served, OpenAPI 3.1, carrying the operations and schemas a client generator needs, and the XML
/// comments written on the actions (Directory.Build.props turns GenerateDocumentationFile on).
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class OpenApiDocumentTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi")]
    [DataRow(DeployedApps.TodoApi, DisplayName = "TodoApi")]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated API)")]
    public async Task TheDocument_Should_DescribeGetCurrentUser_AndCarryTheXmlComments(string api)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var response = await httpClient.GetAsync(new Uri(new Uri(api), "openapi/v1.json"), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(response.Content.Headers.ContentType, $"{api}openapi/v1.json answered with no Content-Type.");
        Assert.AreEqual("application/json", response.Content.Headers.ContentType.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        var root = document.RootElement;

        var openApiVersion = root.GetProperty("openapi").GetString() ?? "";
        Assert.IsTrue(openApiVersion.StartsWith("3.1", StringComparison.Ordinal), $"AddOpenApi asks for OpenAPI 3.1; {api} serves {openApiVersion}.");

        // ---- The bearer token is a security scheme, not a header parameter the spec tells clients to ignore ----
        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.AreEqual("http", bearer.GetProperty("type").GetString(), $"{api} does not describe the token as an http security scheme.");
        Assert.AreEqual("bearer", bearer.GetProperty("scheme").GetString());

        var paths = root.GetProperty("paths");

        // ---- GetCurrentUser: a GET whose 200 is the UserDto schema ----
        Assert.IsTrue(paths.TryGetProperty("/api/v1/User/GetCurrentUser", out var getCurrentUserPath), $"{api}'s document has no GetCurrentUser.");
        var getCurrentUser = getCurrentUserPath.GetProperty("get");
        Assert.IsTrue(getCurrentUser.TryGetProperty("security", out var getCurrentUserSecurity)
            && getCurrentUserSecurity.EnumerateArray().Any(requirement => requirement.TryGetProperty("Bearer", out _)),
            $"GetCurrentUser needs a token, so {api}'s document should require the Bearer scheme on it.");
        Assert.IsFalse(getCurrentUser.TryGetProperty("parameters", out var getCurrentUserParameters)
            && getCurrentUserParameters.EnumerateArray().Any(parameter => parameter.GetProperty("name").GetString()?.Equals("Authorization", StringComparison.OrdinalIgnoreCase) is true),
            "The token must not be modelled as an Authorization header parameter - the spec says to ignore those.");

        var ok = getCurrentUser.GetProperty("responses").GetProperty("200");
        var schemaRefs = ok.GetProperty("content").EnumerateObject()
            .Select(content => content.Value.TryGetProperty("schema", out var schema) && schema.TryGetProperty("$ref", out var reference) ? reference.GetString() : null)
            .ToArray();
        Assert.Contains("#/components/schemas/UserDto", schemaRefs, $"GetCurrentUser's 200 should be a UserDto. It is: {string.Join(", ", schemaRefs.Distinct())}");

        var userDto = root.GetProperty("components").GetProperty("schemas").GetProperty("UserDto").GetProperty("properties");
        foreach (var property in (string[])["id", "userName", "email", "fullName", "displayName"])
        {
            Assert.IsTrue(userDto.TryGetProperty(property, out _), $"UserDto has no '{property}' in {api}'s document.");
        }

        // ---- The XML comments: IdentityController.SendOtp's summary ----
        var sendOtp = paths.GetProperty("/api/v1/Identity/SendOtp").GetProperty("post");
        var described = (sendOtp.TryGetProperty("summary", out var summary) ? summary.GetString() : null)
            ?? (sendOtp.TryGetProperty("description", out var description) ? description.GetString() : null);
        Assert.IsNotNull(described, $"SendOtp carries no summary or description in {api}'s document, so the XML comments did not make it.");
        Assert.Contains("For either otp or magic link", described);
    }
}
