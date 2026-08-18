using Bunit;
using Bunit.TestDoubles;
using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The claims this server makes about the router, checked against the router.
/// <para>
/// Two of its answers are not documentation but verdicts: which templates the router refuses as
/// ambiguous, and which values a constraint accepts. Both are handed to an agent as facts to write
/// code against, and both could drift from the library without anything failing to compile - the
/// analyzer mirrors the router's collision key by hand, and the constraint examples are written down
/// in a catalog. So they are put to the only authority there is: a live Brouter, registering the
/// same templates and matching the same URLs.
/// </para>
/// <para>
/// Every render gets its own DI scope, because a scope hosts one Brouter and these tests mount one
/// per case.
/// </para>
/// </summary>
[TestClass]
public class RouterOracleTests
{
    /// <summary>The constraint registry the demo configures - what both the router and the tool resolve against.</summary>
    private static readonly BrouterConstraintRegistry _constraints = new ServiceCollection()
        .AddDemoServices()
        .BuildServiceProvider()
        .GetRequiredService<IOptions<BrouterOptions>>()
        .Value.Constraints;

    [TestMethod]
    public void The_analyzers_ambiguity_verdict_is_the_routers_verdict()
    {
        // Pairs chosen for the distinctions the collision key is built around: parameter names and
        // literal casing are ignored, while constraints, optionality and declared defaults are not.
        // No expected answer is written down here - the router's own behavior is the expectation.
        (string A, string B)[] pairs =
        [
            ("/users", "/users"),
            ("/users/{id}", "/users/{userId}"),
            ("/Users", "/users"),
            ("/files/**", "/files/{**path}"),
            ("/files/{*path}", "/files/{**other}"),
            ("/files/{*path:nonfile}", "/files/{*path}"),
            ("/users/{id}", "/users/{id:int}"),
            ("/users/{id:int}", "/users/{id:alpha}"),
            ("/users/{id:int:min(1)}", "/users/{id:min(1):int}"),
            ("/x/{page}", "/x/{page=1}"),
            ("/x/{page?}", "/x/{page}"),
            ("/users", "/users/{id}"),
            ("/a/{x}.{y}", "/a/{p}.{q}"),
            ("/a/{x}.{y}", "/a/{x}-{y}"),
            ("/blog/{action=Index}", "/blog/{action=List}"),
            ("/p/{value:slug}", "/p/{other:slug}"),
        ];

        foreach (var (a, b) in pairs)
        {
            var predicted = BrouterTemplateInspector.Analyze([a, b], _constraints).Ambiguous.Length > 0;
            var refused = RegistrationIsRefused(a, b);

            Assert.AreEqual(refused, predicted,
                refused
                    ? $"The router refuses '{a}' next to '{b}', and AnalyzeBrouterRouteTable calls the pair fine."
                    : $"The router accepts '{a}' next to '{b}', and AnalyzeBrouterRouteTable calls the pair ambiguous.");
        }
    }

    [TestMethod]
    public void Every_constraint_accepts_the_value_the_server_says_it_accepts()
    {
        // The passing example is what an agent is shown as a value that works - and what the
        // documentation site's own TryUrl navigates to.
        foreach (var constraint in ConstraintCatalog.All)
        {
            Assert.AreEqual("matched", MatchOutcome(constraint.Kind, constraint.Token, constraint.PassExample),
                $"'{{value:{constraint.Token}}}' is documented as accepting '{constraint.PassExample}', and the router rejects it.");
        }
    }

    [TestMethod]
    public void Every_constraint_rejects_the_value_the_server_says_it_rejects()
    {
        foreach (var constraint in ConstraintCatalog.All)
        {
            Assert.AreEqual("rejected", MatchOutcome(constraint.Kind, constraint.Token, constraint.FailExample),
                $"'{{value:{constraint.Token}}}' is documented as rejecting '{constraint.FailExample}', and the router accepts it.");
        }
    }

    [TestMethod]
    public void The_inspectors_parameter_names_are_the_ones_the_router_binds()
    {
        // The inspection is read back out of the parsed template through reflection; a property that
        // moved would have the tool reporting a plausible-looking answer about the wrong thing.
        var inspection = BrouterTemplateInspector.Inspect("/c/{kind}/{value:int}", _constraints);

        CollectionAssert.AreEqual(new[] { "kind", "value" }, inspection.ParameterNames);

        // The route matched, so the router parsed the same two parameters out of the same template.
        Assert.AreEqual("matched", MatchOutcome("int", "int", "42"));
    }

    /// <summary>Whether the router refuses to register the two templates side by side.</summary>
    private static bool RegistrationIsRefused(string pathA, string pathB)
    {
        try
        {
            InScope("/__oracle__", context => context.Render<TemplatePairHost>(host => host
                .Add(h => h.PathA, pathA)
                .Add(h => h.PathB, pathB)).Markup);

            return false;
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("ambiguous", StringComparison.Ordinal))
        {
            return true;
        }
    }

    /// <summary>Which of the two competing routes a value ends up on: the constrained one, or the fallback.</summary>
    private static string MatchOutcome(string kind, string token, string value)
    {
        return InScope($"/c/{kind}/{Uri.EscapeDataString(value)}", context => context
            .Render<ConstraintHost>(host => host.Add(h => h.Template, $"/c/{kind}/{{value:{token}}}"))
            .Find("[data-testid=outcome]").TextContent);
    }

    /// <summary>Renders in a container of its own, configured the way the documentation site is.</summary>
    private static T InScope<T>(string url, Func<BunitContext, T> render)
    {
        var context = new BunitContext();

        try
        {
            context.JSInterop.Mode = JSRuntimeMode.Loose;

            // The demo's own registration, custom "slug" constraint included, so the router under
            // test resolves templates exactly as the running site does.
            context.Services.AddDemoServices();
            context.Services.GetRequiredService<BunitNavigationManager>().NavigateTo(url);

            return render(context);
        }
        finally
        {
            // The container may hold IAsyncDisposable-only services, which the synchronous path
            // cannot tear down.
            context.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
