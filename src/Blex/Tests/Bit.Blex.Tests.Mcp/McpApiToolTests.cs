using Bit.Blex.Demo.Server.Services;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// GetBlexApi: the public surface of the four runtime packages, read out of the shipped assemblies and
/// their XML documentation.
/// </summary>
[TestClass]
public class McpApiToolTests
{
    [TestMethod]
    public void The_xml_documentation_of_the_libraries_is_loaded()
    {
        // Without it every reference is a list of bare signatures - the build would still pass.
        Assert.IsTrue(BlexXmlDocs.Count > 200, $"Only {BlexXmlDocs.Count} documented members were found; the XML files are missing from the output.");
    }

    [TestMethod]
    public async Task The_index_lists_every_package()
    {
        var index = await McpCall.TextAsync("GetBlexApi");

        foreach (var package in new[] { "## Bit.Blex", "## Bit.Blex.Blazor", "## Bit.Blex.Maui", "## Bit.Blex.Testing" })
        {
            StringAssert.Contains(index, package, $"The API index has no '{package}' group.");
        }

        StringAssert.Contains(index, "**BlexStoreBase**");
        StringAssert.Contains(index, "**BlexProvider** (Component)");
        StringAssert.Contains(index, "**BlexTestHarness**");
    }

    [TestMethod]
    public async Task An_option_reports_the_default_it_really_has()
    {
        var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexOptions" });

        StringAssert.StartsWith(reference, "# BlexOptions (Class)");
        StringAssert.Contains(reference, "**DevToolsName** : `string` = `\"Blex\"`");
        StringAssert.Contains(reference, "Display name shown in Redux DevTools", "The member's XML summary is missing.");
    }

    [TestMethod]
    public async Task The_registration_entry_point_shows_its_full_signatures()
    {
        var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BitBlex" });

        StringAssert.Contains(reference, "# BitBlex (Static class)");
        StringAssert.Contains(reference, "**AddBitBlexServices**(this IServiceCollection services");
        StringAssert.Contains(reference, "**AddBitBlexStore<TStore>**");
        StringAssert.Contains(reference, "ServiceLifetime lifetime = ServiceLifetime.Scoped");
    }

    [TestMethod]
    public async Task A_component_lists_its_blazor_parameters_with_their_defaults()
    {
        var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexProvider" });

        StringAssert.Contains(reference, "## Parameter");
        StringAssert.Contains(reference, "**EnableDevTools** : `bool` = `true`");
        StringAssert.Contains(reference, "**PersistComponentState** : `bool` = `true`");
    }

    [TestMethod]
    public async Task An_attribute_lists_the_properties_a_store_sets_on_it()
    {
        var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexEffectAttribute" });

        StringAssert.Contains(reference, "(Attribute)");
        StringAssert.Contains(reference, "**Concurrency**");
        StringAssert.Contains(reference, "**DebounceMilliseconds**");
    }

    [TestMethod]
    public async Task A_generic_type_is_found_with_or_without_its_arguments()
    {
        var bare = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexEntityAdapter" });
        var withArguments = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "BlexEntityAdapter<Todo, int>" });

        Assert.AreEqual(bare, withArguments);
        StringAssert.StartsWith(bare, "# BlexEntityAdapter<");
    }

    [TestMethod]
    public async Task An_unknown_type_answers_with_the_near_misses()
    {
        StringAssert.Contains(await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "Entity" }), "Did you mean:");
        StringAssert.Contains(await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = "Reducer" }), "has no public type called 'Reducer'");
    }

    [TestMethod]
    public async Task Every_public_type_renders()
    {
        foreach (var type in BlexApiCatalog.Types)
        {
            var reference = await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = type.Name });

            StringAssert.StartsWith(reference, $"# {type.Name} ({type.Kind})");
        }
    }
}
