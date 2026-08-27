using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.BlazorUI.Tests.Mcp.Infrastructure;

namespace Bit.BlazorUI.Tests.Mcp;

/// <summary>
/// The catalog and the per-component answers behind it, checked against each other.
/// <para>
/// Everything here is derived - the nav decides which components exist, the assemblies decide what
/// they are, the demo pages carry the tables - so the failure worth catching is a link in that
/// chain that quietly broke: a component the nav lists whose demo page was renamed, a parameter
/// table that stopped being reachable, an answer that names a follow-up call nothing answers.
/// One walk of the whole catalog catches all three.
/// </para>
/// </summary>
[TestClass]
public class ComponentCatalogTests : McpTestBase
{
    private async Task<string> CatalogAsync() => await OncePerFixtureAsync(() => CallAsync("GetBitBlazorUIComponent"));

    /// <summary>Every component named in the catalog table, in nav order.</summary>
    private async Task<string[]> NamesAsync()
    {
        var catalog = await CatalogAsync();

        return [.. catalog.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Where(line => line.StartsWith("| `Bit", StringComparison.Ordinal))
            .Select(line => line.Split('|')[1].Trim().Trim('`'))
            // The catalog prints a generic component with its type parameters; the tools resolve it
            // by name.
            .Select(name => name.Contains('<', StringComparison.Ordinal) ? name[..name.IndexOf('<', StringComparison.Ordinal)] : name)];
    }

    [TestMethod]
    public async Task Catalog_lists_every_category_with_its_package()
    {
        var catalog = await CatalogAsync();
        var names = await NamesAsync();

        using var scope = Assert.Scope();

        Assert.IsGreaterThan(90, names.Length, "The catalog lost components.");
        Assert.AreEqual(names.Length, names.Distinct(StringComparer.Ordinal).Count(), "The catalog lists a component twice.");

        foreach (var category in new[] { "Buttons", "Inputs", "Layouts", "Lists", "Navs", "Notifications", "Progress", "Surfaces", "Utilities", "Extras", "Legacy" })
        {
            StringAssert.Contains(catalog, $"## {category}", $"The catalog has no {category} section.");
        }

        // The package is the fact about a component that its own signature does not carry, and the
        // one whose absence produces a build error nobody can explain.
        StringAssert.Contains(catalog, "Bit.BlazorUI.Extras");
        StringAssert.Contains(catalog, "Bit.BlazorUI.Legacy");

        // The shared parameters are documented once and pointed at, rather than repeated on each of
        // the hundred-odd rows above.
        StringAssert.Contains(catalog, "BitComponentBase");
    }

    [TestMethod]
    public async Task Every_component_in_the_catalog_answers_with_a_table_and_examples()
    {
        var names = await NamesAsync();

        var answers = await OncePerFixtureAsync(async () =>
        {
            var results = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var name in names) results[name] = await CallAsync("GetBitBlazorUIComponent", new { name });

            return results;
        });

        using var scope = Assert.Scope();

        foreach (var (name, answer) in answers)
        {
            StringAssert.StartsWith(answer, $"# {name}", $"{name} did not resolve to its own answer.");

            // A component with no table at all means its demo page could not be read AND its type
            // could not be reflected - the two independent sources both failing, which is the
            // regression this walk exists to catch.
            Assert.IsTrue(answer.Contains("## Parameters", StringComparison.Ordinal) || answer.Contains("## Members", StringComparison.Ordinal),
                $"{name} has neither a parameter table nor a member table.");

            Assert.Contains("## Worked examples", answer, $"{name} lists no worked examples.");
        }
    }

    [TestMethod]
    public async Task Every_component_resolves_by_its_route_and_by_its_aliases()
    {
        var catalog = await CatalogAsync();

        var aliases = catalog.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Where(line => line.StartsWith("| `Bit", StringComparison.Ordinal))
            .Select(line => line.Split('|'))
            .Where(cells => cells.Length > 3 && string.IsNullOrWhiteSpace(cells[3]) is false)
            .SelectMany(cells => cells[3].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.IsGreaterThan(10, aliases.Length, "The catalog stopped carrying the names other libraries use.");

        using var scope = Assert.Scope();

        // An alias is what someone arriving from another library types. It is worth nothing unless
        // the tools resolve it, and the catalog is the only place that advertises them.
        foreach (var alias in aliases)
        {
            var answer = await CallAsync("GetBitBlazorUIComponent", new { name = alias });

            StringAssert.StartsWith(answer, "# Bit", $"The alias '{alias}' does not resolve to a component.");
        }
    }

    [TestMethod]
    public async Task Base_component_documents_the_parameters_every_component_shares()
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name = "BitComponentBase" });

        var parameters = TableRows(answer, "## Parameters").Select(row => row[0]).ToArray();

        using var scope = Assert.Scope();

        foreach (var expected in new[] { "Class", "Style", "Id", "IsEnabled", "Dir", "Visibility", "HtmlAttributes", "AriaLabel" })
        {
            CollectionAssert.Contains(parameters, expected, $"BitComponentBase no longer documents {expected}.");
        }

        // It is the one component whose answer must not point at itself.
        Assert.DoesNotContain("Inherits the `BitComponentBase` parameters", answer);
    }

    [TestMethod]
    public async Task A_component_from_an_optional_package_says_what_that_package_needs()
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name = "BitChart" });

        using var scope = Assert.Scope();

        StringAssert.Contains(answer, "Bit.BlazorUI.Extras");
        StringAssert.Contains(answer, "AddBitBlazorUIExtrasServices()");
        StringAssert.Contains(answer, "bit.blazorui.extras.css");
        StringAssert.Contains(answer, "bit.blazorui.extras.js");

        // The core package needs no such line: every app that uses the library at all has it.
        var core = await CallAsync("GetBitBlazorUIComponent", new { name = "BitButton" });

        Assert.DoesNotContain("Ships in `Bit.BlazorUI`", core);
    }

    [TestMethod]
    public async Task Shared_types_are_named_rather_than_repeated_on_every_component()
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name = "BitButton" });

        using var scope = Assert.Scope();

        // The component's own type is documented in full, because nothing else documents it.
        StringAssert.Contains(answer, "## BitButtonClassStyles (class)");
        StringAssert.Contains(answer, "## BitButtonType (enum)");

        // The library-wide ones are named with their members and left to GetBitBlazorUIType, because
        // the same handful appears on nearly every component.
        StringAssert.Contains(answer, "## Library types used here");
        StringAssert.Contains(answer, "`BitColor` (enum): Primary, Secondary");

        // Including the shared classes: a class is as much a type a caller has to resolve as an
        // enum is, and BitIconInfo is what BitButton's own Icon parameter takes.
        StringAssert.Contains(answer, "`BitIconInfo` (class)",
            "A shared class the demo page documents was dropped from the answer instead of being named.");

        Assert.DoesNotContain("Primary general color", answer,
            "BitColor's per-value prose is repeated on the component, which is the redundancy the split exists to avoid.");
    }

    [TestMethod]
    public async Task Every_type_a_component_documents_is_either_documented_there_or_named()
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name = "BitButton" });

        // The Icon parameter's type has to be reachable from the answer that names it: either
        // documented in full above, or named as a library type for GetBitBlazorUIType to resolve.
        var icon = await CallAsync("GetBitBlazorUIType", new { typeName = "BitIconInfo" });

        using var scope = Assert.Scope();

        StringAssert.Contains(answer, "BitIconInfo");
        Assert.DoesNotContain("has no public type called", icon, "BitIconInfo does not resolve by name.");
    }
}
