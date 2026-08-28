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
        Assert.DoesNotContain("## Inherited parameters", answer);
    }

    /// <summary>
    /// The parameters an input takes from <c>BitInputBase</c> are the ones a form is built out of,
    /// and no input's own table names them: without this line an agent has no way to learn that
    /// <c>Value</c>, let alone <c>@bind-Value</c>, exists on the component it is about to write.
    /// </summary>
    [TestMethod]
    [DataRow("BitTextField", "BitInputBase<string>", "BitTextInputBase<string>")]
    [DataRow("BitDropdown", "BitInputBase<TValue>", null)]
    [DataRow("BitCheckbox", "BitInputBase<bool>", null)]
    public async Task An_input_names_the_parameters_it_takes_from_its_base(string component, string inputBase, string? textInputBase)
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name = component });

        using var scope = Assert.Scope();

        StringAssert.Contains(answer, "## Inherited parameters");
        StringAssert.Contains(answer, inputBase, $"{component} no longer says which BitInputBase it closes.");
        StringAssert.Contains(answer, "`Value`", $"{component} names no Value parameter.");
        StringAssert.Contains(answer, "`@bind-Value`", $"{component} does not say its value is two-way bindable.");
        StringAssert.Contains(answer, "GetBitBlazorUIComponent(name: \"BitInputBase\")");

        if (textInputBase is not null) StringAssert.Contains(answer, textInputBase);
    }

    /// <summary>
    /// Each of the three inherited sets answers on its own, since that is what every component that
    /// has one points at.
    /// </summary>
    [TestMethod]
    [DataRow("BitInputBase", "Value", "ValueChanged", "ValueExpression", "Required", "ReadOnly", "OnChange")]
    [DataRow("BitTextInputBase", "AutoComplete", "AutoFocus", "DebounceTime", "Immediate", "ThrottleTime")]
    public async Task Each_inherited_set_answers_under_its_own_name(string name, params string[] expected)
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name });

        var parameters = TableRows(answer, "## Parameters").Select(row => row[0]).ToArray();

        using var scope = Assert.Scope();

        foreach (var parameter in expected)
        {
            CollectionAssert.Contains(parameters, parameter, $"{name} no longer documents {parameter}.");
        }
    }

    /// <summary>
    /// The tables are hand-written on the demo pages and the components go on gaining parameters, so
    /// what is answered is the table plus whatever the compiled type has that the table has not
    /// caught up with. A parameter this server does not name is one no agent will use.
    /// </summary>
    [TestMethod]
    [DataRow("BitModal", "ShowOverlay")]
    [DataRow("BitText", "Align")]
    [DataRow("BitMarkdownEditor", "SyncScroll")]
    [DataRow("BitCircularTimePicker", "DisablePast")]
    public async Task A_parameter_the_demo_page_never_listed_is_answered_from_the_type(string component, string parameter)
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name = component });

        var parameters = TableRows(answer, "## Parameters").Select(row => row[0]).ToArray();

        CollectionAssert.Contains(parameters, parameter, $"{component}.{parameter} exists on the type but is not answered.");
    }

    /// <summary>
    /// A generic component's constraints decide what its type arguments may be, and nothing else in
    /// an answer says so - a table full of <c>TItem</c> does not tell a caller it has to be a
    /// reference type with a parameterless constructor.
    /// </summary>
    [TestMethod]
    public async Task A_generic_component_states_its_type_constraints()
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name = "BitDropdown" });

        using var scope = Assert.Scope();

        StringAssert.StartsWith(answer, "# BitDropdown<TItem, TValue>");
        StringAssert.Contains(answer, "where TItem : class, new()");
    }

    /// <summary>
    /// A type that goes inside a component's markup is told apart from one it takes an instance of:
    /// a BitDropdownOption that reads as a class is one an agent constructs rather than writes.
    /// </summary>
    [TestMethod]
    [DataRow("BitPivot", "## BitPivotItem (component)")]
    [DataRow("BitDropdown", "## BitDropdownOption<TValue> (component)")]
    [DataRow("BitDropdown", "## BitDropdownItem<TValue> (class)")]
    public async Task A_child_component_is_named_as_a_component_rather_than_a_class(string component, string heading)
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name = component });

        StringAssert.Contains(answer, heading);
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

    /// <summary>
    /// The rule the answer above states for one parameter, held to across the whole table: every
    /// library type a component's signatures name is reachable from the answer that names it.
    /// A type belonging to one component is deliberately left out of the type listing, so a name
    /// this answer does not carry is one an agent would have to guess at.
    /// </summary>
    [TestMethod]
    [DataRow("BitButton", "BitLinkRels", "BitPosition")]
    [DataRow("BitDropdown", "BitDropdownItemsProvider", "BitDropDirection")]
    [DataRow("BitChart", "BitChartOptions", "BitChartType")]
    public async Task A_type_a_signature_names_is_named_back_with_its_members(string component, params string[] expected)
    {
        var answer = await CallAsync("GetBitBlazorUIComponent", new { name = component });

        using var scope = Assert.Scope();

        foreach (var type in expected)
        {
            StringAssert.Contains(answer, $"`{type}`", $"{component} names {type} in a signature but nowhere else in its answer.");

            var reference = await CallAsync("GetBitBlazorUIType", new { typeName = type });

            Assert.DoesNotContain("has no public type called", reference, $"{type} does not resolve by name.");
        }
    }
}
