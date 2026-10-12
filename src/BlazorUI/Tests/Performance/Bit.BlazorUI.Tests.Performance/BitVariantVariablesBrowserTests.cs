using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the variant tier of the public <c>--bit-&lt;Component&gt;-*</c> variables (#13655): a
/// variable of one variant (<c>--bit-Button-outline-border-color</c>) reaches the instances of that variant and of no
/// other, the shared one it falls back to reaches every variant, and the twin wins over the shared one where both are
/// set - in what each engine actually computed, which the stylesheet contract tests cannot see.
///
/// The tier must not take away what the shared variables could do before it: a variable set on a part's own style
/// still reaches that part, a class an app hands a part still outweighs the variant's rules, an option wrapped in an
/// element of the app's is painted like any other, FixedColor stays readable, and an item that overrides its variant
/// is never handed another variant's tier. Run them with BROWSER=firefox and BROWSER=webkit as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitVariantVariablesBrowserTests
public partial class BitVariantVariablesBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/variant-variables";

    private const string Sentinel = "rgb(1, 2, 3)";
    private const string OtherSentinel = "rgb(4, 5, 6)";
    private const string WidthSentinel = "9px";

    private static readonly string[] Variants = ["fill", "outline", "text"];

    private static readonly string[] Components =
    [
        "Button", "ButtonGroup", "MenuButton", "ToggleButton", "ChoiceGroup", "FileInput", "FileUpload", "OtpInput",
        "TagsInput", "Footer", "Header", "Timeline", "DropMenu", "Pagination", "Badge", "Message", "SnackBar", "Tag"
    ];

    // The words of a twin that only a state the static page is not in paints (a pointer over it, a press, the focus, a
    // filled box, an error, a selected tag), and the knobs that are no color or width. Each of these is pinned by the
    // stylesheet contract tests; the states a part's own style must reach are driven by the scenario tests below.
    private static readonly Regex NotAtRest = new(@"(^|-)(hover|active|focus|invalid|filled|selected|toggle|blur|opacity|translucent)(-|$)", RegexOptions.Compiled);

    // Twins whose part is not rendered by the static page: the remove buttons of a FileInput only exist once files are
    // picked, which AVariableOnAFileInputPartReachesItWhileDisabled does. And a name that only looks like a twin:
    // BitFileInput's --bit-FileInput-text-color is the on-color of its role, not the color of its Text variant.
    private static readonly HashSet<string> NotRenderedAtRest = new(StringComparer.Ordinal)
    {
        "--bit-FileInput-outline-disabled-background",
        "--bit-FileInput-text-disabled-background",
        "--bit-FileInput-text-color",
    };

    // Transitions animate a color from the old value, which getComputedStyle would report for a while after a variable
    // changes; the tests read the end of every change at once instead.
    private const string NoTransitions = "*, *::before, *::after { transition: none !important; animation-duration: 0s !important; }";

    private const string ScanScript = @"([selector, sentinel, isWidth]) => {
        const colorProps = ['color', 'background-color', 'border-top-color', 'border-right-color', 'border-bottom-color',
            'border-left-color', 'outline-color', 'fill', 'stroke', 'box-shadow', 'background-image', 'text-decoration-color', 'caret-color'];
        const widthProps = ['border-top-width', 'border-right-width', 'border-bottom-width', 'border-left-width', 'outline-width'];
        const props = isWidth ? widthProps : colorProps;
        let hits = 0;
        for (const root of document.querySelectorAll(selector)) {
            for (const el of [root, ...root.querySelectorAll('*')]) {
                for (const pseudo of [null, '::before', '::after']) {
                    const cs = getComputedStyle(el, pseudo);
                    for (const p of props) {
                        if ((cs.getPropertyValue(p) || '').includes(sentinel)) hits++;
                    }
                }
            }
        }
        return hits;
    }";

    [TestMethod]
    public async Task AVariantVariableReachesItsOwnVariantAlone()
    {
        await Open();
        var twins = await ReadTwins();
        var failures = new List<string>();
        var checkedCount = 0;

        foreach (var (component, variant, rest) in twins)
        {
            if (NotAtRest.IsMatch(rest) || NotRenderedAtRest.Contains($"--bit-{component}-{variant}-{rest}")) continue;

            var disabled = rest.StartsWith("disabled-", StringComparison.Ordinal);
            var isWidth = rest.EndsWith("-width", StringComparison.Ordinal);
            var sentinel = isWidth ? WidthSentinel : Sentinel;
            var twin = $"--bit-{component}-{variant}-{rest}";
            var shared = $"--bit-{component}-{rest}";

            // The twin alone: its own variant takes it, and no other variant does.
            await SetVariables((twin, sentinel));
            foreach (var other in Variants)
            {
                var hits = await Scan(component, other, disabled, sentinel, isWidth);
                if (other == variant && hits == 0) failures.Add($"{twin} does not reach the {variant} {component}");
                if (other != variant && hits > 0) failures.Add($"{twin} reaches the {other} {component}");
            }

            // The shared one alone reaches the variant through the twin's fallback.
            await SetVariables((shared, sentinel));
            if (await Scan(component, variant, disabled, sentinel, isWidth) == 0) failures.Add($"{shared} does not reach the {variant} {component}");

            // Both: the twin wins.
            if (isWidth is false)
            {
                await SetVariables((shared, OtherSentinel), (twin, sentinel));
                if (await Scan(component, variant, disabled, OtherSentinel, false) > 0) failures.Add($"{shared} wins over {twin} on the {variant} {component}");
                if (await Scan(component, variant, disabled, sentinel, false) == 0) failures.Add($"{twin} loses to {shared} on the {variant} {component}");
            }

            checkedCount++;
        }

        await SetVariables();

        Assert.IsTrue(checkedCount > 100, $"Only {checkedCount} twins were checked; the page or the stylesheet was not read.");
        Assert.AreEqual(0, failures.Count, $"{failures.Count} of {checkedCount} twins misbehave:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [TestMethod]
    public async Task EveryComponentRendersEveryVariantOnThePage()
    {
        await Open();

        foreach (var component in Components)
        {
            foreach (var variant in Variants)
            {
                var count = await Page.Locator($"#variants [data-variant={variant}][data-disabled=false] [data-cmp={component}] *").CountAsync();
                Assert.IsTrue(count > 0, $"The {variant} {component} rendered nothing.");
            }
        }
    }

    [TestMethod]
    public async Task AnItemVariableOnOneItemsStyleReachesItsCard()
    {
        await Open();

        foreach (var variant in new[] { "fill", "outline" })
        {
            var labels = Page.Locator($"[data-case=chg-item-style][data-variant={variant}] .bit-chg-itl");

            Assert.AreEqual("rgb(201, 1, 3)", await Css(labels.Nth(0), "background-color"), $"The checked {variant} card ignores the checked background set on its item.");
            Assert.AreEqual("rgb(201, 1, 1)", await Css(labels.Nth(1), "background-color"), $"The {variant} card ignores the background set on its item.");
            Assert.AreEqual("rgb(201, 1, 2)", await Css(labels.Nth(1), "border-top-color"), $"The {variant} card ignores the border color set on its item.");
        }
    }

    [TestMethod]
    public async Task AWrappedOptionIsPaintedLikeTheOthers()
    {
        await Open();

        foreach (var variant in Variants)
        {
            var group = Page.Locator($"[data-case=btg-wrapped][data-variant={variant}]");
            var wrapped = group.Locator(".wrapper .bit-btg-itm");
            var direct = group.Locator(".bit-btg-itm").Last;

            foreach (var property in new[] { "color", "background-color" })
            {
                Assert.AreEqual(await Css(direct, property), await Css(wrapped, property), $"The wrapped {variant} option is painted apart: {property}.");
            }
        }

        // Filled is the variant the browser's own button look would show through in.
        var fill = Page.Locator("[data-case=btg-wrapped][data-variant=fill] .wrapper .bit-btg-itm");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", await Css(fill, "background-color"));
    }

    [TestMethod]
    public async Task FixedColorWearsTheOnColorOfItsVariant()
    {
        await Open();

        foreach (var (component, part) in new[] { ("Button", ".bit-btn"), ("ToggleButton", ".bit-tgb") })
        {
            foreach (var variant in new[] { "outline", "text" })
            {
                var button = Page.Locator($"[data-case=fixed-color][data-variant={variant}] {part}");

                // A theme recoloring the resting text of the variant is not handed to FixedColor, which has to stay
                // readable on the role fill the hover paints.
                await SetVariables(($"--bit-{component}-{variant}-color", Sentinel), ($"--bit-{component}-{variant}-hover-color", OtherSentinel));
                await Page.Mouse.MoveAsync(0, 0);
                Assert.AreEqual(OtherSentinel, await Css(button, "color"), $"The {variant} {component} with FixedColor does not wear its hover color at rest.");

                await button.HoverAsync();
                Assert.AreEqual(OtherSentinel, await Css(button, "color"), $"The {variant} {component} with FixedColor shifts under the pointer.");
            }

            // A filled one keeps its resting foreground, which is its on-color already.
            var fill = Page.Locator($"[data-case=fixed-color][data-variant=fill] {part}");
            await SetVariables(($"--bit-{component}-fill-color", Sentinel));
            await fill.HoverAsync();
            Assert.AreEqual(Sentinel, await Css(fill, "color"), $"The filled {component} with FixedColor does not keep its foreground under the pointer.");
        }

        await SetVariables();
    }

    [TestMethod]
    public async Task AVariableOnTheOtpBoxesReachesThem()
    {
        await Open();

        foreach (var variant in Variants)
        {
            var inputs = Page.Locator($"[data-case=otp-input-style][data-variant={variant}] .bit-otp-inp");

            Assert.AreEqual("rgb(201, 1, 1)", await Css(inputs.Nth(0), "border-bottom-color"), $"Styles.Input does not reach the {variant} boxes' rule.");
            Assert.AreEqual("rgb(201, 1, 2)", await Css(inputs.Nth(0), "background-color"), $"Styles.Input does not reach the {variant} boxes' background.");
            Assert.AreEqual("rgb(201, 1, 3)", await Css(inputs.Nth(2), "border-bottom-color"), $"Styles.InputsWrapper does not reach the {variant} boxes.");
        }
    }

    [TestMethod]
    public async Task AVariableOnAMenuButtonHalfReachesItsHoverAndPress()
    {
        await Open();

        foreach (var variant in Variants)
        {
            var root = Page.Locator($"[data-case=mnb-half-style][data-variant={variant}]");
            var chevron = root.Locator(".bit-mnb-chb");
            var operatorButton = root.Locator(".bit-mnb-opb");

            await chevron.HoverAsync();
            Assert.AreEqual("rgb(201, 1, 1)", await Css(chevron, "background-color"), $"The hovered {variant} chevron half ignores its own style.");

            await Page.Mouse.DownAsync();
            Assert.AreEqual("rgb(201, 1, 2)", await Css(chevron, "background-color"), $"The pressed {variant} chevron half ignores its own style.");
            await Page.Mouse.MoveAsync(0, 0);
            await Page.Mouse.UpAsync();
            await Page.Keyboard.PressAsync("Escape");

            await operatorButton.HoverAsync();
            Assert.AreEqual("rgb(201, 1, 3)", await Css(operatorButton, "background-color"), $"The hovered {variant} operator half ignores its own style.");
            await Page.Mouse.MoveAsync(0, 0);
        }
    }

    [TestMethod]
    public async Task AVariableOnAFileInputPartReachesItWhileDisabled()
    {
        await Open();

        foreach (var variant in Variants)
        {
            var inputs = Page.Locator($"[data-case=fin-part-style][data-variant={variant}] input[type=file]");
            for (var i = 0; i < 2; i++)
            {
                await inputs.Nth(i).SetInputFilesAsync(new FilePayload { Name = "a.txt", MimeType = "text/plain", Buffer = "a"u8.ToArray() });
            }
        }

        foreach (var variant in Variants)
        {
            await Expect(Page.Locator($"[data-case=fin-part-style][data-variant={variant}] .bit-fin-rbt")).ToHaveCountAsync(2);
        }

        await Page.ClickAsync("#btn-disable");

        foreach (var variant in Variants)
        {
            var root = Page.Locator($"[data-case=fin-part-style][data-variant={variant}]");
            var remove = root.Locator(".bit-fin-rbt").First;

            await Expect(remove).ToBeDisabledAsync();
            Assert.AreEqual("rgb(201, 1, 1)", await Css(remove, "background-color"), $"The disabled {variant} remove button ignores its own style.");
        }

        // The plain remove buttons take the disabled background of their own variant's tier, and of no other.
        foreach (var variant in Variants)
        {
            await SetVariables(($"--bit-FileInput-{variant}-disabled-background", Sentinel));

            foreach (var other in Variants)
            {
                var background = await Css(Page.Locator($"[data-case=fin-part-style][data-variant={other}] [data-plain] .bit-fin-rbt"), "background-color");

                if (other == variant) Assert.AreEqual(Sentinel, background, $"--bit-FileInput-{variant}-disabled-background does not reach the {variant} remove button.");
                else Assert.AreNotEqual(Sentinel, background, $"--bit-FileInput-{variant}-disabled-background reaches the {other} remove button.");
            }
        }

        await SetVariables();

        // Only Fill paints its button with the disabled background; Outline and Text keep it transparent.
        Assert.AreEqual("rgb(201, 1, 2)", await Css(Page.Locator("[data-case=fin-part-style][data-variant=fill] .bit-fin-lbl").First, "background-color"),
                        "The disabled fill button ignores its own style.");
    }

    [TestMethod]
    public async Task AVariableOnAFileUploadLabelReachesItWhileDisabled()
    {
        await Open();

        foreach (var variant in Variants)
        {
            var labels = Page.Locator($"[data-case=upl-label-style][data-variant={variant}] .bit-upl-lbl");

            // The ShowDropArea panel is drawn alike in every variant, with the disabled background and rule.
            Assert.AreEqual("rgb(201, 1, 1)", await Css(labels.Nth(1), "background-color"), $"The disabled {variant} drop area ignores its own background.");
            Assert.AreEqual("rgb(201, 1, 2)", await Css(labels.Nth(1), "border-top-color"), $"The disabled {variant} drop area ignores its own rule.");
        }

        var fill = Page.Locator("[data-case=upl-label-style][data-variant=fill] .bit-upl-lbl").First;
        Assert.AreEqual("rgb(201, 1, 1)", await Css(fill, "background-color"), "The disabled fill browse button ignores its own background.");
        Assert.AreEqual("rgb(201, 1, 2)", await Css(fill, "border-top-color"), "The disabled fill browse button ignores its own rule.");

        var outline = Page.Locator("[data-case=upl-label-style][data-variant=outline] .bit-upl-lbl").First;
        Assert.AreEqual("rgb(201, 1, 2)", await Css(outline, "border-top-color"), "The disabled outline browse button ignores its own rule.");
    }

    [TestMethod]
    public async Task AClassAnAppHandsAPartOutweighsTheVariant()
    {
        await Open();
        await Page.AddStyleTagAsync(new()
        {
            Content = @".app-divider { border-block-end-color: rgb(201, 1, 1); border-block-start-color: rgb(201, 1, 1); }
                        .app-tile { background-color: rgb(201, 1, 2); }
                        .app-button { background-color: rgb(201, 1, 3); }"
        });

        foreach (var variant in new[] { "fill", "text" })
        {
            var root = Page.Locator($"[data-case=class-weight][data-variant={variant}]");

            Assert.AreEqual("rgb(201, 1, 1)", await Css(root.Locator(".bit-hdr"), "border-bottom-color"), $"The {variant} header's divider outweighs the app's class.");
            Assert.AreEqual("rgb(201, 1, 1)", await Css(root.Locator(".bit-ftr"), "border-top-color"), $"The {variant} footer's divider outweighs the app's class.");
        }

        Assert.AreEqual("rgb(201, 1, 2)", await Css(Page.Locator("[data-case=class-weight][data-variant=text] .bit-chg-ihi"), "background-color"),
                        "The text choice group's tile outweighs the app's class.");

        foreach (var variant in Variants)
        {
            Assert.AreEqual("rgb(201, 1, 3)", await Css(Page.Locator($"[data-case=class-weight][data-variant={variant}] .bit-btg-itm").First, "background-color"),
                            $"The {variant} button group's button outweighs the app's class.");
        }
    }

    [TestMethod]
    public async Task AnItemOverridingItsVariantIsNotHandedTheTextTier()
    {
        await Open();
        await SetVariables(("--bit-Timeline-text-dot-background", Sentinel));

        var root = Page.Locator("[data-case=tln-mixed]");

        Assert.AreEqual(Sentinel, await Css(root.Locator(".bit-tln-itm").Nth(0).Locator(".bit-tln-ico"), "background-color"),
                        "The plate of the text item ignores the text dot background.");

        foreach (var index in new[] { 1, 2 })
        {
            Assert.AreEqual("rgba(0, 0, 0, 0)", await Css(root.Locator(".bit-tln-itm").Nth(index).Locator(".bit-tln-ico"), "background-color"),
                            $"Item {index}, which overrides its variant, is handed the text tier on its icon plate.");
        }

        await SetVariables();
    }

    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('[data-cmp=SnackBar] .bit-snb-itm').length === 3", null, new() { Timeout = DefaultTimeout });
        await Page.AddStyleTagAsync(new() { Content = NoTransitions });
    }

    // Every twin of the components on the page, read off the stylesheet the page loads: a variable of a variant whose
    // shared variable the stylesheet reads as well.
    private async Task<List<(string Component, string Variant, string Name)>> ReadTwins()
    {
        var css = await Page.EvaluateAsync<string>("() => fetch('_content/Bit.BlazorUI/styles/bit.blazorui.css').then(r => r.text())");
        var twins = new List<(string, string, string)>();

        foreach (var component in Components)
        {
            var names = Regex.Matches(css, $@"--bit-{component}-[a-z0-9-]+").Select(m => m.Value).ToHashSet(StringComparer.Ordinal);

            foreach (var name in names.Order(StringComparer.Ordinal))
            {
                foreach (var variant in Variants)
                {
                    var prefix = $"--bit-{component}-{variant}-";
                    if (name.StartsWith(prefix, StringComparison.Ordinal) is false) continue;

                    var rest = name[prefix.Length..];
                    if (names.Contains($"--bit-{component}-{rest}")) twins.Add((component, variant, rest));
                }
            }
        }

        return twins;
    }

    private Task SetVariables(params (string Name, string Value)[] variables)
    {
        // Only the variables a previous call set are taken back off: #scenarios lays itself out with a style of its own.
        return Page.EvaluateAsync(@"(variables) => {
            for (const id of ['variants', 'scenarios']) {
                const el = document.getElementById(id);
                for (const name of (window.__bitVariantVars || [])) el.style.removeProperty(name);
                for (const [name, value] of variables) el.style.setProperty(name, value);
            }
            window.__bitVariantVars = variables.map(([name]) => name);
        }", variables.Select(v => new[] { v.Name, v.Value }).ToArray());
    }

    private Task<int> Scan(string component, string variant, bool disabled, string sentinel, bool isWidth)
    {
        var selector = $"#variants [data-variant={variant}][data-disabled={(disabled ? "true" : "false")}] [data-cmp={component}]";

        return Page.EvaluateAsync<int>(ScanScript, new object[] { selector, sentinel, isWidth });
    }

    private static Task<string> Css(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>("(el, property) => getComputedStyle(el).getPropertyValue(property)", property);
    }
}
