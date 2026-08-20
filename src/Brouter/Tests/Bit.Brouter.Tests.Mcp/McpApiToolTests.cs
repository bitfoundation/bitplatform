using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Dtos;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The tools that answer about the shipped library rather than about text someone wrote: the public
/// API read out of the assembly, the constraints, and the source generator's real output.
/// <para>
/// These are the answers an agent writes code against, and the whole point of reading them off the
/// assembly is that they cannot drift from it. So the tests check the things that would silently
/// stop being true: that the XML documentation is actually found at run time, that a Blazor
/// parameter is reported as one with the default value it really has, and that a member an agent
/// asks about by name resolves.
/// </para>
/// </summary>
[TestClass]
public class McpApiToolTests
{
    [TestMethod]
    public async Task The_api_list_covers_the_library_and_classifies_what_it_finds()
    {
        var types = await McpCall.StructuredAsync<BrouterApiTypeDto[]>("GetBrouterApiList");
        var byName = types.ToDictionary(type => type.Name, StringComparer.Ordinal);

        foreach (var expected in new[] { "Brouter", "Broute", "BrouterLink", "BrouterOutlet", "IBrouter", "BrouterOptions" })
        {
            Assert.IsTrue(byName.ContainsKey(expected), $"'{expected}' is missing from the public API listing.");
        }

        Assert.AreEqual("Component", byName["Broute"].Kind);
        Assert.AreEqual("Interface", byName["IBrouter"].Kind);
        Assert.AreEqual("Enum", byName["BrouterScrollMode"].Kind);

        // A nested or compiler-generated type in the listing means the filter stopped working.
        foreach (var type in types)
        {
            Assert.IsFalse(type.Name.Contains('<') && type.Name.Contains('>') && type.Name.Contains("__", StringComparison.Ordinal),
                $"'{type.Name}' is a compiler-generated type and should not be listed.");
        }
    }

    [TestMethod]
    public async Task Public_types_carry_the_summary_their_xml_documentation_gives_them()
    {
        // The XML file is emitted next to the assembly and has to be copied wherever the app runs.
        // When it is not, every summary comes back null and the reference degrades to a list of names.
        var types = await McpCall.StructuredAsync<BrouterApiTypeDto[]>("GetBrouterApiList");
        var documented = types.Count(type => string.IsNullOrWhiteSpace(type.Summary) is false);

        Assert.IsTrue(documented > types.Length * 3 / 4,
            $"Only {documented} of {types.Length} public types have a summary - the Bit.Brouter XML documentation is not being read.");
    }

    [TestMethod]
    public async Task A_component_reference_reports_its_blazor_parameters_with_types_and_defaults()
    {
        var result = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = "Broute" });

        Assert.IsNull(result.Message);
        Assert.IsNotNull(result.Details);
        Assert.AreEqual("Broute", result.Details.Name);
        Assert.AreEqual("Component", result.Details.Kind);
        Assert.AreEqual("Bit.Brouter.Broute", result.Details.FullName);

        var path = result.Details.Members.Single(member => member.Name == "Path");
        Assert.AreEqual("Parameter", path.Kind, "Path is a Blazor [Parameter] and has to be reported as one.");
        Assert.AreEqual("string", path.Type);

        // The default is read off a freshly constructed instance, which is the only way it stays true.
        var group = result.Details.Members.Single(member => member.Name == "Group");
        Assert.AreEqual("bool", group.Type);
        Assert.AreEqual("false", group.Default);

        Assert.IsFalse(string.IsNullOrWhiteSpace(group.Summary), "The parameter arrived without its documentation.");
    }

    [TestMethod]
    public async Task A_service_reference_reports_its_methods_with_their_signatures()
    {
        var result = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = "IBrouter" });

        Assert.IsNotNull(result.Details);

        var navigate = result.Details.Members.First(member => member.Name.StartsWith("NavigateAsync", StringComparison.Ordinal));

        Assert.AreEqual("Method", navigate.Kind);
        Assert.IsNotNull(navigate.Signature);
        StringAssert.StartsWith(navigate.Signature, "(");
        Assert.IsFalse(string.IsNullOrWhiteSpace(navigate.Summary), "IBrouter.NavigateAsync arrived without its documentation.");
    }

    [TestMethod]
    public async Task An_overloaded_method_is_served_as_one_member_per_overload()
    {
        // The catalog keys methods by name AND signature for this reason: collapsing overloads would
        // hide the one an agent needs while still answering "yes, that method exists" - the failure
        // mode this server is built to prevent. IBrouter.ClearKeepAlive is the case in the surface:
        // the parameterless overload and the includeActive one do materially different things.
        var result = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = "IBrouter" });

        Assert.IsNotNull(result.Details);

        var overloads = result.Details.Members
            .Where(member => member.Name == "ClearKeepAlive")
            .ToArray();

        Assert.AreEqual(2, overloads.Length, "Both ClearKeepAlive overloads must be listed, not just one.");
        CollectionAssert.AreEquivalent(new[] { "()", "(bool includeActive)" }, overloads.Select(o => o.Signature).ToArray());

        foreach (var overload in overloads)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(overload.Summary),
                $"ClearKeepAlive{overload.Signature} arrived without its documentation.");
        }
    }

    [TestMethod]
    public async Task An_options_reference_reports_a_reference_typed_default_rather_than_null()
    {
        // "Constraints starts out holding a registry" and "Constraints starts out null" are different
        // facts, and only one of them is true.
        var result = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = "BrouterOptions" });

        Assert.IsNotNull(result.Details);

        var caseSensitive = result.Details.Members.Single(member => member.Name == "CaseSensitive");
        Assert.AreEqual("false", caseSensitive.Default);

        var constraints = result.Details.Members.Single(member => member.Name == "Constraints");
        Assert.IsNotNull(constraints.Default, "BrouterOptions.Constraints is reported as having no default, but it starts out holding a registry.");
    }

    [TestMethod]
    public async Task An_enum_reference_reports_its_values()
    {
        var result = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = "BrouterScrollMode" });

        Assert.IsNotNull(result.Details);
        Assert.AreEqual("Enum", result.Details.Kind);
        Assert.IsTrue(result.Details.Members.All(member => member.Kind == "EnumValue"));
        CollectionAssert.Contains(result.Details.Members.Select(member => member.Name).ToArray(), "ToTop");
    }

    [TestMethod]
    public async Task A_type_is_found_by_the_name_a_caller_would_write()
    {
        // Casing, and the generic spelling a caller reads in the listing.
        foreach (var typeName in new[] { "broute", "BROUTE" })
        {
            var result = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = typeName });

            Assert.IsNotNull(result.Details, $"'{typeName}' did not resolve to Broute.");
            Assert.AreEqual("Broute", result.Details.Name);
        }

        var generic = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = "BrouterAwait<TValue>" });
        Assert.IsNotNull(generic.Details, "A generic type does not resolve under the name the listing shows for it.");
    }

    [TestMethod]
    public async Task A_misspelled_type_answers_with_the_names_it_could_have_meant()
    {
        var result = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = "Brout" });

        Assert.IsNull(result.Details);
        Assert.IsNotNull(result.Message);
        StringAssert.Contains(result.Message, "Did you mean");
        StringAssert.Contains(result.Message, "Broute");
    }

    [TestMethod]
    public async Task An_unrecognizable_type_points_at_the_listing_tool()
    {
        var result = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = "HttpClient" });

        Assert.IsNull(result.Details);
        StringAssert.Contains(result.Message!, "GetBrouterApiList");
    }

    [TestMethod]
    public async Task Every_route_constraint_comes_with_a_rule_and_both_kinds_of_example()
    {
        var constraints = await McpCall.StructuredAsync<BrouterConstraintDto[]>("GetBrouterRouteConstraints");

        Assert.AreEqual(ConstraintCatalog.All.Count, constraints.Length);

        foreach (var constraint in constraints)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(constraint.Rule), $"'{constraint.Token}' states no rule.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(constraint.PassExample), $"'{constraint.Token}' shows no passing value.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(constraint.FailExample), $"'{constraint.Token}' shows no failing value.");
            Assert.AreNotEqual(constraint.PassExample, constraint.FailExample);
            Assert.IsTrue(constraint.Category is "type" or "validation" or "custom" or "chain", $"'{constraint.Token}' is filed under '{constraint.Category}'.");
        }

        // The custom one the demo registers, and constraint chaining, are what a reader is least
        // likely to guess at and most likely to need.
        CollectionAssert.Contains(constraints.Select(constraint => constraint.Category).ToArray(), "custom");
        CollectionAssert.Contains(constraints.Select(constraint => constraint.Category).ToArray(), "chain");
    }

    [TestMethod]
    public async Task Every_constraints_try_url_is_a_single_escaped_path_segment()
    {
        // The value goes into one URL segment: a passing example carrying a slash, a space or a '#'
        // would otherwise produce a different URL than the one that exercises the constraint.
        var constraints = await McpCall.StructuredAsync<BrouterConstraintDto[]>("GetBrouterRouteConstraints");

        foreach (var constraint in constraints)
        {
            StringAssert.StartsWith(constraint.TryUrl, "/c/");

            var segments = constraint.TryUrl.Split('/');
            Assert.AreEqual(4, segments.Length, $"'{constraint.TryUrl}' is not the three-segment demo route it is meant to be.");
            Assert.AreEqual(constraint.PassExample, Uri.UnescapeDataString(segments[3]),
                $"'{constraint.TryUrl}' does not carry the passing example it advertises.");
        }
    }

    [TestMethod]
    public async Task The_typed_routes_tool_shows_the_generators_real_output_for_this_site()
    {
        var result = await McpCall.StructuredAsync<BrouterTypedRoutesResultDto>("GetBrouterTypedRoutes");

        Assert.IsNull(result.Message, $"The typed-route generator's output could not be read back: {result.Message}");
        Assert.IsNotNull(result.TypedRoutes);
        Assert.AreEqual("Bit.Brouter.Demo.Client", result.TypedRoutes.GeneratedFor);
        StringAssert.Contains(result.TypedRoutes.HowItWorks, "Bit.Brouter.Generators");
        Assert.IsTrue(result.TypedRoutes.Builders.Length > 10, "Barely any URL builders were found; the generator did not run for this build.");
        Assert.IsTrue(result.TypedRoutes.Names.Count > 0, "No route-name constants were found, so NavigateToName has nothing to show.");
    }

    [TestMethod]
    public async Task Every_typed_route_builder_shows_the_url_it_builds()
    {
        var result = await McpCall.StructuredAsync<BrouterTypedRoutesResultDto>("GetBrouterTypedRoutes");

        foreach (var builder in result.TypedRoutes!.Builders)
        {
            StringAssert.StartsWith(builder.Signature, "(", $"'{builder.Method}' shows no parameter list.");

            // A builder is a URL builder: an example that is not a URL means it was invoked with
            // arguments it could not use, and shows the caller nothing.
            if (builder.ExampleUrl is null) continue;

            StringAssert.StartsWith(builder.ExampleUrl, "/", $"'{builder.Method}' produced '{builder.ExampleUrl}', which is not a URL.");
        }

        var withExample = result.TypedRoutes.Builders.Count(builder => builder.ExampleUrl is not null);
        Assert.IsTrue(withExample > result.TypedRoutes.Builders.Length / 2,
            $"Only {withExample} of {result.TypedRoutes.Builders.Length} builders produced an example URL.");
    }

    [TestMethod]
    public async Task Every_named_route_constant_names_a_route_of_this_sites_table()
    {
        var result = await McpCall.StructuredAsync<BrouterTypedRoutesResultDto>("GetBrouterTypedRoutes");
        var routeTable = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/AppRouter.razor" });

        foreach (var (constant, name) in result.TypedRoutes!.Names)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(name), $"BrouterRoutes.Names.{constant} is empty.");
            StringAssert.Contains(routeTable, $"Name=\"{name}\"",
                $"BrouterRoutes.Names.{constant} points at a route named '{name}' that the route table does not declare.");
        }
    }
}
