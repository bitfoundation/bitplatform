using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// How the library's JavaScript reaches the browser, as this server describes it: the single
/// <c>bit-butil.js</c> bundle, the bundle a trimmed publish rebuilds from only the modules the app
/// can still call, and lazy scripts - no script tag at all, one <c>import()</c> per API on first use.
/// <para>
/// This is the one subject where the answer an agent acts on is not a member signature but a
/// <em>choice between three shapes of an app</em>, and every one of them fails silently when it is
/// described wrongly: a script tag written into an app using lazy scripts is dead markup, a missing
/// one in a bundle-mode app is every call doing nothing, and a C#-only switch with no
/// <c>BitButilIncludeScriptModules</c> behind it is an <c>import()</c> of a file the publish dropped.
/// The three shapes are described in four separate places on this server - the instructions, the
/// setup guide, the reference guide's own section and the docs pages - so what is asserted here is
/// that they all still describe the same feature, by the names a consumer types into a csproj.
/// </para>
/// </summary>
[TestClass]
public class ScriptDeliveryTests : McpTestBase
{
    /// <summary>The section of the reference guide that covers the whole feature.</summary>
    private const string GuideHeading = "Shipping only the JavaScript you use";

    /// <summary>
    /// The MSBuild properties a consumer actually types. They are the API of this feature every bit
    /// as much as <c>UseLazyScripts</c> is - and unlike a method name, a misspelling of one is
    /// accepted silently by MSBuild and does nothing at all.
    /// </summary>
    private const string LazyProperty = "BitButilLazyScripts";

    private const string TrimProperty = "BitButilTrimScripts";

    private const string ModulesProperty = "BitButilIncludeScriptModules";

    /// <summary>
    /// What an app publishing WITHOUT trimming has to be told, and the escape hatch beside it. Both are new
    /// answers to a question the older properties answer only for a trimmed publish, so an agent that knows
    /// <see cref="TrimProperty"/> and not these tells an untrimmed app the feature does not apply to it.
    /// </summary>
    private const string ScanProperty = "BitButilScriptScan";

    private const string ModuleItem = "BitButilScriptModule";

    [TestMethod]
    public async Task Every_setup_guide_names_both_ways_to_tree_shake_the_javascript()
    {
        // Step 2 of the checklist is where the script tag is prescribed, so it is where an agent
        // decides whether the app gets one. Both switches belong to every hosting model - lazy
        // scripts work in all four, and the trimming default has to be stated where it does NOT
        // apply as much as where it does.
        foreach (var model in ButilMcp.HostingModels)
        {
            var text = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = model }));

            using (Assert.Scope())
            {
                Assert.Contains(LazyProperty, text, $"The '{model}' guide never mentions {LazyProperty}.");
                Assert.Contains(TrimProperty, text, $"The '{model}' guide never mentions {TrimProperty}.");

                // The switch that decides WHETHER to trim is useless on its own to an app publishing
                // untrimmed, which most hosting models here are: without this one named beside it, the
                // agent reads "on by default in WebAssembly" and concludes the feature is not for them.
                Assert.Contains(ScanProperty, text, $"The '{model}' guide never mentions {ScanProperty}, so an untrimmed publish has nothing to tree-shake against.");
                Assert.Contains(ModuleItem, text, $"The '{model}' guide never mentions {ModuleItem}, which is the only way to keep a module nothing static can see.");
            }
        }
    }

    [TestMethod]
    public async Task The_two_hosting_models_that_differ_say_how_they_differ()
    {
        // Publish-time bundle trimming is on by default in exactly one shape and wrong in another,
        // and that is not something an agent can infer from the property's name. A guide that lists
        // the switch without saying which side of that line its hosting model falls on has told the
        // agent nothing it can act on.
        var wasm = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = "wasm" }));
        var webApp = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = "web-app" }));

        using (Assert.Scope())
        {
            Assert.Contains("trimmed", wasm, StringComparison.OrdinalIgnoreCase);

            // The Web App's trap: two projects, so the property goes in two csproj files - the same
            // mistake as registering the services in only one of the two containers.
            Assert.Contains("both", webApp, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(".csproj", webApp);
        }
    }

    [TestMethod]
    public void The_instructions_do_not_leave_the_script_tag_as_the_only_shape()
    {
        // Rule 2 is in the model's context from initialize onwards and is never re-read. If it says
        // "put the script tag in the host page" and nothing else, an agent adding Butil to an app
        // that uses lazy scripts writes a tag that should not be there - and one asked to turn the
        // feature on has no name to turn it on with.
        var instructions = Mcp.ServerInstructions!;

        using (Assert.Scope())
        {
            Assert.Contains(LazyProperty, instructions,
                $"Nothing in the instructions names {LazyProperty}, so rule 2 reads as though the script tag were mandatory.");
            Assert.Contains("import()", instructions);
        }
    }

    [TestMethod]
    public async Task The_guide_section_that_covers_the_feature_is_listed_and_complete()
    {
        var headings = await ListAsync("GetButilGuideSection");

        Assert.Contains(GuideHeading, headings,
            $"'{GuideHeading}' is not in the guide's section index, so nothing points an agent at it.");

        var section = Text(await CallAsync("GetButilGuideSection", new { heading = GuideHeading }));

        using (Assert.Scope())
        {
            Assert.StartsWith("### " + GuideHeading, section);

            // Both switches, the path the modules are served from, and the override that a C#-only
            // switch needs - the four things the section exists to say.
            Assert.Contains(LazyProperty, section);
            Assert.Contains(TrimProperty, section);
            Assert.Contains(ModulesProperty, section);
            Assert.Contains("_content/Bit.Butil/modules/", section);

            // And what the bundle trimming works FROM, which is a separate question from whether it
            // runs: an agent that only ever sees TrimProperty has no answer for an untrimmed publish.
            Assert.Contains(ScanProperty, section);
            Assert.Contains(ModuleItem, section);
        }
    }

    [TestMethod]
    public async Task The_options_type_carries_the_runtime_switches()
    {
        // BitButilOptions is met in a signature and then looked up by name, which is the one path
        // that leads to it: the type listing deliberately withholds summaries for option types.
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "BitButilOptions" });

        Assert.IsNotNull(result.Details, $"BitButilOptions has no reference: {result.Message}");

        var members = result.Details!.Members.ToDictionary(member => member.Name, StringComparer.Ordinal);

        using (Assert.Scope())
        {
            foreach (var name in new[] { "LazyScripts", "ScriptModulesPath", "FastInvoke" })
            {
                Assert.IsTrue(members.ContainsKey(name),
                    $"BitButilOptions.{name} is missing, so the options overload cannot be used from its reference alone.");

                if (members.TryGetValue(name, out var member))
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(member.Summary),
                        $"BitButilOptions.{name} carries no documentation.");
                }
            }

            // A nullable flag rather than a bool: null is "leave the toggle as the MSBuild property
            // set it", which is the difference between configuring the library and overriding it.
            Assert.AreEqual("bool?", members["LazyScripts"].Type);
        }
    }

    [TestMethod]
    public async Task BitButil_carries_the_script_loading_toggles_and_the_options_overload()
    {
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "BitButil" });

        Assert.IsNotNull(result.Details, $"BitButil has no reference: {result.Message}");

        var members = result.Details!.Members;

        using (Assert.Scope())
        {
            var lazy = members.SingleOrDefault(member => member.Name == "UseLazyScripts");
            Assert.IsNotNull(lazy, "BitButil.UseLazyScripts is not in the reference.");

            // The modules path is optional, and an agent that does not see the default assumes the
            // argument is required and invents a path for it.
            Assert.Contains("modulesPath", lazy!.Signature ?? string.Empty);
            Assert.Contains("= null", lazy.Signature ?? string.Empty);

            Assert.IsTrue(members.Any(member => member.Name == "UseBundledScripts"),
                "BitButil.UseBundledScripts is not in the reference, so nothing documents how to override the switch back.");

            // The overload is what makes options reachable at all: without it in the reference, an
            // agent reading AddBitButilServices sees a no-argument call and stops there.
            Assert.IsTrue(members.Any(member => member.Name == "AddBitButilServices"
                                             && (member.Signature ?? string.Empty).Contains("BitButilOptions", StringComparison.Ordinal)),
                "No AddBitButilServices overload taking BitButilOptions is in the reference.");
        }
    }

    /// <summary>
    /// The feature named the way someone reaches for it, rather than by the identifier they would
    /// already have had to know. None of these queries contains a property or method name.
    /// </summary>
    public static IEnumerable<object[]> Queries =>
    [
        ["tree shaking"],
        ["lazy scripts"],
        ["ship only the scripts my app uses"],
        ["make the bundle smaller"],
        ["load a module on first use instead of a script tag"],
    ];

    [TestMethod]
    [DynamicData(nameof(Queries))]
    public async Task Someone_asking_for_a_smaller_bundle_finds_where_it_is_documented(string query)
    {
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query, limit = 20 });

        Assert.IsNotEmpty(result.Hits, $"'{query}' found nothing. {result.Message}");

        // Any of the four places this is documented is a good answer - each one names the switches
        // and each hit carries the exact call that returns its full text.
        var anchors = new[] { GuideHeading, "getting-started", "UseLazyScripts", "BitButilOptions", "troubleshooting" };

        var found = result.Hits.Any(hit => anchors.Any(anchor =>
            $"{hit.Title} {hit.Context} {hit.Tool} {hit.Snippet}".Contains(anchor, StringComparison.OrdinalIgnoreCase)));

        Assert.IsTrue(found,
            $"'{query}' should lead to where script delivery is documented, but the hits were: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}.");
    }

    [TestMethod]
    public async Task The_getting_started_page_documents_both_switches()
    {
        // The page a human reads and the page the tool serves are the same component, so this also
        // pins that the section did not quietly become a code block with no prose around it.
        var page = Text(await CallAsync("GetButilDocsPage", new { slug = "getting-started" }));

        using (Assert.Scope())
        {
            Assert.Contains(LazyProperty, page);
            Assert.Contains(TrimProperty, page);
            Assert.Contains(ModulesProperty, page);
            Assert.Contains(ScanProperty, page);
            Assert.Contains("AddBitButilServices", page);
        }
    }

    [TestMethod]
    public async Task The_render_modes_page_lists_the_toggles_it_documents()
    {
        // Render modes is where the process-wide toggles are gathered - fast invoke lives there, and
        // the script-loading pair belongs beside it rather than only in a getting-started aside.
        var page = Text(await CallAsync("GetButilDocsPage", new { slug = "render-modes" }));

        using (Assert.Scope())
        {
            Assert.Contains("UseLazyScripts", page);
            Assert.Contains("UseBundledScripts", page);
        }
    }

    [TestMethod]
    public async Task Troubleshooting_covers_what_goes_wrong_with_the_new_shapes()
    {
        // The debug-butil-issue prompt sends an agent to this page first, and the two new shapes
        // bring two new failures with them: a module that 404s, and one API's JavaScript missing
        // from a published bundle. Neither resembles the "no script tag" symptom the page already
        // covered, and both look like the library being broken rather than the app being misbuilt.
        var page = Text(await CallAsync("GetButilDocsPage", new { slug = "troubleshooting" }));

        using (Assert.Scope())
        {
            Assert.Contains("_content/Bit.Butil/modules/", page,
                "Nothing on the troubleshooting page covers a lazy-scripts module failing to load.");
            Assert.Contains(ModulesProperty, page,
                $"The page never names {ModulesProperty}, which is the fix when the module files were not published.");
            Assert.Contains(TrimProperty, page,
                $"The page never names {TrimProperty}, which is how a trimmed bundle is ruled in or out as the cause.");
            Assert.Contains(ModuleItem, page,
                $"The page never names {ModuleItem}, which is the fix once a trimmed bundle IS the cause and the API is reached by reflection.");
        }
    }

    [TestMethod]
    public async Task The_prompts_that_touch_the_script_tag_know_it_is_optional()
    {
        // Both of these tell an agent to check for the script tag. In an app using lazy scripts the
        // tag is deliberately absent, so a prompt that stops at "is it there?" sends the agent to
        // add one - and the real cause goes unlooked-at.
        var prompts = new Dictionary<string, Dictionary<string, object?>>(StringComparer.Ordinal)
        {
            ["add-butil-to-app"] = new(StringComparer.Ordinal) { ["hostingModel"] = "wasm" },
            ["debug-butil-issue"] = new(StringComparer.Ordinal) { ["symptom"] = "every Butil call does nothing" },
        };

        foreach (var (name, arguments) in prompts)
        {
            var rendered = await Mcp.GetPromptAsync(name, arguments, cancellationToken: Ct);

            var text = string.Join("\n", rendered.Messages
                .Select(message => message.Content)
                .OfType<TextContentBlock>()
                .Select(block => block.Text));

            Assert.Contains("Butil", text, $"The '{name}' prompt came back empty.");
            Assert.IsTrue(text.Contains(LazyProperty, StringComparison.Ordinal)
                       || text.Contains("lazy scripts", StringComparison.OrdinalIgnoreCase)
                       || text.Contains("tree-shak", StringComparison.OrdinalIgnoreCase),
                $"The '{name}' prompt talks about the script tag without ever allowing that an app may not have one.");
        }
    }
}
