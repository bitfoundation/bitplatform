using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for how a disabled component says so in its markup (#13549), against the live DOM of every
/// engine (BROWSER=chromium, firefox, webkit): the bit-dis class on the root of a disabled component, and on every native
/// control rendered disabled the disabled attribute together with aria-disabled="true" - so that :disabled and
/// [aria-disabled="true"] each find it - on the first render, and again after Disabled is turned off and back on at
/// runtime, which is the renderer's diff taking both attributes away and putting them back.
/// </summary>
/// <remarks>
/// The bUnit contract tests read the markup and render the components; what only a browser shows is the DOM the
/// engine builds of it - a relocated popup, a control the component's script creates or changes, the :disabled the
/// engine computes - and that the page runs without a circuit error.
/// </remarks>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitDisabledStateBrowserTests
public class BitDisabledStateBrowserTests : PerformanceTestBase
{
    // Every component the change touched, one ?part= of the test page each.
    public static IEnumerable<object[]> Parts =>
    [
        ["actionbutton"], ["button"], ["button-loading"], ["buttongroup"], ["menubutton"], ["togglebutton"],
        ["calendar"], ["calendar-range"], ["checkbox"], ["choicegroup"], ["circulartimepicker"], ["colorpicker"],
        ["datepicker"], ["datepicker-input"], ["daterangepicker"], ["dropdown"], ["fileinput"], ["fileupload"],
        ["numberfield"], ["otpinput"], ["rating"], ["searchbox"], ["slider"], ["slider-ranged"], ["tagsinput"],
        ["textfield"], ["timepicker"], ["toggle"],
        ["basiclist"], ["carousel"], ["swiper"],
        ["breadcrumb"], ["dropmenu"], ["nav"], ["navbar"], ["pagination"], ["pivot"],
        ["badge"], ["message"], ["persona"], ["tag"],
        ["card"], ["dialog"], ["dialog-ok-off"], ["modal"], ["panel"],
        ["link"], ["element-div"],
        ["accentcolorswitcher"], ["themeswitcher"], ["chart"], ["countrypicker"], ["datagrid"], ["fullcalendar"], ["fullcalendar-week"],
        ["markdowneditor"], ["markdownviewer"], ["pdfviewer-empty"], ["pdfviewer"], ["phoneinput"], ["richtexteditor"], ["timezonepicker"],
        ["richtexteditor-readonly"],
    ];

    // The parts that render native controls disabled while they are enabled: a bound reached, nothing to act on yet,
    // read-only content.
    public static IEnumerable<object[]> PartsDisabledWhileEnabled =>
    [
        ["calendar-range"], ["pagination"], ["pivot"], ["dialog-ok-off"], ["datagrid"], ["markdownviewer"],
        ["pdfviewer-empty"], ["richtexteditor-readonly"],
    ];

    // The parts whose controls are disabled by what they hold rather than by Disabled - a task list with no handler, a
    // viewer with no document, the first page of one - so enabling them leaves those as they are. A disabled PDF viewer
    // makes its regions inert instead (PdfViewerDisabledMakesItsRegionsInert).
    private static readonly HashSet<string> DisabledByContent = ["markdownviewer", "pdfviewer-empty", "pdfviewer"];

    // The parts whose Disabled locks the content (the events of a calendar) and leaves its navigation live, so no
    // control of theirs is disabled.
    private static readonly HashSet<string> NavigableWhileDisabled = ["fullcalendar", "fullcalendar-week"];

    // The controls kept focusable while unavailable, which carry aria-disabled alone.
    public static IEnumerable<object[]> FocusableParts =>
    [
        ["button-adf", "button"], ["checkbox-adf", "input"], ["toggle-adf", "button"], ["buttongroup-di", "button"],
    ];

    private readonly List<string> _errors = [];

    [TestMethod, DynamicData(nameof(Parts))]
    public async Task DisabledComponentPairsEveryDisabledControlAndFollowsTheToggle(string part)
    {
        await OpenPage(part, disabled: true);

        var disabled = await Audit();
        AssertPaired(part, "disabled", disabled);
        Assert.IsTrue(disabled.RootIsDisabled, $"{part}: the root of the disabled component has no bit-dis class. {disabled}");
        if (NavigableWhileDisabled.Contains(part) is false)
        {
            Assert.IsTrue(disabled.DisabledCount + disabled.AriaDisabledCount > 0,
                          $"{part}: the disabled component says it is disabled nowhere in its controls. {disabled}");
        }

        await Toggle("Enabled");

        var enabled = await Audit();
        AssertPaired(part, "enabled", enabled);
        Assert.IsFalse(enabled.RootIsDisabled, $"{part}: the root of the enabled component still has the bit-dis class. {enabled}");
        if (DisabledByContent.Contains(part))
        {
            Assert.AreEqual(disabled.DisabledCount, enabled.DisabledCount,
                            $"{part}: the controls its content disables changed with Disabled. {enabled}");
        }
        else if (disabled.DisabledCount > 0)
        {
            Assert.IsTrue(enabled.DisabledCount < disabled.DisabledCount,
                          $"{part}: enabling the component left its {disabled.DisabledCount} disabled controls disabled. {enabled}");
        }

        await Toggle("Disabled");

        var again = await Audit();
        AssertPaired(part, "disabled again", again);
        Assert.IsTrue(again.RootIsDisabled, $"{part}: the root lost its bit-dis class after a round trip. {again}");
        Assert.AreEqual(disabled.DisabledCount, again.DisabledCount,
                        $"{part}: a round trip through enabled disables a different number of controls. {again}");

        AssertNoErrors(part);
    }

    [TestMethod, DynamicData(nameof(PartsDisabledWhileEnabled))]
    public async Task EnabledComponentPairsTheControlsItDisablesOnItsOwn(string part)
    {
        await OpenPage(part, disabled: false);

        var audit = await Audit();
        AssertPaired(part, "enabled", audit);
        Assert.IsFalse(audit.RootIsDisabled, $"{part}: the root of the enabled component has the bit-dis class. {audit}");
        Assert.IsTrue(audit.DisabledCount > 0, $"{part}: the enabled component disables no control of its own here. {audit}");

        AssertNoErrors(part);
    }

    [TestMethod, DynamicData(nameof(FocusableParts))]
    public async Task FocusableDisabledControlCarriesAriaDisabledAlone(string part, string tag)
    {
        await OpenPage(part, disabled: true);

        AssertPaired(part, "disabled", await Audit());

        var control = Page.Locator($"[data-case-root] {tag}[aria-disabled='true'], {tag}[data-case-root][aria-disabled='true']").First;
        await Expect(control).ToHaveCountAsync(1);
        await Expect(control).Not.ToHaveAttributeAsync("disabled", new System.Text.RegularExpressions.Regex(".*"));

        await control.FocusAsync();
        Assert.IsTrue(await control.EvaluateAsync<bool>("e => document.activeElement === e"),
                      $"{part}: the control kept focusable while disabled does not take the focus.");

        AssertNoErrors(part);
    }

    // A boolean splatted through InputHtmlAttributes is written the way the renderer writes any splatted boolean: with
    // no value while true, left out while false; a string is written as it is; and Disabled writes "true" over all three.
    [TestMethod]
    [DataRow("textfield-splat-bool-true", "")]
    [DataRow("textfield-splat-bool-false", null)]
    [DataRow("textfield-splat-string", "true")]
    public async Task SplattedAriaDisabledOfAnInputFollowsTheRenderersRules(string part, string? expected)
    {
        await OpenPage(part, disabled: false);

        var input = Page.Locator("[data-case-root] input").First;
        Assert.AreEqual(expected, await input.GetAttributeAsync("aria-disabled"), $"{part}: enabled");
        Assert.IsNull(await input.GetAttributeAsync("disabled"), $"{part}: the enabled input is disabled");

        await Toggle("Disabled");

        Assert.AreEqual("true", await input.GetAttributeAsync("aria-disabled"), $"{part}: disabled");
        Assert.IsTrue(await input.IsDisabledAsync(), $"{part}: the disabled input is not disabled");

        await Toggle("Enabled");

        Assert.AreEqual(expected, await input.GetAttributeAsync("aria-disabled"), $"{part}: enabled again");

        AssertNoErrors(part);
    }

    [TestMethod]
    public async Task MarkdownTaskCheckboxWithoutAHandlerIsDisabledBothWays()
    {
        await OpenPage("markdownviewer", disabled: false);

        var boxes = Page.Locator("input.task-list-item-checkbox");
        await Expect(boxes).ToHaveCountAsync(2);

        for (var i = 0; i < 2; i++)
        {
            await Expect(boxes.Nth(i)).ToBeDisabledAsync();
            await Expect(boxes.Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        Assert.AreEqual(2, await Page.Locator("input.task-list-item-checkbox:disabled[aria-disabled='true']").CountAsync());

        AssertNoErrors("markdownviewer");
    }

    // A disabled control does not act on a click the pointer or the keyboard delivers.
    [TestMethod]
    [DataRow("button")]
    [DataRow("actionbutton")]
    [DataRow("togglebutton")]
    public async Task DisabledButtonRunsNoClickHandler(string part)
    {
        await OpenPage(part, disabled: true);

        var button = Page.Locator("button[data-case-root], [data-case-root] button").First;
        await button.ClickAsync(new() { Force = true });
        await button.DispatchEventAsync("click");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#v-clicks")).ToHaveTextAsync("0");

        await Toggle("Enabled");
        await button.ClickAsync();
        await Expect(Page.Locator("#v-clicks")).ToHaveTextAsync("1");

        AssertNoErrors(part);
    }

    [TestMethod]
    public async Task PdfViewerDisabledMakesItsRegionsInert()
    {
        await OpenPage("pdfviewer", disabled: true);

        var inert = Page.Locator("[data-case-root] [inert]");
        Assert.IsTrue(await inert.CountAsync() > 0, "A disabled PDF viewer has no inert region.");

        await Toggle("Enabled");
        await Expect(inert).ToHaveCountAsync(0);

        await Toggle("Disabled");
        Assert.IsTrue(await inert.CountAsync() > 0, "The PDF viewer lost its inert regions after a round trip.");

        AssertNoErrors("pdfviewer");
    }

    private async Task OpenPage(string part, bool disabled)
    {
        // The base class opens the host's home page first, and leaving it while its circuit is still negotiating makes
        // Firefox and WebKit log the aborted fetch. A blank page in between ends that one before the errors are counted.
        await Page.GotoAsync("about:blank");
        await Page.WaitForTimeoutAsync(100);

        Page.Console += (_, message) =>
        {
            if (message.Type == "error") _errors.Add(message.Text);
        };
        Page.PageError += (_, error) => _errors.Add(error);

        await Page.GotoAsync($"{BaseUrl}/regression/disabled-state?part={part}&disabled={(disabled ? 1 : 0)}");
        await WaitForStatus("Ready");
        await Settle(part);
    }

    private async Task Toggle(string status)
    {
        // Clicked through the DOM: an open dialog, modal or panel covers the page's own controls from the pointer.
        await Page.Locator("#btn-toggle").EvaluateAsync("button => button.click()");
        await WaitForStatus(status);
        await Settle(null);
    }

    // Lets the scripts a component runs after its render - a callout, a measured overflow, a chart, a loaded document -
    // finish before the DOM is read.
    private async Task Settle(string? part)
    {
        if (part is "pdfviewer")
        {
            await Page.WaitForFunctionAsync("() => document.querySelectorAll('[data-case-root] .bit-pdv-page').length > 0",
                                            null, new() { Timeout = DefaultTimeout });
        }

        await Page.WaitForTimeoutAsync(part is null ? 250 : 600);
    }

    private void AssertPaired(string part, string state, AuditResult audit)
    {
        Assert.IsTrue(audit.RootFound, $"{part} ({state}): no element carries the data-case-root splatted onto the component.");
        Assert.AreEqual(0, audit.Unpaired.Length,
                        $"{part} ({state}): these controls are disabled without aria-disabled=\"true\":\n{string.Join("\n", audit.Unpaired)}");
        Assert.AreEqual(0, audit.NotMatchingDisabled.Length,
                        $"{part} ({state}): these controls carry the disabled attribute but do not match :disabled:\n{string.Join("\n", audit.NotMatchingDisabled)}");
        Assert.AreEqual(0, audit.NonNative.Length,
                        $"{part} ({state}): these elements carry the disabled attribute, which HTML defines on the form controls only:\n{string.Join("\n", audit.NonNative)}");
    }

    private void AssertNoErrors(string part)
    {
        Assert.AreEqual(0, _errors.Count, $"{part}: the page logged errors:\n{string.Join("\n", _errors)}");
    }

    // The disabled state of the whole document but the test's own controls: a relocated popup sits outside the case.
    private async Task<AuditResult> Audit()
    {
        var json = await Page.EvaluateAsync<string>(
            """
            () => {
                const native = ['BUTTON', 'INPUT', 'SELECT', 'TEXTAREA', 'FIELDSET', 'OPTION', 'OPTGROUP'];
                const describe = e => e.tagName.toLowerCase() + (e.id ? '#' + e.id : '') +
                                      (e.className && typeof e.className === 'string' ? '.' + e.className.trim().split(/\s+/).join('.') : '') +
                                      (e.getAttribute('aria-label') ? ` [${e.getAttribute('aria-label')}]` : '') +
                                      ` aria-disabled=${e.getAttribute('aria-disabled')}`;
                const outside = e => e.closest('#test-controls') === null;
                const disabled = [...document.querySelectorAll('[disabled]')].filter(outside)
                    .filter(e => !(e.tagName === 'INPUT' && e.type === 'hidden'));
                const nativeDisabled = disabled.filter(e => native.includes(e.tagName));
                const root = document.querySelector('[data-case-root]');
                return JSON.stringify({
                    rootFound: root !== null,
                    rootIsDisabled: root !== null && root.classList.contains('bit-dis'),
                    disabledCount: nativeDisabled.length,
                    ariaDisabledCount: [...document.querySelectorAll('[aria-disabled="true"]')].filter(outside).length,
                    unpaired: nativeDisabled.filter(e => e.getAttribute('aria-disabled') !== 'true').map(describe),
                    notMatchingDisabled: nativeDisabled.filter(e => !e.matches(':disabled')).map(describe),
                    nonNative: disabled.filter(e => !native.includes(e.tagName)).map(describe),
                });
            }
            """);

        return JsonSerializer.Deserialize<AuditResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private sealed record AuditResult(bool RootFound, bool RootIsDisabled, int DisabledCount, int AriaDisabledCount,
                                      string[] Unpaired, string[] NotMatchingDisabled, string[] NonNative)
    {
        public override string ToString() =>
            $"(native disabled: {DisabledCount}, aria-disabled: {AriaDisabledCount}, root bit-dis: {RootIsDisabled})";
    }
}
