using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// What each tool actually answers with when it is called properly.
/// <para>
/// The assertions are deliberately about substance rather than about "something came back": these
/// tools exist to stop an agent inventing an API, and an answer that is well-formed but empty -
/// a type with no members, a page with no prose, a catalog built from a resource that did not get
/// embedded - fails at exactly that job while passing every structural check. Most of what can go
/// wrong here only goes wrong in a real deployment, which is why the suite talks to one.
/// </para>
/// </summary>
[TestClass]
public class ToolBehaviourTests : McpTestBase
{
    [TestMethod]
    public async Task Api_list_is_the_shipped_public_surface()
    {
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails");

        Assert.IsNull(result.Details, "GetButilApiDetails with no type name is a request for the list, not for a type.");
        Assert.IsNotNull(result.Types, "GetButilApiDetails with no type name must answer with every public type.");

        var types = result.Types!;

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(types);
            Assert.IsGreaterThan(100, types.Length, "Butil is roughly seventy services plus their option types and enums.");

            // Reflected off the assembly, so these are here unless the library dropped them.
            foreach (var expected in new[] { "Clipboard", "LocalStorage", "Geolocation", "ButilSubscription", "ButilEvents" })
            {
                Assert.Contains(expected, types.Select(type => type.Name));
            }

            Assert.AreEqual(types.Length, types.Select(type => type.Name).Distinct(StringComparer.Ordinal).Count(),
                "Two public types answered under the same name, so one of them is unreachable through GetButilApiDetails.");

            // "The 'IsInjectable' ones are the classes you inject by their own name" - the tool's
            // own description. Kind and IsInjectable have to agree about which those are.
            foreach (var type in types)
            {
                Assert.IsFalse(string.IsNullOrEmpty(type.Kind));
                Assert.AreEqual(type.Kind == "Service", type.IsInjectable, $"{type.Name} is a '{type.Kind}' but IsInjectable is {type.IsInjectable}.");
            }

            Assert.IsGreaterThan(40, types.Count(type => type.IsInjectable), "The injectable services are the reason the library exists.");
        }
    }

    [TestMethod]
    public async Task The_api_list_summarises_what_a_caller_picks_from()
    {
        // The listing is read to choose something to call, and what a caller calls is a service or a
        // static class - the summary is what separates two of those names. Everything else on the
        // surface is an options record, a handle, an event-args type or an enum, met in a signature
        // and then looked up by that name. Their summaries were two thirds of a 45,000-character
        // answer to a question each of them answers better on its own.
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails");

        var types = result.Types!;
        var callable = types.Where(type => type.IsInjectable || type.Kind == "Static class").ToArray();
        var rest = types.Except(callable).ToArray();

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(callable);
            Assert.IsNotEmpty(rest);

            // Most carry one - a handful of static classes have no XML summary in the library
            // itself, and this listing cannot invent what was never written.
            Assert.IsGreaterThan(callable.Length - 10, callable.Count(type => string.IsNullOrWhiteSpace(type.Summary) is false),
                "The listing dropped the summaries of the types it exists to be chosen from.");

            foreach (var name in new[] { "Clipboard", "LocalStorage", "Geolocation" })
            {
                Assert.IsFalse(string.IsNullOrEmpty(callable.Single(type => type.Name == name).Summary),
                    $"{name} is listed without the summary that separates it from the name next to it.");
            }

            Assert.IsEmpty(rest.Where(type => string.IsNullOrWhiteSpace(type.Summary) is false),
                "The listing carries summaries for types nobody picks off a list; they are one call away by name.");

            // Every name is still here - the listing lost prose, not reach.
            Assert.Contains("ClipboardItem", types.Select(type => type.Name));
            Assert.Contains("ButilEvents", types.Select(type => type.Name));
        }
    }

    [TestMethod]
    public async Task No_type_reference_exceeds_the_documented_cap()
    {
        // The same promise the document tools keep, on the tool that answers with data. A handful of
        // types are enormous - the extension classes are sixty members with their remarks - and one
        // of them uncapped was 30,000 characters. The members are the answer, so the remarks are
        // what goes, and the reference says where they went.
        foreach (var typeName in new[] { "ElementReferenceExtensions", "ElementReferenceAriaExtensions", "Window", "ButilKeyCodes", "Clipboard" })
        {
            var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName });
            var text = Text(await CallRawAsync("GetButilApiDetails", new { typeName }));

            Assert.IsLessThanOrEqualTo(ButilMcp.MaxDocumentLength + 2_000, text.Length,
                $"{typeName} came back at {text.Length} characters.");

            var details = result.Details;

            Assert.IsNotNull(details);

            // Trimmed or not, every member is still named with its signature: cutting the list would
            // hide the member that was asked about as readily as any other.
            Assert.IsNotEmpty(details!.Members);
            Assert.IsEmpty(details.Members.Where(member => string.IsNullOrWhiteSpace(member.Name)));

            if (details.Members.Any(member => member.Remarks is null) && details.Remarks is not null)
            {
                Assert.IsTrue(details.Members.All(member => member.Remarks is null) is false
                              || details.Remarks!.Contains("omitted", StringComparison.Ordinal),
                              $"{typeName} dropped the remarks without saying so.");
            }
        }
    }

    [TestMethod]
    public async Task The_element_page_reaches_every_extension_class_its_members_live_on()
    {
        // The ElementReference surface is spread over several static classes rather than one, and
        // nothing about a class makes an agent look for it: a member is only findable through the
        // page that names the class it is on. A class added to the library and not to the docs nav
        // is a member the tools cannot answer about, which no consistency check can notice - the
        // page and the reflected assembly would each still be perfectly coherent on their own.
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ElementReferenceExtensions"] = "Click",
            ["ElementReferenceDomExtensions"] = "InsertAdjacentHtml",
            ["ElementReferenceStateExtensions"] = "GetTitle",
            ["ElementReferenceAriaExtensions"] = "SetAriaLabel",
            ["ElementReferenceEventExtensions"] = "SubscribeEvent",
            ["ElementReferenceMediaExtensions"] = "Play",
        };

        var page = (await DocsIndexAsync()).Single(row => row.Slug == "element");

        using (Assert.Scope())
        {
            Assert.IsEmpty(expected.Keys.Except(page.Services, StringComparer.Ordinal),
                "The Element docs page does not name every class its members live on.");

            foreach (var (typeName, member) in expected)
            {
                var details = (await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName })).Details;

                Assert.IsNotNull(details, $"{typeName} has no reference.");
                Assert.Contains(member, details!.Members.Select(m => m.Name),
                    $"{typeName} answers without {member}.");
            }
        }
    }

    [TestMethod]
    public async Task Api_details_carry_the_signatures_and_the_shipped_documentation()
    {
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "Clipboard" });

        Assert.IsNotNull(result.Details, result.Message);

        var details = result.Details!;

        using (Assert.Scope())
        {
            Assert.AreEqual("Clipboard", details.Name);
            Assert.AreEqual("Bit.Butil.Clipboard", details.FullName);
            Assert.AreEqual("Service", details.Kind);
            Assert.AreEqual("@inject Bit.Butil.Clipboard clipboard", details.Inject);
            Assert.AreEqual("/clipboard", details.DocsUrl);

            // The XML documentation is read from beside the assembly at runtime. If the build did
            // not put it there, every summary on this server is silently null - which looks like a
            // working server right up until an agent asks what a member does.
            Assert.IsFalse(string.IsNullOrEmpty(details.Summary),
                "Clipboard has no summary, which means the Bit.Butil XML documentation was not loaded next to the assembly.");

            var writeText = details.Members.FirstOrDefault(member => member.Name == "WriteText");

            Assert.IsNotNull(writeText, "Clipboard.WriteText is the canonical member of the canonical service.");
            Assert.AreEqual("Method", writeText!.Kind);
            Assert.StartsWith("(", writeText.Signature);
            Assert.EndsWith(")", writeText.Signature);
            Assert.Contains("string", writeText.Signature);
            Assert.IsFalse(string.IsNullOrEmpty(writeText.Summary));
        }
    }

    [TestMethod]
    public async Task Api_details_reads_the_const_catalogs_as_their_values()
    {
        // ButilEvents is nothing but its constants: "KeyDown" is useless without "keydown" beside
        // it, and the value is what the caller actually passes to the browser.
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "ButilEvents" });

        Assert.IsNotNull(result.Details, result.Message);

        var fields = result.Details!.Members.Where(member => member.Kind == "Field").ToArray();

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(fields);
            Assert.IsTrue(fields.All(field => string.IsNullOrEmpty(field.Default) is false),
                "A constant catalog whose members report no value tells an agent nothing it can pass to the browser.");
        }
    }

    [TestMethod]
    public async Task Api_details_resolves_a_type_however_it_is_spelled()
    {
        foreach (var spelling in new[] { "Clipboard", "clipboard", "CLIPBOARD", " Clipboard ", "Bit.Butil.Clipboard" })
        {
            var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = spelling });

            Assert.AreEqual("Clipboard", result.Details?.Name, $"'{spelling}' did not resolve to the Clipboard type.");
        }
    }

    [TestMethod]
    public async Task Plan_reports_what_the_page_has_to_arrange_first()
    {
        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "Clipboard" });
        var inspection = plan.Apis.Single();

        using (Assert.Scope())
        {
            Assert.IsTrue(inspection.IsKnown, inspection.Message);
            Assert.AreEqual("Clipboard", inspection.Api);
            Assert.Contains("Clipboard", inspection.Services ?? []);
            Assert.Contains("@inject Bit.Butil.Clipboard clipboard", inspection.Inject ?? []);

            // The whole reason this tool exists: the clipboard needs a secure context, and a page
            // served over plain http fails silently rather than loudly.
            Assert.Contains("Secure context", string.Join(" ", inspection.Requires ?? []), StringComparison.OrdinalIgnoreCase);

            Assert.IsNotEmpty(inspection.NextCalls ?? []);

            // Prerendering is the first mistake anyone makes with this library, so it is stated in
            // every plan - once, in the checklist. Each API used to repeat the same paragraph in a
            // Notes list of its own, which for a five-API feature was the same advice five times
            // over, said again by the checklist, about rules the instructions had already given.
            Assert.Contains("OnAfterRenderAsync", string.Join(" ", plan.Checklist));
        }
    }

    [TestMethod]
    public async Task A_plan_says_each_thing_once()
    {
        // The checklist speaks for the whole set and names the APIs each item applies to, so nothing
        // in a plan needs to repeat it per API. This is the assertion that keeps it that way: a
        // plan of five APIs must not cost five copies of the prerendering paragraph.
        var plan = await CallStructuredAsync<FeaturePlan>(
            "PlanButilFeature", new { apis = "Clipboard, Geolocation, MediaDevices, WakeLock, LocalStorage" });

        using (Assert.Scope())
        {
            Assert.HasCount(5, plan.Apis);

            // Nothing per-API is prose about what to do - the fields are what the API IS.
            foreach (var api in plan.Apis)
            {
                Assert.DoesNotContain("OnAfterRenderAsync", string.Join(" ", api.Requires ?? []),
                    $"{api.Api} carries the plan's advice as well as its own preconditions.");
            }

            // And the checklist itself states each rule once rather than once per API it applies to.
            var prerender = plan.Checklist.Count(item => item.Contains("OnAfterRenderAsync", StringComparison.Ordinal));

            Assert.AreEqual(1, prerender, "The checklist states the prerendering rule more than once.");
        }
    }

    [TestMethod]
    public async Task Plan_answers_a_member_as_a_question_about_its_api()
    {
        // "Geolocation.SubscribeWatch" is a question about Geolocation. Answering "no such type"
        // would be technically true and useless.
        var inspection = await InspectAsync("Geolocation.SubscribeWatch");

        using (Assert.Scope())
        {
            Assert.IsTrue(inspection.IsKnown, inspection.Message);
            Assert.AreEqual("Geolocation", inspection.Api);

            // WatchPosition hands back something that keeps the sensor on until it is disposed.
            Assert.IsNotEmpty(inspection.Disposables ?? [],
                "Geolocation returns handles and subscriptions; reporting nothing to dispose is the leak this list exists to prevent.");
        }
    }

    [TestMethod]
    public async Task Plan_accepts_a_docs_slug()
    {
        // "storage" is the slug of the "Local & Session Storage" page - a name no type goes by, and
        // exactly what a search hit and a docs listing both hand back.
        var inspection = await InspectAsync("storage");

        using (Assert.Scope())
        {
            Assert.IsTrue(inspection.IsKnown, $"A slug straight out of the docs index did not resolve: {inspection.Message}");
            Assert.AreEqual("Local & Session Storage", inspection.Api);
            CollectionAssert.AreEquivalent(new[] { "LocalStorage", "SessionStorage" }, inspection.Services ?? []);
        }
    }

    [TestMethod]
    public async Task Every_docs_slug_is_something_the_planner_can_resolve()
    {
        var pages = await DocsIndexAsync();

        // A guide page documents no API, so it is allowed to be unknown to the inspector; every
        // page that documents services is not.
        var apiPages = pages.Where(page => page.Services.Length > 0).ToArray();

        Assert.IsNotEmpty(apiPages);

        foreach (var page in apiPages)
        {
            var inspection = await InspectAsync(page.Slug);

            Assert.IsTrue(inspection.IsKnown, $"PlanButilFeature(apis: \"{page.Slug}\") answered: {inspection.Message}");
        }
    }

    [TestMethod]
    public async Task Plan_combines_the_consequences_of_a_whole_feature()
    {
        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "MediaDevices, MediaRecorder, FileSystem" });

        using (Assert.Scope())
        {
            Assert.IsEmpty(plan.Unknown, $"The plan did not recognise: {string.Join(", ", plan.Unknown)}.");
            Assert.HasCount(3, plan.Apis);
            Assert.IsTrue(plan.Apis.All(api => api.IsKnown));

            // A camera is the textbook case: HTTPS, a prompt, and hardware held open until disposal.
            Assert.IsTrue(plan.RequiresSecureContext);
            Assert.IsTrue(plan.RequiresPermission);

            Assert.IsNotEmpty(plan.Checklist);
            Assert.Contains("AddBitButilServices()", plan.Checklist[0]);
            Assert.Contains("OnAfterRenderAsync", string.Join(" ", plan.Checklist));
            Assert.Contains("Dispose what you open", string.Join(" ", plan.Checklist));
            Assert.IsNull(plan.Ignored);
        }
    }

    [TestMethod]
    public async Task Plan_accepts_the_separators_an_agent_will_actually_send()
    {
        foreach (var separator in new[] { ", ", "\n", "; ", ",\n" })
        {
            var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = string.Join(separator, "Clipboard", "LocalStorage") });

            Assert.HasCount(2, plan.Apis, $"Splitting on '{separator.Replace("\n", "\\n")}' did not produce two APIs.");
            Assert.IsEmpty(plan.Unknown);
        }
    }

    [TestMethod]
    public async Task Plan_reports_what_it_did_not_plan()
    {
        // The cap is 25 (ButilCapabilityCatalog.MaxPlannedApis). Past it the names come back rather
        // than being silently dropped: a plan that quietly ignores half the feature is worse than
        // no plan, because nothing about it says so.
        var apis = string.Join(", ", Enumerable.Repeat("Clipboard", 30));

        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis });

        using (Assert.Scope())
        {
            Assert.HasCount(25, plan.Apis);
            Assert.IsNotEmpty(plan.Ignored ?? []);
            Assert.HasCount(5, plan.Ignored);
            Assert.Contains("Not planned", string.Join(" ", plan.Checklist));
        }
    }

    [TestMethod]
    public async Task Plan_names_what_it_could_not_resolve_without_failing()
    {
        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "Clipboard, Telepathy" });

        using (Assert.Scope())
        {
            Assert.AreSequenceEqual(new[] { "Telepathy" }, plan.Unknown);
            Assert.AreEqual(1, plan.Apis.Count(api => api.IsKnown), "One bad name must not cost the caller the rest of the plan.");
        }
    }

    [TestMethod]
    public async Task The_docs_index_is_also_the_whole_support_matrix()
    {
        // Two tools once, one now: the rows that say where an API is documented are the rows that
        // say which engines run it. Everything the matrix was consulted for has to still be here,
        // or the fold quietly cost a reader the data rather than a tool description.
        var pages = await DocsIndexAsync();

        var apiPages = pages.Where(page => page.Services.Length > 0).ToArray();

        using (Assert.Scope())
        {
            Assert.IsGreaterThan(40, apiPages.Length);

            foreach (var page in apiPages)
            {
                Assert.IsNotEmpty(page.Title);
                Assert.IsNotEmpty(page.Engines, $"{page.Title} says nothing about which engines implement it.");
            }

            // The matrix is only useful if it distinguishes: a table where every row says the same
            // thing is a table nobody can choose between two APIs with.
            Assert.IsGreaterThan(1, apiPages.Select(page => page.Engines).Distinct(StringComparer.Ordinal).Count());
            Assert.IsTrue(apiPages.Any(page => page.Requires.Length > 0));

            // The preconditions are named, not spelled out - the sentence is a PlanButilFeature away.
            Assert.Contains("Secure context", string.Join(" ", apiPages.SelectMany(page => page.Requires)));
        }
    }

    [TestMethod]
    public async Task Docs_list_is_a_usable_index()
    {
        var pages = await DocsIndexAsync();

        using (Assert.Scope())
        {
            Assert.IsGreaterThan(40, pages.Length);

            foreach (var page in pages)
            {
                Assert.IsNotEmpty(page.Slug);
                Assert.IsNotEmpty(page.Title);
                Assert.IsNotEmpty(page.Group, $"The '{page.Title}' row sits under no group heading.");
            }

            Assert.AreEqual(pages.Length, pages.Select(page => page.Slug).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                "Two pages share a slug, so one of them can never be fetched.");

            Assert.Contains("clipboard", pages.Select(page => page.Slug));
            Assert.Contains("troubleshooting", pages.Select(page => page.Slug));
        }
    }

    [TestMethod]
    public async Task Guide_sections_index_the_readme()
    {
        var listing = Text(await CallAsync("GetButilGuideSection"));
        var sections = await ListAsync("GetButilGuideSection");

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(sections, "No guide sections means the README was not embedded into the published app.");
            Assert.Contains("Getting started", sections);

            // Each entry states how much text asking for it would return, which is what makes the
            // listing worth reading before choosing - and a zero is an empty section.
            Assert.DoesNotContain("(0 lines)", listing);

            foreach (var heading in sections)
            {
                Assert.MatchesRegex($@"`{Regex.Escape(heading)}` \(\d+ lines\)", listing,
                    $"The '{heading}' entry does not say how long the section is.");
            }
        }
    }

    [TestMethod]
    public async Task Every_guide_section_can_be_read_back_by_its_own_heading()
    {
        var sections = await ListAsync("GetButilGuideSection");

        // The listing exists to be used: a heading it hands out has to be a heading the reader
        // accepts, or calling the tool with and without an argument disagree in the one way nothing
        // else would notice.
        foreach (var heading in sections)
        {
            var text = Text(await CallAsync("GetButilGuideSection", new { heading }));

            Assert.MatchesRegex($@"^#{{2,3}} {Regex.Escape(heading)}", text,
                $"Reading back the '{heading}' section did not return that section.");
        }
    }

    [TestMethod]
    public async Task Guide_section_matching_forgives_case_and_punctuation()
    {
        var canonical = Text(await CallAsync("GetButilGuideSection", new { heading = "Getting started" }));

        foreach (var spelling in new[] { "getting started", "GETTING STARTED", "Getting-started", " Getting started " })
        {
            var text = Text(await CallAsync("GetButilGuideSection", new { heading = spelling }));

            Assert.AreEqual(canonical, text, $"'{spelling}' did not find the 'Getting started' section.");
        }
    }

    [TestMethod]
    public async Task Source_files_are_real_working_files()
    {
        var listing = Text(await CallAsync("GetButilSourceFile"));
        var files = await ListAsync("GetButilSourceFile");

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(files, "No source files means the demo's sources were not embedded into the published app.");
            Assert.Contains("Demo/Client/Pages/ClipboardPage.razor", files);

            // The listing is grouped by kind and each entry says how long the file is, which is what
            // makes it a thing to choose from rather than a wall of paths.
            Assert.Contains("## Demo", listing);
            Assert.Contains("## Sample", listing);
            Assert.DoesNotContain("(0 lines)", listing);
        }

        var content = Text(await CallAsync("GetButilSourceFile", new { path = "Demo/Client/Pages/ClipboardPage.razor" }));

        using (Assert.Scope())
        {
            // The page IS the example: it has to be the file, not a description of it.
            Assert.Contains("@page", content);
            Assert.Contains("Clipboard", content);
            Assert.DoesNotContain("No source file at", content);
        }
    }

    [TestMethod]
    public async Task Source_file_paths_are_matched_case_insensitively()
    {
        var content = Text(await CallAsync("GetButilSourceFile", new { path = "demo/client/pages/clipboardpage.razor" }));

        Assert.DoesNotStartWith("No source file at", content,
            "Paths come back out of a listing an agent may re-case; the lookup is declared case-insensitive.");
    }

    [TestMethod]
    public async Task Setup_guide_answers_for_every_hosting_model()
    {
        // The server names its own hosting models when it cannot resolve one, and that sentence is
        // the only list a client ever sees. Checked against the pinned list first: a model added to
        // the server and not here would otherwise go untested by the loop below, and one dropped
        // from the server would leave the loop asserting over an empty set.
        const string marker = "Use one of:";

        var refused = Text(await CallRawAsync("GetButilSetupGuide", new { hostingModel = "no-such-model" }));
        var listed = refused.IndexOf(marker, StringComparison.Ordinal);

        Assert.IsNotEmpty(ButilMcp.HostingModels);
        Assert.IsGreaterThanOrEqualTo(0, listed, $"An unknown hosting model was answered with: {refused}");

        var offered = refused[(listed + marker.Length)..]
            .TrimEnd('.', ' ')
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        CollectionAssert.AreEquivalent(ButilMcp.HostingModels, offered,
            "The hosting models the server offers are not the ones this suite covers.");

        foreach (var model in ButilMcp.HostingModels)
        {
            var text = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = model }));

            using (Assert.Scope())
            {
                Assert.StartsWith("# Setting Butil up", text, $"'{model}' did not return a setup guide.");
                Assert.Contains("## Checklist", text);
                Assert.Contains("AddBitButilServices()", text);
                Assert.Contains("bit-butil.js", text);

                // "as the real files of a working project" - the guide's own promise. Prose with no
                // code in it is the failure mode where the embedded sources did not make the build.
                Assert.Contains("```", text, $"The '{model}' guide contains no code at all.");
            }
        }
    }

    [TestMethod]
    public async Task Setup_guide_accepts_the_names_people_use_for_a_hosting_model()
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["webassembly"] = "wasm",
            ["blazor-wasm"] = "wasm",
            ["WASM"] = "wasm",
            ["blazor web app"] = "web-app",
            ["InteractiveAuto"] = "web-app",
            ["blazor-server"] = "server",
            ["maui"] = "hybrid",
            ["wpf"] = "hybrid",
        };

        foreach (var (alias, canonical) in aliases)
        {
            var aliased = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = alias }));
            var expected = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = canonical }));

            Assert.AreEqual(expected, aliased, $"'{alias}' should be understood as the '{canonical}' hosting model.");
        }
    }

    [TestMethod]
    public async Task Web_app_guide_leads_with_the_mistake_that_hosting_model_invites()
    {
        // Two DI containers is the thing that goes wrong in a Blazor Web App, and it goes wrong
        // silently. If the guide stops saying so, it has stopped being worth calling.
        var text = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = "web-app" }));

        using (Assert.Scope())
        {
            Assert.Contains("BOTH", text);
            Assert.Contains("prerender", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("OnAfterRenderAsync", text);
        }
    }

    [TestMethod]
    public async Task Docs_page_is_the_page_a_human_reads()
    {
        var text = Text(await CallAsync("GetButilDocsPage", new { slug = "clipboard" }));

        using (Assert.Scope())
        {
            Assert.StartsWith("Bit.Butil documentation page: /clipboard", text);
            Assert.Contains("# ", text, "The rendered page carries its own heading.");
            Assert.Contains("Clipboard", text);

            // Flattened to Markdown, not handed over as markup: the point of the conversion is that
            // a client's context is spent on prose rather than on div soup.
            Assert.DoesNotContain("<div", text);
            Assert.DoesNotContain("class=\"", text);

            Assert.DoesNotContain("could not be rendered on the server", text);
        }
    }

    [TestMethod]
    public async Task Docs_page_slugs_are_forgiving_of_a_leading_slash()
    {
        var plain = Text(await CallAsync("GetButilDocsPage", new { slug = "clipboard" }));
        var slashed = Text(await CallAsync("GetButilDocsPage", new { slug = "/clipboard" }));

        Assert.AreEqual(plain, slashed, "A slug copied out of a Url field arrives with its slash.");
    }

    [TestMethod]
    public async Task Every_documentation_page_renders()
    {
        // The single most valuable test here. Each page is a live Blazor component rendered outside
        // the app's router and layout, with no JS runtime; a page that reads something from its
        // surroundings throws, and the tool answers with an apology instead of the documentation.
        // Nothing but rendering all of them finds the one that does.
        var pages = await DocsIndexAsync();

        var failures = new List<string>();

        foreach (var page in pages)
        {
            var text = Text(await CallAsync("GetButilDocsPage", new { slug = page.Slug }));

            if (text.Contains("could not be rendered on the server", StringComparison.Ordinal))
            {
                failures.Add($"/{page.Slug}: {text}");
                continue;
            }

            if (text.Length < 400) failures.Add($"/{page.Slug}: rendered to only {text.Length} characters.");
        }

        Assert.IsEmpty(failures, $"Documentation pages that did not render:\n{string.Join("\n", failures)}");
    }

    [TestMethod]
    public async Task No_answer_exceeds_the_documented_cap()
    {
        // Every document-shaped answer is truncated to the same length, so one page can never eat a
        // client's whole context window. The marker is part of the promise: a client has to be able
        // to tell a complete answer from a cut one.
        var slugs = new[] { "clipboard", "getting-started", "troubleshooting", "mcp-server" };

        foreach (var slug in slugs)
        {
            var text = Text(await CallAsync("GetButilDocsPage", new { slug }));

            Assert.IsLessThanOrEqualTo(ButilMcp.MaxDocumentLength + 200, text.Length,
                $"/{slug} came back at {text.Length} characters, past the cap plus its truncation notice.");

            if (text.Length > ButilMcp.MaxDocumentLength)
            {
                Assert.Contains(ButilMcp.TruncationMarker, text, $"/{slug} was cut without saying so.");
            }
        }
    }
}
