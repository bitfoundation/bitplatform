using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiGenerativeUiView at /e2e/aigenerativeuiview in a real browser: the component the host registered for
/// generative UI (E2EAiCard as e2e_card) instantiated from the props a model wrote, both from a finished part and from
/// the generative script of the scripted transport; a component nobody registered (the generative-unknown script) and the
/// type name of a registered one refused with a placeholder; props streaming in and binding as far as they have arrived;
/// the binding diagnostics and their public variables; RTL.
/// </summary>
[TestClass]
public class BitAiGenerativeUiViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aigenerativeuiview";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Card(ILocator root) => root.Locator(":scope > .bit-aiguv-cnt > .e2e-ai-card");

    private ILocator Placeholder(ILocator root) => root.Locator(":scope > .bit-aiguv-unr");

    [TestMethod]
    public async Task Registered_TheComponent_RendersWithThePropsTheModelWrote()
    {
        await OpenAsync(Route);

        var root = Root("guv-card");
        await Expect(root).ToHaveClassAsync(HasClass("bit-aiguv"));

        var card = Card(root);
        await Expect(card.Locator(".e2e-ai-card-title")).ToHaveTextAsync("Visitors");
        await Expect(card.Locator(".e2e-ai-card-value")).ToHaveTextAsync("1200");
        await Expect(Placeholder(root)).ToHaveCountAsync(0);
        await Expect(root.Locator(".bit-aiguv-dgn")).ToHaveCountAsync(0);

        // The container keeps whatever was registered from widening what it sits in.
        await Expect(root.Locator(":scope > .bit-aiguv-cnt")).ToHaveCSSAsync("max-width", "100%");
    }

    [TestMethod]
    public async Task Streaming_ModelOutput_InstantiatesTheRegisteredComponentsInOrder()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-guv-generative").ClickAsync();
        await Expect(Page.Locator("#guv-generative-answers")).ToHaveTextAsync("1");

        var views = Page.Locator("#guv-generative > .bit-aiguv");
        await Expect(views).ToHaveCountAsync(2);
        await Expect(Page.Locator("#guv-generative .e2e-ai-card-title")).ToHaveTextAsync(["Visitors", "Signups"]);
        await Expect(Page.Locator("#guv-generative .e2e-ai-card-value")).ToHaveTextAsync(["1200", "85"]);
        await Expect(Page.Locator("#guv-generative .bit-aiguv-unr")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#guv-generative-text")).ToContainTextAsync("Here are the numbers.");
        await Expect(Page.Locator("#guv-generative-text")).ToContainTextAsync("Signups are up.");
    }

    [TestMethod]
    public async Task Unknown_AComponentNobodyRegistered_IsRefusedWithAPlaceholder()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-guv-unknown").ClickAsync();
        await Expect(Page.Locator("#guv-unknown-answers")).ToHaveTextAsync("1");

        var view = Page.Locator("#guv-unknown > .bit-aiguv");
        await Expect(view).ToHaveCountAsync(1);

        var tag = Placeholder(view);
        await Expect(tag).ToBeVisibleAsync();
        await Expect(tag).ToHaveClassAsync(HasClass("bit-tag"));
        await Expect(tag.Locator(".bit-tag-tex")).ToHaveTextAsync("not_registered");
        await Expect(tag).ToHaveAttributeAsync("title", "No component named 'not_registered' is registered.");
        // A tooltip is not read out, so the reason is the name of the tag too: text no one sees, in place of the name shown.
        await Expect(tag.Locator(".bit-tag-vhd")).ToHaveTextAsync("No component named 'not_registered' is registered.");
        await Expect(tag.Locator(".bit-tag-lbl")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(tag).ToHaveClassAsync(HasClass("bit-tag-wrn"));

        // Nothing of the payload is rendered, and the page goes on working.
        await Expect(view.Locator(".bit-aiguv-cnt")).ToHaveCountAsync(0);
        await Expect(view.Locator(".e2e-ai-card")).ToHaveCountAsync(0);
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    [DataRow("guv-type-name", "E2EAiCard")]
    [DataRow("guv-full-type-name", "Bit.BlazorUI.Tests.E2E.TestHost.AI.E2EAiCard")]
    public async Task Unknown_TheTypeNameOfARegisteredComponent_DoesNotReachIt(string id, string name)
    {
        await OpenAsync(Route);

        await Expect(Placeholder(Root(id)).Locator(".bit-tag-tex")).ToHaveTextAsync(name);
        await Expect(Root(id).Locator(".e2e-ai-card")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Props_TheComponent_FillsInAsThePropsArrive()
    {
        await OpenAsync(Route);

        var root = Root("guv-props");
        var append = Page.Locator("#btn-guv-props");

        // A registered name renders as soon as the part opens, before any of its props have arrived.
        await Expect(Card(root)).ToBeVisibleAsync();
        await Expect(Card(root).Locator(".e2e-ai-card-value")).ToHaveTextAsync("0");

        await append.ClickAsync();
        await Expect(Page.Locator("#guv-props-step")).ToHaveTextAsync("1");
        await Expect(Card(root).Locator(".e2e-ai-card-title")).ToHaveTextAsync(new Regex("^(Sig)?$"));

        await append.ClickAsync();
        await Expect(Card(root).Locator(".e2e-ai-card-title")).ToHaveTextAsync("Signups");
        await Expect(Card(root).Locator(".e2e-ai-card-value")).ToHaveTextAsync("8");

        await append.ClickAsync();
        await Expect(Card(root).Locator(".e2e-ai-card-value")).ToHaveTextAsync("85");

        await append.ClickAsync();
        await Expect(Page.Locator("#guv-props-step")).ToHaveTextAsync("4");
        await Expect(Card(root).Locator(".e2e-ai-card-value")).ToHaveTextAsync("85");
        await Expect(Root("guv-props").Locator(".e2e-ai-card")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Props_AnUnknownNameStillStreaming_SaysNothingUntilThePartCompletes()
    {
        await OpenAsync(Route);

        var root = Root("guv-streaming-unknown");
        await Expect(root).ToBeAttachedAsync();
        await Expect(Placeholder(root)).ToHaveCountAsync(0);
        await Expect(root).ToHaveTextAsync("");

        await Page.Locator("#btn-guv-unknown-complete").ClickAsync();
        await Expect(Placeholder(root).Locator(".bit-tag-tex")).ToHaveTextAsync("not_registered");
    }

    [TestMethod]
    public async Task Diagnostics_ShowBindingErrors_ListsTheErrorsAndTheIgnoredProperties()
    {
        await OpenAsync(Route);

        var root = Root("guv-diagnostics");

        // The component still renders with what did bind.
        await Expect(Card(root)).ToBeVisibleAsync();
        await Expect(Card(root).Locator(".e2e-ai-card-value")).ToHaveTextAsync("0");

        var diagnostics = root.Locator(":scope > .bit-aiguv-dgn");
        await Expect(diagnostics).ToBeVisibleAsync();

        var errors = diagnostics.Locator(".bit-aiguv-err");
        await Expect(errors).ToHaveCountAsync(2);
        // Worded for the reader; what the serializer said, written for a developer, is the title of the tag.
        await Expect(errors.Nth(0)).ToHaveTextAsync(new Regex("^'value' could not be read as Int32"));
        await Expect(errors.Nth(0)).ToHaveAttributeAsync("title", new Regex(".+"));
        await Expect(errors.Nth(1)).ToHaveTextAsync(new Regex("^'Title' is required by 'e2e_card'\\.?$"));
        await Expect(errors.First).ToHaveClassAsync(HasClass("bit-tag-err"));

        var ignored = diagnostics.Locator(".bit-aiguv-ign");
        await Expect(ignored).ToHaveTextAsync("Ignored: color");
        await Expect(ignored).ToHaveAttributeAsync("title", "The component does not declare this property, so it was not passed.");
        await Expect(ignored).ToHaveClassAsync(HasClass("bit-tag-inf"));

        // The diagnostics are a wrapping row under the component.
        await Expect(diagnostics).ToHaveCSSAsync("display", "flex");
        await Expect(diagnostics).ToHaveCSSAsync("flex-wrap", "wrap");
        var card = await root.Locator(":scope > .bit-aiguv-cnt").RectAsync();
        var row = await diagnostics.RectAsync();
        Assert.IsTrue(row.Top >= card.Bottom, "The diagnostics are under the component.");

        // Off by default: the same part with the parameter unset says nothing.
        await Expect(Card(Root("guv-no-diagnostics"))).ToBeVisibleAsync();
        await Expect(Root("guv-no-diagnostics").Locator(".bit-aiguv-dgn")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Variables_TheDiagnostics_AreSpacedByThePublicVariables()
    {
        await OpenAsync(Route);

        var diagnostics = Root("guv-diagnostics-vars").Locator(":scope > .bit-aiguv-dgn");
        await Expect(diagnostics).ToHaveCSSAsync("column-gap", "17px");
        await Expect(diagnostics).ToHaveCSSAsync("margin-top", "23px");

        // And the defaults, read from the spacing of the theme.
        var defaults = Root("guv-diagnostics").Locator(":scope > .bit-aiguv-dgn");
        await Expect(defaults).Not.ToHaveCSSAsync("column-gap", "17px");
        await Expect(defaults).Not.ToHaveCSSAsync("margin-top", "0px");
    }

    [TestMethod]
    public async Task Rtl_TheViewAndItsPlaceholder_ReadRightToLeft()
    {
        await OpenAsync(Route);

        await Expect(Root("guv-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("guv-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Card(Root("guv-rtl"))).ToHaveCSSAsync("direction", "rtl");

        var tag = Placeholder(Root("guv-rtl-unknown"));
        await Expect(tag).ToHaveCSSAsync("direction", "rtl");
        var icon = await tag.Locator("i").First.RectAsync();
        var text = await tag.Locator(".bit-tag-tex").RectAsync();
        Assert.IsTrue(icon.Left >= text.Right - 1, "The warning glyph is to the right of the name.");
    }
}
