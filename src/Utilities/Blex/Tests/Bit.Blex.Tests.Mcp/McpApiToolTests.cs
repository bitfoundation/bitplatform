using Bit.Blex.Demo.Server.Services;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// GetBlexApi: the public surface of the three runtime packages, read out of the shipped assemblies and
/// their XML documentation.
/// </summary>
[TestClass]
public class McpApiToolTests
{
    [TestMethod]
    public void The_xml_documentation_of_the_libraries_is_loaded()
    {
        // Without it every reference is a list of bare signatures - the build would still pass.
        Assert.IsGreaterThan(200, BlexXmlDocs.Count, $"Only {BlexXmlDocs.Count} documented members were found; the XML files are missing from the output.");
    }

    [TestMethod]
    public async Task The_index_lists_every_package()
    {
        var index = await McpCall.TextAsync("GetBlexApi");

        foreach (var package in new[] { "## Bit.Blex", "## Bit.Blex.Maui", "## Bit.Blex.Testing" })
        {
            Assert.Contains(package, index, $"The API index has no '{package}' group.");
        }

        // The Blazor integration ships inside Bit.Blex, so its types are listed in that group.
        Assert.IsFalse(index.Contains("## Bit.Blex.Blazor", StringComparison.Ordinal), "The API index lists a Bit.Blex.Blazor package that no longer exists.");

        Assert.Contains("**BlexStoreBase**", index);
        Assert.Contains("**BlexProvider** (Component)", index);
        Assert.Contains("**BlexTestHarness**", index);
    }

    [TestMethod]
    public async Task An_option_reports_the_default_it_really_has()
    {
        var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexOptions" });

        Assert.StartsWith("# BlexOptions (Class)", reference);
        Assert.Contains("**DevToolsName** : `string` = `\"Blex\"`", reference);
        Assert.Contains("Display name shown in Redux DevTools", reference, "The member's XML summary is missing.");
    }

    [TestMethod]
    public async Task The_registration_entry_point_shows_its_full_signatures()
    {
        var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BitBlex" });

        Assert.Contains("# BitBlex (Static class)", reference);
        Assert.Contains("**AddBitBlexServices**(this IServiceCollection services", reference);
        Assert.Contains("**AddBitBlexStore<TStore>**", reference);
        Assert.Contains("ServiceLifetime lifetime = ServiceLifetime.Scoped", reference);
    }

    [TestMethod]
    public async Task A_component_lists_its_blazor_parameters_with_their_defaults()
    {
        var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexProvider" });

        Assert.Contains("## Parameter", reference);
        Assert.Contains("**EnableDevTools** : `bool` = `false`", reference);
        Assert.Contains("**PersistComponentState** : `bool` = `true`", reference);
    }

    [TestMethod]
    public async Task An_attribute_lists_the_properties_a_store_sets_on_it()
    {
        var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexEffectAttribute" });

        Assert.Contains("(Attribute)", reference);
        Assert.Contains("**Concurrency**", reference);
        Assert.Contains("**DebounceMilliseconds**", reference);
    }

    [TestMethod]
    public async Task A_generic_type_is_found_with_or_without_its_arguments()
    {
        var bare = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexEntityAdapter" });
        var withArguments = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexEntityAdapter<Todo, int>" });

        Assert.AreEqual(bare, withArguments);
        Assert.StartsWith("# BlexEntityAdapter<", bare);
    }

    [TestMethod]
    public async Task An_unknown_type_answers_with_the_near_misses()
    {
        Assert.Contains("Did you mean:", await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "Entity" }));
        Assert.Contains("has no public type called 'Reducer'", await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "Reducer" }));
    }

    [TestMethod]
    public async Task Every_public_type_renders()
    {
        foreach (var type in BlexApiCatalog.Types)
        {
            var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = type.Name });

            Assert.StartsWith($"# {type.Name} ({type.Kind})", reference);
        }
    }
}
