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
    public async Task Overview_is_the_map_of_everything_else()
    {
        var text = Text(await CallAsync("GetButilOverview"));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Bit.Butil"));
            Assert.That(text, Does.Contain("Which tool to call"));
            Assert.That(text, Does.Contain("Rules of thumb when writing Butil code"));

            // The overview is assembled from named README sections. When one is renamed the server
            // says so in place of the text - which is the right behaviour, and a bug to fix here.
            Assert.That(text, Does.Not.Contain("was not found in this build"),
                "A section the overview quotes is missing from the README it was built against.");

            // It states which build it answers for, and the count comes from the live catalog.
            Assert.That(text, Does.Match(@"These tools answer from Bit\.Butil [^\s]+, loaded in this server"));
            Assert.That(text, Does.Not.Contain("0 injectable services"));
        });

        // Every tool it tells an agent to reach for has to be a tool the server advertises.
        var advertised = (await Mcp.ListToolsAsync(cancellationToken: Ct)).Select(tool => tool.Name).ToArray();

        Assert.Multiple(() =>
        {
            foreach (var tool in advertised.Where(name => name != "GetButilOverview"))
            {
                Assert.That(text, Does.Contain(tool), $"The overview never mentions the {tool} tool.");
            }
        });
    }

    [Test]
    public async Task Api_list_is_the_shipped_public_surface()
    {
        var types = await CallStructuredAsync<ApiType[]>("GetButilApiList");

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
    public async Task Inspect_reports_what_the_page_has_to_arrange_first()
    {
        var inspection = await CallStructuredAsync<ApiInspection>("InspectButilApi", new { name = "Clipboard" });

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsKnown, Is.True, inspection.Message);
            Assert.That(inspection.Api, Is.EqualTo("Clipboard"));
            Assert.That(inspection.Services, Does.Contain("Clipboard"));
            Assert.That(inspection.Inject, Does.Contain("@inject Bit.Butil.Clipboard clipboard"));

            // The whole reason this tool exists: the clipboard needs a secure context, and a page
            // served over plain http fails silently rather than loudly.
            Assert.That(string.Join(" ", inspection.Requires ?? []), Does.Contain("Secure context").IgnoreCase);

            // Prerendering is the first note on every API, because it is the first mistake.
            Assert.That(string.Join(" ", inspection.Notes ?? []), Does.Contain("Prerendering"));

            Assert.That(inspection.NextCalls, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task Inspect_answers_a_member_as_a_question_about_its_api()
    {
        // "Geolocation.SubscribeWatch" is a question about Geolocation. Answering "no such type"
        // would be technically true and useless.
        var inspection = await CallStructuredAsync<ApiInspection>("InspectButilApi", new { name = "Geolocation.SubscribeWatch" });

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
    public async Task Inspect_accepts_a_docs_slug()
    {
        // "storage" is the slug of the "Local & Session Storage" page - a name no type goes by, and
        // exactly what a search hit and a docs listing both hand back.
        var inspection = await CallStructuredAsync<ApiInspection>("InspectButilApi", new { name = "storage" });

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsKnown, Is.True, $"A slug straight out of GetButilDocsList did not resolve: {inspection.Message}");
            Assert.That(inspection.Api, Is.EqualTo("Local & Session Storage"));
            Assert.That(inspection.Services, Is.EquivalentTo(new[] { "LocalStorage", "SessionStorage" }));
        });
    }

    [Test]
    public async Task Every_docs_slug_is_something_inspect_can_resolve()
    {
        var pages = await CallStructuredAsync<DocsPage[]>("GetButilDocsList");

        // A guide page documents no API, so it is allowed to be unknown to the inspector; every
        // page that documents services is not.
        var apiPages = pages.Where(page => page.Services.Length > 0).ToArray();

        Assert.That(apiPages, Is.Not.Empty);

        foreach (var page in apiPages)
        {
            var inspection = await CallStructuredAsync<ApiInspection>("InspectButilApi", new { name = page.Slug });

            Assert.That(inspection.IsKnown, Is.True, $"InspectButilApi('{page.Slug}') answered: {inspection.Message}");
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
    public async Task Browser_support_is_the_whole_matrix()
    {
        var capabilities = await CallStructuredAsync<Capability[]>("GetButilBrowserSupport");

        Assert.Multiple(() =>
        {
            Assert.That(capabilities.Length, Is.GreaterThan(40));

            foreach (var capability in capabilities)
            {
                Assert.That(capability.Api, Is.Not.Empty);
                Assert.That(capability.Summary, Is.Not.Empty);
                Assert.That(capability.Services, Is.Not.Empty, $"{capability.Api} names no Bit.Butil type, so a reader cannot act on it.");
                Assert.That(capability.BrowserSupport, Is.Not.Empty);
                Assert.That(capability.DocsUrl, Does.StartWith("/"));
            }

            // The matrix is only useful if it distinguishes: a table where every row says the same
            // thing is a table nobody can choose between two APIs with.
            Assert.That(capabilities.Select(capability => capability.BrowserSupport).Distinct().Count(), Is.GreaterThan(1));
            Assert.That(capabilities.Any(capability => capability.Requires.Length > 0), Is.True);
        });
    }

    [Test]
    public async Task Docs_list_is_a_usable_index()
    {
        var pages = await CallStructuredAsync<DocsPage[]>("GetButilDocsList");

        Assert.Multiple(() =>
        {
            Assert.That(pages.Length, Is.GreaterThan(40));

            foreach (var page in pages)
            {
                Assert.That(page.Slug, Is.Not.Empty);
                Assert.That(page.Url, Is.EqualTo($"/{page.Slug}"));
                Assert.That(page.Title, Is.Not.Empty);
                Assert.That(page.Summary, Is.Not.Empty, $"The '{page.Title}' page has no summary, so nothing in the index says what it covers.");
                Assert.That(page.Group, Is.Not.Empty);
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
        var sections = await CallStructuredAsync<GuideSection[]>("GetButilGuideSections");

        Assert.Multiple(() =>
        {
            Assert.That(sections, Is.Not.Empty, "No guide sections means the README was not embedded into the published app.");
            Assert.That(sections.Select(section => section.Heading), Does.Contain("Getting started"));

            foreach (var section in sections)
            {
                Assert.That(section.Level, Is.InRange(2, 3));
                Assert.That(section.Lines, Is.GreaterThan(0), $"The '{section.Heading}' section is empty.");
                Assert.That(section.Parent, section.Level == 2 ? Is.Null : Is.Not.Null,
                    $"'{section.Heading}' is a level-{section.Level} heading with Parent '{section.Parent}'.");
            }
        });
    }

    [Test]
    public async Task Every_guide_section_can_be_read_back_by_its_own_heading()
    {
        var sections = await CallStructuredAsync<GuideSection[]>("GetButilGuideSections");

        // The listing exists to be used: a heading it hands out has to be a heading the reader
        // accepts, or the pair of tools is broken in the one way nothing else would notice.
        foreach (var section in sections)
        {
            var text = Text(await CallAsync("GetButilGuideSection", new { heading = section.Heading }));

            Assert.That(text, Does.StartWith(new string('#', section.Level) + " "),
                $"Reading back the '{section.Heading}' section did not return that section.");
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
        var files = await CallStructuredAsync<SourceFile[]>("GetButilSourceFiles");

        Assert.Multiple(() =>
        {
            Assert.That(files, Is.Not.Empty, "No source files means the demo's sources were not embedded into the published app.");
            Assert.That(files.Select(file => file.Kind).Distinct(), Is.EquivalentTo(new[] { "Demo", "Sample" }));
            Assert.That(files.All(file => file.Lines > 0), Is.True);
            Assert.That(files.Select(file => file.Path), Does.Contain("Demo/Client/Pages/ClipboardPage.razor"));
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
        var pages = await CallStructuredAsync<DocsPage[]>("GetButilDocsList");

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
