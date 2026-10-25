using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// Drives BitElement (/e2e/element) in a real browser: any tag (the default div, headings, SVG with case-sensitive
/// names in the SVG namespace, custom elements, the invalid-name fallback, void elements left empty), splatted
/// attributes and events with the shared parameters winning over them, the click and named-event modifiers
/// (StopPropagation, PreventDefault and their Events lists), a tag changed at runtime, NoWrapper, FocusAsync with and
/// without scrolling, the disabled contract on every kind of tag (native disabled, aria-disabled, the tab stop, a
/// disabled anchor's href and role, a disabled area's prevented clicks, the page's activation handlers dropped and
/// given back), Visibility, the BitParams cascade, Style/Class merged with the splatted ones, the
/// --bit-Element-disabled-opacity variable, RTL, the prerender and unmount.
/// </summary>
[TestClass]
public class BitElementE2ETests : E2ETestBase
{
    private const string Path = "/e2e/element";

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheElementsBeforeTheCircuitConnects()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Path}")).TextAsync();

        StringAssert.Contains(html, "<h4 id=\"elm-h4\" class=\"bit-elm\">A heading (h4)</h4>");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Path);
        await Expect(Page.Locator("#elm-h4")).ToHaveTextAsync("A heading (h4)");
    }

    [TestMethod]
    public async Task Element_RendersTheTagItNames_AndADivByDefault()
    {
        await OpenAsync(Path);

        Assert.AreEqual("DIV", await TagAsync("#elm-default"));
        await Expect(Page.Locator("#elm-default")).ToHaveClassAsync(HasClass("bit-elm"));
        Assert.AreEqual("H4", await TagAsync("#elm-h4"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 4, Name = "A heading (h4)" })).ToHaveIdAsync("elm-h4");
        Assert.AreEqual("P", await TagAsync("#elm-p"));
        Assert.AreEqual("MARK", await TagAsync("#elm-p > #elm-mark"));
        Assert.AreEqual("BLOCKQUOTE", await TagAsync("#elm-blockquote"));
        Assert.AreEqual("SECTION", await TagAsync("#elm-padded"));
    }

    [TestMethod]
    public async Task Element_AnInvalidTagName_FallsBackToADiv()
    {
        await OpenAsync(Path);

        Assert.AreEqual("DIV", await TagAsync("#elm-invalid"));
        Assert.AreEqual("DIV", await TagAsync("#elm-invalid-symbol"));
        await Expect(Page.Locator("script#elm-invalid-symbol")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Attributes_AreSplattedOntoTheTag_AndItsEventsRun()
    {
        await OpenAsync(Path);

        var anchor = Page.Locator("#elm-anchor");
        await Expect(anchor).ToHaveAttributeAsync("href", "/e2e/element#attributes");
        await Expect(anchor).ToHaveAttributeAsync("target", "_self");
        await Expect(anchor).ToHaveAttributeAsync("rel", "noopener");
        await Expect(anchor).ToHaveAttributeAsync("data-e2e", "anchor");

        var button = Page.Locator("#elm-button");
        await Expect(button).ToHaveAttributeAsync("data-demo", "counter");
        await button.ClickAsync();
        await button.ClickAsync();
        await Expect(button).ToHaveTextAsync("Clicked 2 times");

        var input = Page.Locator("#elm-input");
        await input.PressSequentiallyAsync("hello");
        await Expect(Page.Locator("#typed-output")).ToHaveTextAsync("You typed: hello");
        await Expect(input).ToHaveAccessibleNameAsync("Your text");
        await Expect(input).ToHaveAccessibleDescriptionAsync("You typed: hello");
        await Expect(Page.Locator("#typed-output")).ToHaveAttributeAsync("role", "status");
    }

    [TestMethod]
    public async Task Attributes_TheSharedParameters_WinOverTheSplattedOnesOfTheSameName()
    {
        await OpenAsync(Path);

        var own = Page.Locator("#elm-own-id");
        await Expect(own).ToHaveAttributeAsync("aria-label", "Own label");
        await Expect(own).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Page.Locator("#splatted-id")).ToHaveCountAsync(0);

        // A splatted id and tab index are used when nothing else names them.
        await Expect(Page.Locator("#elm-splat-id")).ToHaveAttributeAsync("tabindex", "2");
    }

    [TestMethod]
    public async Task Svg_TheTagsCaseIsKept_AndTheShapesAreDrawnInTheSvgNamespace()
    {
        await OpenAsync(Path);

        var svg = Page.Locator("#elm-svg");
        await Expect(svg).ToHaveAttributeAsync("viewBox", "0 0 160 48");
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "A gradient bar" })).ToHaveIdAsync("elm-svg");

        var gradient = Page.Locator("#elm-gradient");
        Assert.AreEqual("linearGradient", await gradient.EvaluateAsync<string>("el => el.tagName"));
        Assert.AreEqual("http://www.w3.org/2000/svg", await gradient.EvaluateAsync<string>("el => el.namespaceURI"));
        Assert.AreEqual(2, await gradient.EvaluateAsync<int>("el => el.querySelectorAll('stop').length"));

        var rect = await Page.Locator("#elm-rect").BoundingBoxAsync();
        Assert.AreEqual(160, Math.Round(rect!.Width));
        Assert.AreEqual(48, Math.Round(rect.Height));
        StringAssert.Contains(await ComputedStyleAsync("#elm-rect", "fill"), "elm-gradient");

        Assert.AreEqual("DEMO-BADGE", await TagAsync("#elm-custom"));
        await Expect(Page.Locator("#elm-custom")).ToHaveTextAsync("A custom element");
    }

    [TestMethod]
    public async Task VoidElements_RenderWithoutContent()
    {
        await OpenAsync(Path);

        Assert.AreEqual("INPUT", await TagAsync("#elm-void-input"));
        await Expect(Page.Locator("#elm-void-input")).ToHaveAttributeAsync("placeholder", "An input");
        Assert.AreEqual("HR", await TagAsync("#elm-void-hr"));

        var img = Page.Locator("#elm-void-img");
        await Expect(img).ToHaveAttributeAsync("alt", "bit logo");
        await Page.WaitForFunctionAsync("() => document.getElementById('elm-void-img').naturalWidth > 0");

        Assert.AreEqual("BR", await TagAsync("#elm-void-br"));
        Assert.AreEqual(0, await Page.Locator("#elm-void-br").EvaluateAsync<int>("el => el.childNodes.length"));
        await Expect(Page.GetByText("Not rendered: a br holds no content.")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task StopPropagation_KeepsTheClickFromTheCard()
    {
        await OpenAsync(Path);

        await Page.Locator("#elm-stop").ClickAsync();
        await Expect(Page.Locator("#inner-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#card-clicks")).ToHaveTextAsync("0");

        await Page.Locator("#elm-bubble").ClickAsync();
        await Expect(Page.Locator("#inner-clicks")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#card-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task PreventDefault_KeepsAnAnchorFromNavigating()
    {
        await OpenAsync(Path);

        await Page.Locator("#elm-prevent").ClickAsync();

        await Expect(Page.Locator("#prevented-clicks")).ToHaveTextAsync("1");
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/element$"));
    }

    [TestMethod]
    public async Task EventsLists_StopTheDoubleClickAndPreventTheContextMenu_WithOrWithoutAHandler()
    {
        await OpenAsync(Path);

        await InstallDefaultPreventedProbeAsync("contextmenu");

        await Page.Locator("#elm-events").DblClickAsync();
        await Expect(Page.Locator("#inner-double")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#doubled")).ToHaveTextAsync("0");

        await Page.Locator("#elm-events").ClickAsync(new() { Button = MouseButton.Right });
        await Expect(Page.Locator("#inner-context")).ToHaveTextAsync("1");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__e2eDefaultPrevented"), "The context menu's default is prevented.");

        await Page.EvaluateAsync("() => window.__e2eDefaultPrevented = null");
        await Page.Locator("#elm-no-handler").ClickAsync(new() { Button = MouseButton.Right });
        await Page.WaitForFunctionAsync("() => window.__e2eDefaultPrevented === true");

        // A plain element around it is not prevented.
        await Page.EvaluateAsync("() => window.__e2eDefaultPrevented = null");
        await Page.Locator("#card-clicks").ClickAsync(new() { Button = MouseButton.Right });
        await Page.WaitForFunctionAsync("() => window.__e2eDefaultPrevented === false");
    }

    [TestMethod]
    public async Task Dynamic_ChangingTheTagAtRuntime_RebuildsTheElementWithTheAttributesItUnderstands()
    {
        await OpenAsync(Path);

        var element = Page.Locator("#elm-dynamic");
        Assert.AreEqual("DIV", await TagAsync("#elm-dynamic"));

        await Page.Locator("#btn-element-a").ClickAsync();
        await Expect(Page.Locator("a#elm-dynamic")).ToHaveAttributeAsync("href", "/e2e/element#dynamic");
        await Expect(element).ToHaveTextAsync("a");

        await Page.Locator("#btn-element-input").ClickAsync();
        await Expect(Page.Locator("input#elm-dynamic")).ToHaveAttributeAsync("placeholder", "input");
        Assert.AreEqual(0, await element.EvaluateAsync<int>("el => el.childNodes.length"));

        await Page.Locator("#btn-element-button").ClickAsync();
        await Expect(Page.Locator("button#elm-dynamic")).ToHaveTextAsync("button");

        await Page.Locator("#btn-element-div").ClickAsync();
        await Expect(Page.Locator("div#elm-dynamic")).ToHaveTextAsync("div");
    }

    [TestMethod]
    public async Task NoWrapper_RendersTheContentAlone_AndFocusAsyncIsANoOpThen()
    {
        await OpenAsync(Path);

        var host = Page.Locator("#wrapper-host");
        await Expect(host.Locator("mark#elm-wrapper.wrapper-class")).ToHaveTextAsync("The same content, highlighted or bare.");

        await Page.Locator("#btn-toggle-wrapper").ClickAsync();
        await Expect(host.Locator("mark")).ToHaveCountAsync(0);
        await Expect(host).ToHaveTextAsync("The same content, highlighted or bare.");
        await Expect(Page.Locator(".wrapper-class")).ToHaveCountAsync(0);

        await Page.Locator("#btn-focus-unwrapped").ClickAsync();
        await Expect(Page.Locator("#unwrapped-focus")).ToHaveTextAsync("done");

        await Page.Locator("#btn-toggle-wrapper").ClickAsync();
        await Expect(host.Locator("mark#elm-wrapper")).ToBeVisibleAsync();
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task FocusAsync_FocusesTheElement_WithOrWithoutScrollingItIntoView()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-focus-input").ClickAsync();
        await Expect(Page.Locator("#elm-focus-input")).ToBeFocusedAsync();

        await Page.Locator("#btn-focus-box-prevent").ClickAsync();
        await Expect(Page.Locator("#elm-focus-box")).ToBeFocusedAsync();
        Assert.AreEqual(0, await Page.Locator("#scroller").EvaluateAsync<double>("el => el.scrollTop"), "preventScroll left the scroller where it was.");

        await Page.Locator("#btn-focus-box").ClickAsync();
        await Expect(Page.Locator("#elm-focus-box")).ToBeFocusedAsync();
        await Page.WaitForFunctionAsync("() => document.getElementById('scroller').scrollTop > 0");
    }

    [TestMethod]
    public async Task Disabled_AFormElement_IsNativelyDisabledAndAnnouncedSo()
    {
        await OpenAsync(Path);

        var button = Page.Locator("#elm-dis-button");
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToHaveCSSAsync("opacity", await ResolveAsync("opacity", "var(--bit-opa-dis)"));
        await button.ClickAsync(new() { Force = true });
        await button.DispatchEventAsync("click");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");

        await Expect(Page.Locator("#elm-dis-input")).ToBeDisabledAsync();
        await Expect(Page.Locator("#elm-dis-input")).ToHaveAttributeAsync("aria-disabled", "true");

        // A disabled fieldset disables the form controls inside it.
        await Expect(Page.Locator("#elm-dis-fieldset")).ToHaveAttributeAsync("disabled", "");
        await Expect(Page.Locator("#fieldset-input")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task Disabled_AnAnchor_LosesItsHrefButIsStillALink_AndAPlaceholderStaysGeneric()
    {
        await OpenAsync(Path);

        var anchor = Page.Locator("#elm-dis-anchor");
        await Expect(anchor).Not.ToHaveAttributeAsync("href", new Regex(".*"));
        await Expect(anchor).ToHaveAttributeAsync("role", "link");
        await Expect(anchor).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(anchor).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(anchor).ToHaveCSSAsync("pointer-events", "none");

        await anchor.DispatchEventAsync("click");
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/element$"));

        await Expect(Page.Locator("#elm-dis-anchor-role")).ToHaveAttributeAsync("role", "button");
        await Expect(Page.Locator("#elm-dis-placeholder")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
    }

    [TestMethod]
    public async Task Disabled_AnArea_KeepsItsHref_AndItsClicksDoNotFollowIt()
    {
        await OpenAsync(Path);

        var area = Page.Locator("#elm-dis-area");
        await Expect(area).ToHaveAttributeAsync("href", "/e2e/element/elsewhere");
        await Expect(area).ToHaveAttributeAsync("aria-disabled", "true");

        await InstallDefaultPreventedProbeAsync("click");
        await area.DispatchEventAsync("click");

        await Page.WaitForFunctionAsync("() => window.__e2eDefaultPrevented === true");
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/element$"));
    }

    [TestMethod]
    public async Task Disabled_AGenericTag_LeavesTheTabOrder_AndDropsThePagesActivationHandlers()
    {
        await OpenAsync(Path);

        var div = Page.Locator("#elm-dis-div");
        await Expect(div).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(div).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(div).Not.ToHaveAttributeAsync("disabled", new Regex(".*"));
        await Expect(div).ToHaveCSSAsync("pointer-events", "none");

        // A screen reader's click, a key and a press: none runs the page's handler. A hover still does.
        await div.DispatchEventAsync("click");
        await div.DispatchEventAsync("keydown", new Dictionary<string, object> { ["key"] = "Enter" });
        await div.DispatchEventAsync("pointerdown");
        await div.DispatchEventAsync("mouseover");
        await Expect(Page.Locator("#disabled-hovers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");

        // An enabled control inside keeps its own handler and tab stop, and its click does not run the disabled root's.
        await Page.Locator("#inner-enabled").DispatchEventAsync("click");
        await Expect(Page.Locator("#inner-enabled-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");

        await Page.Locator("#before-disabled-div").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#inner-enabled")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Disabled_EnabledAgain_GetsItsTabStopAndHandlersBack()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-toggle-div-disabled").ClickAsync();

        var div = Page.Locator("#elm-dis-div");
        await Expect(div).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(div).ToHaveAttributeAsync("tabindex", "0");
        await Expect(div).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));

        await div.ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Expect(Page.Locator("#disabled-clicks")).Not.ToHaveTextAsync("0");

        await Page.Locator("#before-disabled-div").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(div).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheSpace_CollapsedRemovesIt_AndDropsUnwrappedContent()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#elm-hidden")).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await Page.Locator("#elm-hidden").BoundingBoxAsync())!.Height > 0);
        await Expect(Page.Locator("#elm-collapsed")).ToHaveCSSAsync("display", "none");

        await Expect(Page.Locator("#unwrapped-collapsed-host")).ToHaveTextAsync("[Unwrapped content]");
        await Page.Locator("#btn-collapse-unwrapped").ClickAsync();
        await Expect(Page.Locator("#unwrapped-collapsed-host")).ToHaveTextAsync("[]");
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_AndAnElementsOwnValueWins()
    {
        await OpenAsync(Path);

        Assert.AreEqual("BUTTON", await TagAsync("#elm-params-1"));
        Assert.AreEqual("BUTTON", await TagAsync("#elm-params-3"));
        Assert.AreEqual("DIV", await TagAsync("#elm-params-outside"));

        await Page.Locator("#elm-params-1").ClickAsync();
        await Page.Locator("#elm-params-2").ClickAsync();
        await Expect(Page.Locator("#toolbar-clicks")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#toolbar-card-clicks")).ToHaveTextAsync("0");

        await Page.Locator("#elm-params-3").ClickAsync();
        await Expect(Page.Locator("#toolbar-clicks")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#toolbar-card-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task StyleAndClass_AreMergedWithTheSplattedOnes()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#elm-style")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        await Expect(Page.Locator("#elm-style")).ToHaveCSSAsync("font-weight", "700");
        await Expect(Page.Locator("#elm-class")).ToHaveClassAsync(HasClass("e2e-element-class"));

        var merged = Page.Locator("#elm-merged");
        await Expect(merged).ToHaveClassAsync(HasClass("own-class"));
        await Expect(merged).ToHaveClassAsync(HasClass("splat-class"));
        await Expect(merged).ToHaveClassAsync(HasClass("bit-elm"));
        await Expect(merged).ToHaveCSSAsync("color", "rgb(60, 179, 113)");
        await Expect(merged).ToHaveCSSAsync("font-style", "italic");
    }

    [TestMethod]
    public async Task CssVariable_TheDisabledOpacity_InheritsIntoNestedElements_WhileARealOpacityStaysOnOne()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#elm-var")).ToHaveCSSAsync("opacity", "0.2");

        await Expect(Page.Locator("#elm-var-fieldset")).ToHaveCSSAsync("opacity", "1");
        await Expect(Page.Locator("#elm-var-inner")).ToHaveCSSAsync("opacity", "1");

        await Expect(Page.Locator("#elm-opacity-fieldset")).ToHaveCSSAsync("opacity", "1");
        await Expect(Page.Locator("#elm-opacity-inner")).ToHaveCSSAsync("opacity", await ResolveAsync("opacity", "var(--bit-opa-dis)"));
    }

    [TestMethod]
    public async Task Rtl_WritesTheDirOnAnyTag_AndAnAncestorsDirIsInherited()
    {
        await OpenAsync(Path);

        foreach (var id in new[] { "#elm-rtl", "#elm-rtl-blockquote" })
        {
            await Expect(Page.Locator(id)).ToHaveAttributeAsync("dir", "rtl");
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass("bit-rtl"));
            await Expect(Page.Locator(id)).ToHaveCSSAsync("direction", "rtl");
        }

        await Expect(Page.Locator("#elm-rtl-inherited")).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
        await Expect(Page.Locator("#elm-rtl-inherited")).ToHaveCSSAsync("direction", "rtl");
    }

    [TestMethod]
    public async Task Unmount_RemovesTheElement_WithoutAnError()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#elm-mounted")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("section#elm-mounted")).ToHaveTextAsync("Mounted");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private Task<string> TagAsync(string selector) => Page.Locator(selector).EvaluateAsync<string>("el => el.tagName");

    // A window listener runs after Blazor's own on the document, so it reads whether the event's default was prevented.
    private Task InstallDefaultPreventedProbeAsync(string eventName)
    {
        return Page.EvaluateAsync("name => window.addEventListener(name, e => { window.__e2eDefaultPrevented = e.defaultPrevented; if (name === 'contextmenu') e.preventDefault(); })", eventName);
    }

    // What a CSS value - a theme token, typically - resolves to on this page, read off a probe element.
    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(
            @"([property, value]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, value);
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return resolved;
            }", new[] { property, value });
    }
}
