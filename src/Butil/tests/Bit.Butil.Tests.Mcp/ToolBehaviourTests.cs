using System.Text.RegularExpressions;
using NUnit.Framework;
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
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ToolBehaviourTests : McpTestBase
{
    [Test]
    public async Task Api_list_is_the_shipped_public_surface()
    {
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails");

        Assert.That(result.Details, Is.Null, "GetButilApiDetails with no type name is a request for the list, not for a type.");
        Assert.That(result.Types, Is.Not.Null, "GetButilApiDetails with no type name must answer with every public type.");

        var types = result.Types!;

        Assert.Multiple(() =>
        {
            Assert.That(types, Is.Not.Empty);
            Assert.That(types.Length, Is.GreaterThan(100), "Butil is roughly sixty services plus their option types and enums.");

            // Reflected off the assembly, so these are here unless the library dropped them.
            foreach (var expected in new[] { "Clipboard", "LocalStorage", "Geolocation", "ButilSubscription", "ButilEvents" })
            {
                Assert.That(types.Select(type => type.Name), Does.Contain(expected));
            }

            Assert.That(types.Select(type => type.Name).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(types.Length),
                "Two public types answered under the same name, so one of them is unreachable through GetButilApiDetails.");

            // "The 'IsInjectable' ones are the classes you inject by their own name" - the tool's
            // own description. Kind and IsInjectable have to agree about which those are.
            foreach (var type in types)
            {
                Assert.That(type.Kind, Is.Not.Null.And.Not.Empty);
                Assert.That(type.IsInjectable, Is.EqualTo(type.Kind == "Service"), $"{type.Name} is a '{type.Kind}' but IsInjectable is {type.IsInjectable}.");
            }

            Assert.That(types.Count(type => type.IsInjectable), Is.GreaterThan(40), "The injectable services are the reason the library exists.");
        });
    }

    [Test]
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

        Assert.Multiple(() =>
        {
            Assert.That(callable, Is.Not.Empty);
            Assert.That(rest, Is.Not.Empty);

            // Most carry one - a handful of static classes have no XML summary in the library
            // itself, and this listing cannot invent what was never written.
            Assert.That(callable.Count(type => string.IsNullOrWhiteSpace(type.Summary) is false),
                Is.GreaterThan(callable.Length - 10),
                "The listing dropped the summaries of the types it exists to be chosen from.");

            foreach (var name in new[] { "Clipboard", "LocalStorage", "Geolocation" })
            {
                Assert.That(callable.Single(type => type.Name == name).Summary, Is.Not.Null.And.Not.Empty,
                    $"{name} is listed without the summary that separates it from the name next to it.");
            }

            Assert.That(rest.Where(type => string.IsNullOrWhiteSpace(type.Summary) is false), Is.Empty,
                "The listing carries summaries for types nobody picks off a list; they are one call away by name.");

            // Every name is still here - the listing lost prose, not reach.
            Assert.That(types.Select(type => type.Name), Does.Contain("ClipboardItem").And.Contain("ButilEvents"));
        });
    }

    [Test]
    public async Task No_type_reference_exceeds_the_documented_cap()
    {
        // The same promise the document tools keep, on the tool that answers with data. A handful of
        // types are enormous - the extension classes are sixty members with their remarks - and one
        // of them uncapped was 30,000 characters. The members are the answer, so the remarks are
        // what goes, and the reference says where they went.
        foreach (var typeName in new[] { "ElementReferenceExtensions", "Window", "ButilKeyCodes", "Clipboard" })
        {
            var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName });
            var text = Text(await CallRawAsync("GetButilApiDetails", new { typeName }));

            Assert.That(text.Length, Is.LessThanOrEqualTo(ButilMcp.MaxDocumentLength + 2_000),
                $"{typeName} came back at {text.Length} characters.");

            var details = result.Details;

            Assert.That(details, Is.Not.Null);

            // Trimmed or not, every member is still named with its signature: cutting the list would
            // hide the member that was asked about as readily as any other.
            Assert.That(details!.Members, Is.Not.Empty);
            Assert.That(details.Members.Where(member => string.IsNullOrWhiteSpace(member.Name)), Is.Empty);

            if (details.Members.Any(member => member.Remarks is null) && details.Remarks is not null)
            {
                Assert.That(details.Members.All(member => member.Remarks is null) is false || details.Remarks!.Contains("omitted", StringComparison.Ordinal),
                    Is.True, $"{typeName} dropped the remarks without saying so.");
            }
        }
    }

    [Test]
    public async Task Api_details_carry_the_signatures_and_the_shipped_documentation()
    {
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "Clipboard" });

        Assert.That(result.Details, Is.Not.Null, result.Message);

        var details = result.Details!;

        Assert.Multiple(() =>
        {
            Assert.That(details.Name, Is.EqualTo("Clipboard"));
            Assert.That(details.FullName, Is.EqualTo("Bit.Butil.Clipboard"));
            Assert.That(details.Kind, Is.EqualTo("Service"));
            Assert.That(details.Inject, Is.EqualTo("@inject Bit.Butil.Clipboard clipboard"));
            Assert.That(details.DocsUrl, Is.EqualTo("/clipboard"));

            // The XML documentation is read from beside the assembly at runtime. If the build did
            // not put it there, every summary on this server is silently null - which looks like a
            // working server right up until an agent asks what a member does.
            Assert.That(details.Summary, Is.Not.Null.And.Not.Empty,
                "Clipboard has no summary, which means the Bit.Butil XML documentation was not loaded next to the assembly.");

            var writeText = details.Members.FirstOrDefault(member => member.Name == "WriteText");

            Assert.That(writeText, Is.Not.Null, "Clipboard.WriteText is the canonical member of the canonical service.");
            Assert.That(writeText!.Kind, Is.EqualTo("Method"));
            Assert.That(writeText.Signature, Does.StartWith("(").And.EndWith(")"));
            Assert.That(writeText.Signature, Does.Contain("string"));
            Assert.That(writeText.Summary, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task Api_details_reads_the_const_catalogs_as_their_values()
    {
        // ButilEvents is nothing but its constants: "KeyDown" is useless without "keydown" beside
        // it, and the value is what the caller actually passes to the browser.
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "ButilEvents" });

        Assert.That(result.Details, Is.Not.Null, result.Message);

        var fields = result.Details!.Members.Where(member => member.Kind == "Field").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(fields, Is.Not.Empty);
            Assert.That(fields.All(field => string.IsNullOrEmpty(field.Default) is false), Is.True,
                "A constant catalog whose members report no value tells an agent nothing it can pass to the browser.");
        });
    }

    [Test]
    public async Task Api_details_resolves_a_type_however_it_is_spelled()
    {
        foreach (var spelling in new[] { "Clipboard", "clipboard", "CLIPBOARD", " Clipboard ", "Bit.Butil.Clipboard" })
        {
            var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = spelling });

            Assert.That(result.Details?.Name, Is.EqualTo("Clipboard"), $"'{spelling}' did not resolve to the Clipboard type.");
        }
    }

    [Test]
    public async Task Plan_reports_what_the_page_has_to_arrange_first()
    {
        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "Clipboard" });
        var inspection = plan.Apis.Single();

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsKnown, Is.True, inspection.Message);
            Assert.That(inspection.Api, Is.EqualTo("Clipboard"));
            Assert.That(inspection.Services, Does.Contain("Clipboard"));
            Assert.That(inspection.Inject, Does.Contain("@inject Bit.Butil.Clipboard clipboard"));

            // The whole reason this tool exists: the clipboard needs a secure context, and a page
            // served over plain http fails silently rather than loudly.
            Assert.That(string.Join(" ", inspection.Requires ?? []), Does.Contain("Secure context").IgnoreCase);

            Assert.That(inspection.NextCalls, Is.Not.Null.And.Not.Empty);

            // Prerendering is the first mistake anyone makes with this library, so it is stated in
            // every plan - once, in the checklist. Each API used to repeat the same paragraph in a
            // Notes list of its own, which for a five-API feature was the same advice five times
            // over, said again by the checklist, about rules the instructions had already given.
            Assert.That(string.Join(" ", plan.Checklist), Does.Contain("OnAfterRenderAsync"));
        });
    }

    [Test]
    public async Task A_plan_says_each_thing_once()
    {
        // The checklist speaks for the whole set and names the APIs each item applies to, so nothing
        // in a plan needs to repeat it per API. This is the assertion that keeps it that way: a
        // plan of five APIs must not cost five copies of the prerendering paragraph.
        var plan = await CallStructuredAsync<FeaturePlan>(
            "PlanButilFeature", new { apis = "Clipboard, Geolocation, MediaDevices, WakeLock, LocalStorage" });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Apis, Has.Length.EqualTo(5));

            // Nothing per-API is prose about what to do - the fields are what the API IS.
            foreach (var api in plan.Apis)
            {
                Assert.That(string.Join(" ", api.Requires ?? []), Does.Not.Contain("OnAfterRenderAsync"),
                    $"{api.Api} carries the plan's advice as well as its own preconditions.");
            }

            // And the checklist itself states each rule once rather than once per API it applies to.
            var prerender = plan.Checklist.Count(item => item.Contains("OnAfterRenderAsync", StringComparison.Ordinal));

            Assert.That(prerender, Is.EqualTo(1), "The checklist states the prerendering rule more than once.");
        });
    }

    [Test]
    public async Task Plan_answers_a_member_as_a_question_about_its_api()
    {
        // "Geolocation.SubscribeWatch" is a question about Geolocation. Answering "no such type"
        // would be technically true and useless.
        var inspection = await InspectAsync("Geolocation.SubscribeWatch");

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsKnown, Is.True, inspection.Message);
            Assert.That(inspection.Api, Is.EqualTo("Geolocation"));

            // WatchPosition hands back something that keeps the sensor on until it is disposed.
            Assert.That(inspection.Disposables, Is.Not.Null.And.Not.Empty,
                "Geolocation returns handles and subscriptions; reporting nothing to dispose is the leak this list exists to prevent.");
        });
    }

    [Test]
    public async Task Plan_accepts_a_docs_slug()
    {
        // "storage" is the slug of the "Local & Session Storage" page - a name no type goes by, and
        // exactly what a search hit and a docs listing both hand back.
        var inspection = await InspectAsync("storage");

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsKnown, Is.True, $"A slug straight out of the docs index did not resolve: {inspection.Message}");
            Assert.That(inspection.Api, Is.EqualTo("Local & Session Storage"));
            Assert.That(inspection.Services, Is.EquivalentTo(new[] { "LocalStorage", "SessionStorage" }));
        });
    }

    [Test]
    public async Task Every_docs_slug_is_something_the_planner_can_resolve()
    {
        var pages = await DocsIndexAsync();

        // A guide page documents no API, so it is allowed to be unknown to the inspector; every
        // page that documents services is not.
        var apiPages = pages.Where(page => page.Services.Length > 0).ToArray();

        Assert.That(apiPages, Is.Not.Empty);

        foreach (var page in apiPages)
        {
            var inspection = await InspectAsync(page.Slug);

            Assert.That(inspection.IsKnown, Is.True, $"PlanButilFeature(apis: \"{page.Slug}\") answered: {inspection.Message}");
        }
    }

    [Test]
    public async Task Plan_combines_the_consequences_of_a_whole_feature()
    {
        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "MediaDevices, MediaRecorder, FileSystem" });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Unknown, Is.Empty, $"The plan did not recognise: {string.Join(", ", plan.Unknown)}.");
            Assert.That(plan.Apis, Has.Length.EqualTo(3));
            Assert.That(plan.Apis.All(api => api.IsKnown), Is.True);

            // A camera is the textbook case: HTTPS, a prompt, and hardware held open until disposal.
            Assert.That(plan.RequiresSecureContext, Is.True);
            Assert.That(plan.RequiresPermission, Is.True);

            Assert.That(plan.Checklist, Is.Not.Empty);
            Assert.That(plan.Checklist[0], Does.Contain("AddBitButilServices()"));
            Assert.That(string.Join(" ", plan.Checklist), Does.Contain("OnAfterRenderAsync"));
            Assert.That(string.Join(" ", plan.Checklist), Does.Contain("Dispose what you open"));
            Assert.That(plan.Ignored, Is.Null);
        });
    }

    [Test]
    public async Task Plan_accepts_the_separators_an_agent_will_actually_send()
    {
        foreach (var separator in new[] { ", ", "\n", "; ", ",\n" })
        {
            var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = string.Join(separator, "Clipboard", "LocalStorage") });

            Assert.That(plan.Apis, Has.Length.EqualTo(2), $"Splitting on '{separator.Replace("\n", "\\n")}' did not produce two APIs.");
            Assert.That(plan.Unknown, Is.Empty);
        }
    }

    [Test]
    public async Task Plan_reports_what_it_did_not_plan()
    {
        // The cap is 25 (ButilCapabilityCatalog.MaxPlannedApis). Past it the names come back rather
        // than being silently dropped: a plan that quietly ignores half the feature is worse than
        // no plan, because nothing about it says so.
        var apis = string.Join(", ", Enumerable.Repeat("Clipboard", 30));

        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Apis, Has.Length.EqualTo(25));
            Assert.That(plan.Ignored, Is.Not.Null.And.Not.Empty);
            Assert.That(plan.Ignored, Has.Length.EqualTo(5));
            Assert.That(string.Join(" ", plan.Checklist), Does.Contain("Not planned"));
        });
    }

    [Test]
    public async Task Plan_names_what_it_could_not_resolve_without_failing()
    {
        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "Clipboard, Telepathy" });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Unknown, Is.EqualTo(new[] { "Telepathy" }).AsCollection);
            Assert.That(plan.Apis.Count(api => api.IsKnown), Is.EqualTo(1), "One bad name must not cost the caller the rest of the plan.");
        });
    }

    [Test]
    public async Task The_docs_index_is_also_the_whole_support_matrix()
    {
        // Two tools once, one now: the rows that say where an API is documented are the rows that
        // say which engines run it. Everything the matrix was consulted for has to still be here,
        // or the fold quietly cost a reader the data rather than a tool description.
        var pages = await DocsIndexAsync();

        var apiPages = pages.Where(page => page.Services.Length > 0).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(apiPages.Length, Is.GreaterThan(40));

            foreach (var page in apiPages)
            {
                Assert.That(page.Title, Is.Not.Empty);
                Assert.That(page.Engines, Is.Not.Empty, $"{page.Title} says nothing about which engines implement it.");
            }

            // The matrix is only useful if it distinguishes: a table where every row says the same
            // thing is a table nobody can choose between two APIs with.
            Assert.That(apiPages.Select(page => page.Engines).Distinct(StringComparer.Ordinal).Count(), Is.GreaterThan(1));
            Assert.That(apiPages.Any(page => page.Requires.Length > 0), Is.True);

            // The preconditions are named, not spelled out - the sentence is a PlanButilFeature away.
            Assert.That(string.Join(" ", apiPages.SelectMany(page => page.Requires)), Does.Contain("Secure context"));
        });
    }

    [Test]
    public async Task Docs_list_is_a_usable_index()
    {
        var pages = await DocsIndexAsync();

        Assert.Multiple(() =>
        {
            Assert.That(pages.Length, Is.GreaterThan(40));

            foreach (var page in pages)
            {
                Assert.That(page.Slug, Is.Not.Empty);
                Assert.That(page.Title, Is.Not.Empty);
                Assert.That(page.Group, Is.Not.Empty, $"The '{page.Title}' row sits under no group heading.");
            }

            Assert.That(pages.Select(page => page.Slug).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(pages.Length),
                "Two pages share a slug, so one of them can never be fetched.");

            Assert.That(pages.Select(page => page.Slug), Does.Contain("clipboard"));
            Assert.That(pages.Select(page => page.Slug), Does.Contain("troubleshooting"));
        });
    }

    [Test]
    public async Task Guide_sections_index_the_readme()
    {
        var listing = Text(await CallAsync("GetButilGuideSection"));
        var sections = await ListAsync("GetButilGuideSection");

        Assert.Multiple(() =>
        {
            Assert.That(sections, Is.Not.Empty, "No guide sections means the README was not embedded into the published app.");
            Assert.That(sections, Does.Contain("Getting started"));

            // Each entry states how much text asking for it would return, which is what makes the
            // listing worth reading before choosing - and a zero is an empty section.
            Assert.That(listing, Does.Not.Contain("(0 lines)"));

            foreach (var heading in sections)
            {
                Assert.That(listing, Does.Match($@"`{Regex.Escape(heading)}` \(\d+ lines\)"),
                    $"The '{heading}' entry does not say how long the section is.");
            }
        });
    }

    [Test]
    public async Task Every_guide_section_can_be_read_back_by_its_own_heading()
    {
        var sections = await ListAsync("GetButilGuideSection");

        // The listing exists to be used: a heading it hands out has to be a heading the reader
        // accepts, or calling the tool with and without an argument disagree in the one way nothing
        // else would notice.
        foreach (var heading in sections)
        {
            var text = Text(await CallAsync("GetButilGuideSection", new { heading }));

            Assert.That(text, Does.Match($@"^#{{2,3}} {Regex.Escape(heading)}"),
                $"Reading back the '{heading}' section did not return that section.");
        }
    }

    [Test]
    public async Task Guide_section_matching_forgives_case_and_punctuation()
    {
        var canonical = Text(await CallAsync("GetButilGuideSection", new { heading = "Getting started" }));

        foreach (var spelling in new[] { "getting started", "GETTING STARTED", "Getting-started", " Getting started " })
        {
            var text = Text(await CallAsync("GetButilGuideSection", new { heading = spelling }));

            Assert.That(text, Is.EqualTo(canonical), $"'{spelling}' did not find the 'Getting started' section.");
        }
    }

    [Test]
    public async Task Source_files_are_real_working_files()
    {
        var listing = Text(await CallAsync("GetButilSourceFile"));
        var files = await ListAsync("GetButilSourceFile");

        Assert.Multiple(() =>
        {
            Assert.That(files, Is.Not.Empty, "No source files means the demo's sources were not embedded into the published app.");
            Assert.That(files, Does.Contain("Demo/Client/Pages/ClipboardPage.razor"));

            // The listing is grouped by kind and each entry says how long the file is, which is what
            // makes it a thing to choose from rather than a wall of paths.
            Assert.That(listing, Does.Contain("## Demo").And.Contain("## Sample"));
            Assert.That(listing, Does.Not.Contain("(0 lines)"));
        });

        var content = Text(await CallAsync("GetButilSourceFile", new { path = "Demo/Client/Pages/ClipboardPage.razor" }));

        Assert.Multiple(() =>
        {
            // The page IS the example: it has to be the file, not a description of it.
            Assert.That(content, Does.Contain("@page"));
            Assert.That(content, Does.Contain("Clipboard"));
            Assert.That(content, Does.Not.Contain("No source file at"));
        });
    }

    [Test]
    public async Task Source_file_paths_are_matched_case_insensitively()
    {
        var content = Text(await CallAsync("GetButilSourceFile", new { path = "demo/client/pages/clipboardpage.razor" }));

        Assert.That(content, Does.Not.StartWith("No source file at"),
            "Paths come back out of a listing an agent may re-case; the lookup is declared case-insensitive.");
    }

    [Test]
    public async Task Setup_guide_answers_for_every_hosting_model()
    {
        // The server names its own hosting models when it cannot resolve one, and that sentence is
        // the only list a client ever sees. Checked against the pinned list first: a model added to
        // the server and not here would otherwise go untested by the loop below, and one dropped
        // from the server would leave the loop asserting over an empty set.
        const string marker = "Use one of:";

        var refused = Text(await CallRawAsync("GetButilSetupGuide", new { hostingModel = "no-such-model" }));
        var listed = refused.IndexOf(marker, StringComparison.Ordinal);

        Assert.That(ButilMcp.HostingModels, Is.Not.Empty);
        Assert.That(listed, Is.GreaterThanOrEqualTo(0), $"An unknown hosting model was answered with: {refused}");

        var offered = refused[(listed + marker.Length)..]
            .TrimEnd('.', ' ')
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        Assert.That(offered, Is.EquivalentTo(ButilMcp.HostingModels),
            "The hosting models the server offers are not the ones this suite covers.");

        foreach (var model in ButilMcp.HostingModels)
        {
            var text = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = model }));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.StartWith("# Setting Butil up"), $"'{model}' did not return a setup guide.");
                Assert.That(text, Does.Contain("## Checklist"));
                Assert.That(text, Does.Contain("AddBitButilServices()"));
                Assert.That(text, Does.Contain("bit-butil.js"));

                // "as the real files of a working project" - the guide's own promise. Prose with no
                // code in it is the failure mode where the embedded sources did not make the build.
                Assert.That(text, Does.Contain("```"), $"The '{model}' guide contains no code at all.");
            });
        }
    }

    [Test]
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

            Assert.That(aliased, Is.EqualTo(expected), $"'{alias}' should be understood as the '{canonical}' hosting model.");
        }
    }

    [Test]
    public async Task Web_app_guide_leads_with_the_mistake_that_hosting_model_invites()
    {
        // Two DI containers is the thing that goes wrong in a Blazor Web App, and it goes wrong
        // silently. If the guide stops saying so, it has stopped being worth calling.
        var text = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = "web-app" }));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("BOTH"));
            Assert.That(text, Does.Contain("prerender").IgnoreCase);
            Assert.That(text, Does.Contain("OnAfterRenderAsync"));
        });
    }

    [Test]
    public async Task Docs_page_is_the_page_a_human_reads()
    {
        var text = Text(await CallAsync("GetButilDocsPage", new { slug = "clipboard" }));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.StartWith("Bit.Butil documentation page: /clipboard"));
            Assert.That(text, Does.Contain("# "), "The rendered page carries its own heading.");
            Assert.That(text, Does.Contain("Clipboard"));

            // Flattened to Markdown, not handed over as markup: the point of the conversion is that
            // a client's context is spent on prose rather than on div soup.
            Assert.That(text, Does.Not.Contain("<div"));
            Assert.That(text, Does.Not.Contain("class=\""));

            Assert.That(text, Does.Not.Contain("could not be rendered on the server"));
        });
    }

    [Test]
    public async Task Docs_page_slugs_are_forgiving_of_a_leading_slash()
    {
        var plain = Text(await CallAsync("GetButilDocsPage", new { slug = "clipboard" }));
        var slashed = Text(await CallAsync("GetButilDocsPage", new { slug = "/clipboard" }));

        Assert.That(slashed, Is.EqualTo(plain), "A slug copied out of a Url field arrives with its slash.");
    }

    [Test]
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

        Assert.That(failures, Is.Empty, $"Documentation pages that did not render:\n{string.Join("\n", failures)}");
    }

    [Test]
    public async Task No_answer_exceeds_the_documented_cap()
    {
        // Every document-shaped answer is truncated to the same length, so one page can never eat a
        // client's whole context window. The marker is part of the promise: a client has to be able
        // to tell a complete answer from a cut one.
        var slugs = new[] { "clipboard", "getting-started", "troubleshooting", "mcp-server" };

        foreach (var slug in slugs)
        {
            var text = Text(await CallAsync("GetButilDocsPage", new { slug }));

            Assert.That(text.Length, Is.LessThanOrEqualTo(ButilMcp.MaxDocumentLength + 200),
                $"/{slug} came back at {text.Length} characters, past the cap plus its truncation notice.");

            if (text.Length > ButilMcp.MaxDocumentLength)
            {
                Assert.That(text, Does.Contain(ButilMcp.TruncationMarker), $"/{slug} was cut without saying so.");
            }
        }
    }
}
