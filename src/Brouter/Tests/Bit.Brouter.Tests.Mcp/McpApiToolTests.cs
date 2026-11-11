using Bit.Brouter.Demo.Client;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The tools that answer about the shipped library rather than about text someone wrote: the public
/// API read out of the assembly, and the route constraints.
/// <para>
/// These are the answers an agent writes code against, and the whole point of reading them off the
/// assembly is that they cannot drift from it. So the tests check the things that would silently
/// stop being true: that the XML documentation is actually found at run time, that a Blazor
/// parameter is reported as one with the default value it really has, and that a member an agent
/// asks about by name resolves.
/// </para>
/// <para>
/// Both answer in Markdown rather than with an object, which is what these assertions read. That is
/// not a formatting preference: a tool that publishes an output schema sends its answer twice, once
/// as the object and once as the JSON text the spec asks for on its behalf, and a reference nobody
/// validates is the wrong place to spend that.
/// </para>
/// </summary>
[TestClass]
public class McpApiToolTests
{
    private static Task<string> ApiAsync(string? typeName = null)
        => McpCall.TextAsync("GetBrouterApi", typeName is null ? null : new() { ["typeName"] = typeName });

    /// <summary>The index's entries: "- **Name** (Kind) - summary".</summary>
    private static string[] IndexEntries(string index)
        => [.. index.Split('\n').Select(line => line.Trim()).Where(line => line.StartsWith("- **", StringComparison.Ordinal))];

    [TestMethod]
    public async Task The_api_index_covers_the_library_and_classifies_what_it_finds()
    {
        var index = await ApiAsync();

        foreach (var expected in new[] { "Brouter", "Broute", "BrouterLink", "BrouterOutlet", "IBrouter", "BrouterOptions" })
        {
            Assert.Contains($"- **{expected}** (", index, $"'{expected}' is missing from the public API index.");
        }

        Assert.Contains("- **Broute** (Component)", index);
        Assert.Contains("- **IBrouter** (Interface)", index);
        Assert.Contains("- **BrouterScrollMode** (Enum)", index);

        // The index is what a caller picks a type from, so it has to name the call that reads one.
        Assert.Contains("GetBrouterApi(typeName:", index);

        // A nested or compiler-generated type in the index means the filter stopped working.
        Assert.IsFalse(index.Contains("__", StringComparison.Ordinal), "A compiler-generated type is listed in the API index.");
    }

    [TestMethod]
    public async Task Public_types_carry_the_summary_their_xml_documentation_gives_them()
    {
        // The XML file is emitted next to the assembly and has to be copied wherever the app runs.
        // When it is not, every summary comes back empty and the reference degrades to a list of names.
        var entries = IndexEntries(await ApiAsync());

        Assert.IsGreaterThan(30, entries.Length, $"Only {entries.Length} public types are listed at all.");

        var documented = entries.Count(entry => entry.Contains(") - ", StringComparison.Ordinal));

        Assert.IsGreaterThan(entries.Length * 3 / 4, documented,
            $"Only {documented} of {entries.Length} public types have a summary - the Bit.Brouter XML documentation is not being read.");
    }

    [TestMethod]
    public async Task The_index_summarizes_rather_than_reprinting_the_whole_reference()
    {
        // An index is read to choose what to read next. Whole summaries made this listing several
        // times longer than the reference of the one type the caller was going to ask for anyway.
        var index = await ApiAsync();
        var route = await ApiAsync("IBrouterRoute");

        var entry = IndexEntries(index).Single(line => line.StartsWith("- **IBrouterRoute**", StringComparison.Ordinal));

        Assert.IsLessThan(route.Length, entry.Length,
            "The index entry for a type is not shorter than the type's own reference.");
    }

    [TestMethod]
    public async Task A_component_reference_reports_its_blazor_parameters_with_types_and_defaults()
    {
        var broute = await ApiAsync("Broute");

        Assert.StartsWith("# Broute (Component)", broute);
        Assert.Contains("`Bit.Brouter.Broute`", broute);

        // "Parameter" means a Blazor [Parameter] - the distinction an agent needs before writing a tag.
        Assert.Contains("## Parameter", broute);

        var path = MemberLine(broute, "Path");
        Assert.Contains("`string`", path);

        // The default is read off a freshly constructed instance, which is the only way it stays true.
        var group = MemberLine(broute, "Group");
        Assert.Contains("`bool`", group);
        Assert.Contains("= `false`", group);
        Assert.IsTrue(group.Contains(" - ", StringComparison.Ordinal), "The parameter arrived without its documentation.");
    }

    [TestMethod]
    public async Task A_service_reference_reports_its_methods_with_their_signatures()
    {
        var brouter = await ApiAsync("IBrouter");

        Assert.Contains("## Method", brouter);
        Assert.Contains("- **NavigateAsync**(", brouter);

        var navigate = MemberLine(brouter, "NavigateAsync");
        Assert.IsTrue(navigate.Contains(" - ", StringComparison.Ordinal), "IBrouter.NavigateAsync arrived without its documentation.");
    }

    [TestMethod]
    public async Task An_overloaded_method_is_served_as_one_member_per_overload()
    {
        // The catalog keys methods by name AND signature for this reason: collapsing overloads would
        // hide the one an agent needs while still answering "yes, that method exists" - the failure
        // mode this server is built to prevent. IBrouter.ClearKeepAlive is the case in the surface:
        // the parameterless overload and the includeActive one do materially different things.
        var brouter = await ApiAsync("IBrouter");

        Assert.Contains("- **ClearKeepAlive**()", brouter);
        Assert.Contains("- **ClearKeepAlive**(bool includeActive)", brouter);
    }

    [TestMethod]
    public async Task A_members_remarks_are_served_with_it_rather_than_dropped()
    {
        // The remarks carry the caveats - the half of the documentation an agent writing against a
        // member most needs and is least likely to have remembered.
        var broute = await ApiAsync("Broute");

        Assert.Contains("KeepAlive", broute);
        Assert.IsGreaterThan(4000, broute.Length, "The component reference is too short to be carrying the remarks as well.");
    }

    [TestMethod]
    public async Task An_options_reference_reports_a_reference_typed_default_rather_than_null()
    {
        // "Constraints starts out holding a registry" and "Constraints starts out null" are different
        // facts, and only one of them is true.
        var options = await ApiAsync("BrouterOptions");

        Assert.Contains("= `false`", MemberLine(options, "CaseSensitive"));
        Assert.Contains("= `", MemberLine(options, "Constraints"),
            "BrouterOptions.Constraints is reported as having no default, but it starts out holding a registry.");
    }

    [TestMethod]
    public async Task An_enum_reference_reports_its_values()
    {
        var mode = await ApiAsync("BrouterScrollMode");

        Assert.StartsWith("# BrouterScrollMode (Enum)", mode);
        Assert.Contains("## EnumValue", mode);
        Assert.Contains("- **ToTop**", mode);
    }

    [TestMethod]
    public async Task A_type_is_found_by_the_name_a_caller_would_write()
    {
        // Casing, and the generic spelling a caller reads in the index.
        foreach (var typeName in new[] { "broute", "BROUTE" })
        {
            Assert.StartsWith("# Broute (Component)", await ApiAsync(typeName), $"'{typeName}' did not resolve to Broute.");
        }

        Assert.StartsWith("# BrouterAwait", await ApiAsync("BrouterAwait<TValue>"),
            "A generic type does not resolve under the name the index shows for it.");
    }

    [TestMethod]
    public async Task A_misspelled_type_answers_with_the_names_it_could_have_meant()
    {
        var answer = await ApiAsync("Brout");

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Broute", answer);
    }

    [TestMethod]
    public async Task An_unrecognizable_type_points_at_the_index()
    {
        var answer = await ApiAsync("HttpClient");

        Assert.Contains("GetBrouterApi", answer);
        Assert.Contains("index", answer);
    }

    [TestMethod]
    public async Task Every_route_constraint_comes_with_a_rule_and_both_kinds_of_example()
    {
        var constraints = await McpCall.TextAsync("GetBrouterRouteConstraints");

        foreach (var constraint in ConstraintCatalog.All)
        {
            var row = constraints.Split('\n').SingleOrDefault(line => line.StartsWith($"| `{{value:{constraint.Token}}}` |", StringComparison.Ordinal));

            Assert.IsNotNull(row, $"'{constraint.Token}' has no row in the constraint table.");

            var cells = row.Split('|', StringSplitOptions.TrimEntries).Where(cell => cell.Length > 0).ToArray();

            Assert.HasCount(5, cells, $"'{constraint.Token}' does not state all five of token, category, rule, pass and fail.");
            Assert.Contains(constraint.Rule, row, $"'{constraint.Token}' states no rule.");
            Assert.Contains($"`{constraint.PassExample}`", row, $"'{constraint.Token}' shows no passing value.");
            Assert.Contains($"`{constraint.FailExample}`", row, $"'{constraint.Token}' shows no failing value.");
            Assert.IsTrue(constraint.Category is "type" or "validation" or "custom" or "chain", $"'{constraint.Token}' is filed under '{constraint.Category}'.");
        }

        // The custom one the demo registers, and constraint chaining, are what a reader is least
        // likely to guess at and most likely to need.
        Assert.Contains("| custom |", constraints);
        Assert.Contains("| chain |", constraints);

        // A constraint is written in a template, so the table says where to check the finished one.
        Assert.Contains("InspectBrouterRouteTemplates", constraints);
    }

    /// <summary>The rendered line for one member, which is where its type, default and summary are.</summary>
    private static string MemberLine(string reference, string memberName)
    {
        var line = reference.Split('\n')
                           .Select(text => text.Trim())
                           .FirstOrDefault(text => text.StartsWith($"- **{memberName}**", StringComparison.Ordinal));

        Assert.IsNotNull(line, $"'{memberName}' is missing from the reference.");

        return line;
    }
}
