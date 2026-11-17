using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OData.Query;

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

        await server.Build().Start(TestContext.CancellationToken);

        using var httpClient = server.CreateRawHttpClient();

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

    /// <summary>
    /// An action that takes <c>ODataQueryOptions</c>, such as <c>GetTenants</c>, applies <c>$filter</c>, <c>$orderby</c>,
    /// <c>$top</c> and <c>$skip</c> from the query string. The document used to describe the parameter itself instead, as an
    /// object holding <c>HttpContext</c>, the EDM model and about a hundred types they reach, which no client sends, and listed
    /// none of the query options a client does send.
    /// </summary>
    [TestMethod]
    public async Task AnActionThatTakesODataQueryOptions_Should_PublishItsQueryOptionsRatherThanTheType()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        // The actions are found by their parameters, as the document no longer names the type.
        string[] actions = [.. server.ApiApp.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(description => description.ActionDescriptor.Parameters.Any(parameter => parameter.ParameterType.IsAssignableTo(typeof(ODataQueryOptions))))
            .Select(description => $"/{description.RelativePath}")
            .Distinct()];

        if (actions.Length is 0)
            Assert.Inconclusive("No action of this configuration takes ODataQueryOptions.");

        using var httpClient = server.CreateRawHttpClient();

        using var json = JsonDocument.Parse(await httpClient.GetStringAsync("openapi/v1.json", TestContext.CancellationToken));

        var paths = json.RootElement.GetProperty("paths");
        List<string> offenders = [];

        foreach (var action in actions)
        {
            if (paths.TryGetProperty(action, out var path) is false)
            {
                offenders.Add($"{action} is not in the document");
                continue;
            }

            foreach (var operation in path.EnumerateObject())
            {
                // Each query option by its name, with the type of its schema, which OpenAPI 3.1 requires on a parameter.
                var types = (operation.Value.TryGetProperty("parameters", out var parameters) ? parameters.EnumerateArray().ToArray() : [])
                    .Where(parameter => parameter.TryGetProperty("name", out var name) && name.GetString()?.StartsWith('$') is true)
                    .ToDictionary(
                        parameter => parameter.GetProperty("name").GetString()!,
                        parameter => parameter.TryGetProperty("schema", out var schema) && schema.TryGetProperty("type", out var type) ? type.ToString() : null);

                foreach (var (name, type) in new[] { ("$filter", "string"), ("$orderby", "string"), ("$top", "integer"), ("$skip", "integer") })
                {
                    if (types.TryGetValue(name, out var declared) is false)
                    {
                        offenders.Add($"{operation.Name.ToUpperInvariant()} {action} lists no {name}");
                    }
                    else if (declared != type)
                    {
                        offenders.Add($"{operation.Name.ToUpperInvariant()} {action} gives {name} the schema type {declared ?? "(none)"} instead of {type}");
                    }
                }

                // The action casts what ApplyTo returns to its typed query: $select and $expand would make that throw, and
                // nothing applies $search.
                foreach (var name in new[] { "$select", "$expand", "$search" }.Where(types.ContainsKey))
                {
                    offenders.Add($"{operation.Name.ToUpperInvariant()} {action} lists {name}, which it doesn't apply");
                }
            }
        }

        string[] frameworkSchemas = [.. json.RootElement.GetProperty("components").GetProperty("schemas").EnumerateObject()
            .Select(schema => schema.Name)
            .Where(name => name.StartsWith("ODataQueryOptions", StringComparison.Ordinal) || name is "HttpContext" or "IEdmModel")];

        Assert.IsEmpty(offenders, string.Join(Environment.NewLine, offenders));
        Assert.IsEmpty(frameworkSchemas, $"The document describes {string.Join(", ", frameworkSchemas)}.");
    }

    [GeneratedRegex(@"\{(?<name>[^}]+)\}")]
    private static partial Regex PathPlaceholder();
}
