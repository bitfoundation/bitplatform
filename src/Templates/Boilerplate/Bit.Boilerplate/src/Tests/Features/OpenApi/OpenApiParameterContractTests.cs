using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Features.OpenApi;

/// <summary>
/// <b>Regression test for BP-161.</b>
/// <para>
/// The OpenAPI operation transformer registered in <c>Program.Services</c> used to do
/// <c>operation.Parameters = [new OpenApiParameter { ... Authorization ... }]</c> - a wholesale <b>assignment</b>
/// where an <b>append</b> was needed. Transformers run after the document service has already filled
/// <c>operation.Parameters</c> from the endpoint's <c>ApiDescription</c>, so every path parameter and every
/// non-OData query parameter was discarded; the seven OData query options were re-added afterwards, but only for
/// <c>[EnableQuery]</c> actions, and nothing restored the route parameters.
/// </para>
/// <para>
/// The published document then declared path templates such as <c>/api/v1/Product/Delete/{id}/{version}</c> with
/// no corresponding <c>path</c> parameters, which OpenAPI 3.1 requires. Scalar's "Try it" had no field to fill in,
/// and a client generated from the document took no id argument and requested the literal template.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class OpenApiParameterContractTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Reads the real generated document and compares each path template's <c>{placeholders}</c> against the
    /// <c>path</c> parameters the corresponding operations declare. The first assertion is a control: if the
    /// document contained no templated path at all, a green "no offenders" result would be meaningless.
    /// </summary>
    [TestMethod]
    public async Task ThePublishedOpenApiDocument_Should_DeclareEveryPathParameterItsTemplateUses()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var document = await httpClient.GetStringAsync("openapi/v1.json", TestContext.CancellationToken);

        using var json = JsonDocument.Parse(document);

        var templatedPathCount = 0;
        List<string> offenders = [];

        foreach (var path in json.RootElement.GetProperty("paths").EnumerateObject())
        {
            string[] placeholders = [.. PathPlaceholder().Matches(path.Name).Select(match => match.Groups["name"].Value)];

            if (placeholders.Length is 0)
                continue;

            templatedPathCount++;

            foreach (var operation in path.Value.EnumerateObject())
            {
                string?[] declared = operation.Value.TryGetProperty("parameters", out var parameters)
                    ? [.. parameters.EnumerateArray()
                        .Where(parameter => parameter.TryGetProperty("in", out var location) && location.GetString() is "path")
                        .Select(parameter => parameter.TryGetProperty("name", out var name) ? name.GetString() : null)]
                    : [];

                var missing = placeholders.Except(declared).ToArray();

                if (missing.Length is not 0)
                {
                    offenders.Add($"{operation.Name.ToUpperInvariant()} {path.Name} declares no path parameter for {{{string.Join("}, {", missing)}}}");
                }
            }
        }

        Assert.IsGreaterThan(0, templatedPathCount, "Control: the generated document must contain at least one templated path, otherwise this test proves nothing.");

        Assert.IsEmpty(offenders,
            $"{offenders.Count} of the document's operations on {templatedPathCount} templated path(s) publish no path parameter:{Environment.NewLine}{string.Join(Environment.NewLine, offenders.Take(20))}");
    }

    [GeneratedRegex(@"\{(?<name>[^}]+)\}")]
    private static partial Regex PathPlaceholder();
}
