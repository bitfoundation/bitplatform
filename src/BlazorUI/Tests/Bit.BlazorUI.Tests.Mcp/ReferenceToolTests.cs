using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.BlazorUI.Tests.Mcp.Infrastructure;

namespace Bit.BlazorUI.Tests.Mcp;

/// <summary>
/// The three reference tools that are not about a component: the type lookup, the setup guide and
/// the theming reference.
/// <para>
/// Each answers from a different source - reflection over the loaded assemblies, hand-written
/// prose, and a page rendered by the real components - so each has its own way of going quietly
/// blank. The assertions are on the facts that would be wrong if it had.
/// </para>
/// </summary>
[TestClass]
public class ReferenceToolTests : McpTestBase
{
    [TestMethod]
    public async Task An_enum_answers_with_every_value_and_what_it_means()
    {
        var answer = await CallAsync("GetBitBlazorUIType", new { typeName = "BitColor" });

        var values = TableRows(answer, "| Name | Value | Description |");

        using var scope = Assert.Scope();

        StringAssert.StartsWith(answer, "# BitColor");
        Assert.IsGreaterThan(10, values.Length, "BitColor lost most of its values.");

        foreach (var row in values)
        {
            Assert.IsNotEmpty(row[0], "An enum value has no name.");
            Assert.IsNotEmpty(row[2], $"BitColor.{row[0]} has no description, so the XML documentation is not being read.");
        }
    }

    [TestMethod]
    public async Task A_type_resolves_with_or_without_its_prefix_and_its_generic_arguments()
    {
        using var scope = Assert.Scope();

        foreach (var typed in new[] { "BitVariant", "Variant", "BitDropdownItem<TValue>", "BitModalService" })
        {
            var answer = await CallAsync("GetBitBlazorUIType", new { typeName = typed });

            Assert.DoesNotContain("has no public type called", answer, $"'{typed}' did not resolve.");
        }
    }

    [TestMethod]
    public async Task A_dotted_name_reaches_a_nested_token_catalog()
    {
        var answer = await CallAsync("GetBitBlazorUIType", new { typeName = "BitCss.Var.Color.Primary" });

        using var scope = Assert.Scope();

        StringAssert.StartsWith(answer, "# BitCss.Var.Color.Primary");
        StringAssert.Contains(answer, "--bit-clr-pri", "The CSS custom property the constant holds is missing, and it is the value the whole type exists for.");
    }

    [TestMethod]
    public async Task A_token_catalog_names_the_branches_it_is_organised_into()
    {
        // BitCss holds no constants of its own - it is nothing but nested static classes - so an
        // answer that only listed them beside a constants table left the whole tree unreachable.
        var root = await CallAsync("GetBitBlazorUIType", new { typeName = "BitCss" });

        using var scope = Assert.Scope();

        StringAssert.Contains(root, "`BitCss.Var`", "BitCss does not name the branches it is organised into.");
        StringAssert.Contains(root, "`BitCss.Class`");

        // And every branch it names is a name the same tool resolves: the path is the full dotted
        // one and not the CLR simple name, which would send a caller to a different type entirely.
        var branch = await CallAsync("GetBitBlazorUIType", new { typeName = "BitCss.Var.Color" });

        StringAssert.Contains(branch, "`BitCss.Var.Color.Primary`", "A nested branch is named by its simple name, which does not resolve.");

        var leaf = await CallAsync("GetBitBlazorUIType", new { typeName = "BitCss.Var.Color.Primary" });

        Assert.DoesNotContain("has no public type called", leaf, "A path the answer above printed does not resolve.");
    }

    [TestMethod]
    public async Task A_short_dotted_name_answers_under_the_path_that_resolves()
    {
        // The Bit prefix is optional on the way in, but what comes back is what a caller pastes
        // into the next call, so it carries the name the type actually has.
        var answer = await CallAsync("GetBitBlazorUIType", new { typeName = "Css.Var.Color" });

        StringAssert.StartsWith(answer, "# BitCss.Var.Color");
    }

    [TestMethod]
    public async Task The_type_listing_leaves_out_what_a_component_documents_and_says_so()
    {
        var listing = await CallAsync("GetBitBlazorUIType");

        using var scope = Assert.Scope();

        StringAssert.Contains(listing, "## enum");
        StringAssert.Contains(listing, "`BitColor`");

        // A type named after a component is read in that component's context, and the listing says
        // where rather than repeating it - it names one as an example, so the assertion is about the
        // list rather than about the prose above it.
        Assert.DoesNotContain("- `BitButtonClassStyles`", listing);
        StringAssert.Contains(listing, "GetBitBlazorUIComponent");

        // Anything it leaves out is counted, not silently dropped.
        StringAssert.Contains(listing, "not listed here");

        // "Referenced by a component" is a test on the name, not on the characters: BitLink names
        // no Link parameter, and a substring test listed the Assets component under it.
        Assert.DoesNotContain("- `Link`", listing,
            "A component nothing takes as a parameter is listed because its name sits inside another one.");
    }

    [TestMethod]
    public async Task A_two_thousand_name_catalog_is_summarised_rather_than_dumped()
    {
        var answer = await CallAsync("GetBitBlazorUIType", new { typeName = "BitIconName" });

        using var scope = Assert.Scope();

        StringAssert.Contains(answer, "FindBitBlazorUIIcons", "BitIconName does not point at the tool that searches it.");
        Assert.IsLessThan(20_000, answer.Length, "BitIconName answered with something close to all two thousand names.");
    }

    [DataTestMethod]
    [DataRow("web-app")]
    [DataRow("wasm")]
    [DataRow("server")]
    [DataRow("hybrid")]
    public async Task Every_hosting_model_has_a_guide_with_the_four_things_that_fail_silently(string hostingModel)
    {
        var answer = await CallAsync("GetBitBlazorUISetupGuide", new { hostingModel });

        using var scope = Assert.Scope();

        StringAssert.Contains(answer, "dotnet add package Bit.BlazorUI");
        StringAssert.Contains(answer, "@using Bit.BlazorUI");
        StringAssert.Contains(answer, "AddBitBlazorUIServices");
        StringAssert.Contains(answer, "_content/Bit.BlazorUI/styles/bit.blazorui.css");
        StringAssert.Contains(answer, "_content/Bit.BlazorUI/scripts/bit.blazorui.js");

        // The optional packages, with the whole of what each one takes rather than a mention.
        StringAssert.Contains(answer, "Bit.BlazorUI.Extras");
        StringAssert.Contains(answer, "Bit.BlazorUI.Icons");
        StringAssert.Contains(answer, "Bit.BlazorUI.Legacy");

        // And the check that proves it worked.
        StringAssert.Contains(answer, "## Verify");
    }

    [TestMethod]
    public async Task The_setup_guide_says_which_hosting_models_may_use_singletons()
    {
        using var scope = Assert.Scope();

        var server = await CallAsync("GetBitBlazorUISetupGuide", new { hostingModel = "server" });
        StringAssert.Contains(server, "trySingleton", "The Blazor Server guide does not warn about singleton services leaking across circuits.");

        var wasm = await CallAsync("GetBitBlazorUISetupGuide", new { hostingModel = "wasm" });
        StringAssert.Contains(wasm, "trySingleton: true");

        var webApp = await CallAsync("GetBitBlazorUISetupGuide", new { hostingModel = "web-app" });
        StringAssert.Contains(webApp, "both", "The Blazor Web App guide does not say the services go in both containers.");
    }

    [TestMethod]
    public async Task The_theming_index_lists_the_chapters_it_can_return()
    {
        var index = await CallAsync("GetBitBlazorUIThemingGuide");

        using var scope = Assert.Scope();

        // Rendered from the site's own page, so a failure to render answers as prose saying where
        // the page is - which is a pass for the tool and a fail for this assertion.
        Assert.DoesNotContain("could not be rendered", index, "The theming page did not render on the server.");

        StringAssert.StartsWith(index, "# Theming");
        StringAssert.Contains(index, "--bit-*");

        foreach (var chapter in new[] { "Design tokens", "Presets", "The C# API", "Server-side rendering" })
        {
            StringAssert.Contains(index, $"**{chapter}**", $"The theming index has no '{chapter}' chapter.");
        }

        // The closing block every page carries is not part of the reference.
        Assert.DoesNotContain("**Feedback**", index);
    }

    [TestMethod]
    public async Task A_theming_chapter_answers_with_that_chapter_and_a_sub_heading_resolves_too()
    {
        using var scope = Assert.Scope();

        var chapter = await CallAsync("GetBitBlazorUIThemingGuide", new { section = "Design tokens" });

        StringAssert.StartsWith(chapter, "## Design tokens");
        StringAssert.Contains(chapter, "--bit-clr-");
        Assert.DoesNotContain("\n## Presets", chapter, "The chapter brought its neighbour with it.");

        // Matching ignores case and punctuation, and reaches the sub-headings the index advertises.
        var sub = await CallAsync("GetBitBlazorUIThemingGuide", new { section = "overriding a token" });

        Assert.DoesNotContain("has no chapter called", sub, "A sub-heading the index lists does not resolve.");
    }

    [TestMethod]
    public async Task Icons_are_searched_by_what_they_show()
    {
        using var scope = Assert.Scope();

        var cart = await CallAsync("FindBitBlazorUIIcons", new { query = "shopping cart" });
        StringAssert.Contains(cart, "ShoppingCart");

        // Names are matched word by word, so a two-word query finds the Pascal-case name.
        var friend = await CallAsync("FindBitBlazorUIIcons", new { query = "add friend" });
        StringAssert.Contains(friend, "AddFriend");

        // The answer says what the glyph needs to render at all, because a missing package is an
        // empty box rather than an error.
        StringAssert.Contains(cart, "Bit.BlazorUI.Icons");
    }
}
