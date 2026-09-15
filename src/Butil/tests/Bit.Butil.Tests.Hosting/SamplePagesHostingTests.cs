using System.Net;
using System.Reflection;
using Bit.Butil.Samples.Core;
using ButilTests.Hosting.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Modes = ButilTests.Harness.Web.HarnessRenderModes;

namespace ButilTests.Hosting;

/// <summary>
/// Every page of Bit.Butil.Samples.Core - one per browser API, the pages the Samples apps and the documentation
/// point people at - rendered statically in each render mode. Several of them call Butil while they initialize,
/// which is the prerendering mistake the library is built to absorb; this is the check that it does, page by page,
/// in a real host rather than against a stub runtime.
/// </summary>
[TestClass]
public class SamplePagesHostingTests
{
    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task Every_sample_page_renders_statically_without_logging_an_error(string mode)
    {
        var routes = Routes();
        // Discovery by reflection: a page added tomorrow is covered tomorrow. Too few found means the discovery
        // broke, not that the samples shrank.
        Assert.IsGreaterThanOrEqualTo(40, routes.Length, $"Only {routes.Length} sample page routes were found.");

        await using var factory = new HarnessHostFactory(mode);
        using var client = factory.CreateClient();

        var failures = new List<string>();
        foreach (var route in routes)
        {
            using var response = await client.GetAsync(route);
            if (response.StatusCode is not HttpStatusCode.OK) failures.Add($"{route}: {(int)response.StatusCode}");
        }

        var errors = factory.Logs.Entries.Where(e => e.Level >= LogLevel.Error).Select(e => e.ToString());

        var problems = failures.Concat(errors).ToArray();
        Assert.AreEqual(0, problems.Length, string.Join(Environment.NewLine, problems));
    }

    private static string[] Routes() =>
        [.. typeof(Routes).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>(inherit: false))
            .Select(route => route.Template)
            // A template with parameters has no URL of its own to request.
            .Where(template => template.Contains('{') is false)
            .Select(template => template.StartsWith('/') ? template : "/" + template)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)];
}
