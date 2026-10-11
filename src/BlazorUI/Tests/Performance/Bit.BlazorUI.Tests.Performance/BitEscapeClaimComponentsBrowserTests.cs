using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the Escape claim (Utils.claimEscape) of the components that act on the key in their
/// .NET handlers - the Extras' dialogs, panels and popups, and the calendar and the snack bar of the core - driven with
/// real keyboard input, in every engine (BROWSER=chromium, firefox, webkit) and inside every surface.
///
/// One press does one thing: the component's own state closes on the first Escape and the surface around it on the
/// next. An Escape with a modifier is claimed by nobody, so it closes the surface and leaves the component alone - shown
/// with the component on the page itself (?surface=none), where nothing closes on it. A component whose popup is open
/// and a shown tooltip are left the key even inside a root that claims every Escape pressed in it.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitEscapeClaimComponentsBrowserTests
public class BitEscapeClaimComponentsBrowserTests : PerformanceTestBase
{
    // ---- BitFullCalendar ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task FullCalendar_AddDialog_ClosesFirstAndTheSurfaceOnTheNextPress(string surface)
    {
        await OpenSurface(surface, "fc");

        await OpenFcAddDialog();

        await PressEscapeAndExpectOpen();
        await Expect(FcDialog).ToHaveCountAsync(0);

        await Page.Locator("#surface-btn").FocusAsync();
        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task FullCalendar_AddDialog_AFieldOfTheTemplateClearsFirst(string surface)
    {
        await OpenSurface(surface, "fc");

        await OpenFcAddDialog();
        await FocusAndType("#w-fcf input", "abc");
        await Expect(Page.Locator("#v-field")).ToHaveTextAsync("abc");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-field")).ToHaveTextAsync("");
        await Expect(FcDialog).ToHaveCountAsync(1);

        await PressEscapeAndExpectOpen();
        await Expect(FcDialog).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task FullCalendar_AddDialog_ATooltipOfTheTemplateGoesFirst(string surface)
    {
        await OpenSurface(surface, "fc");

        await OpenFcAddDialog();
        await Page.Locator("#fc-tip-btn").FocusAsync();
        var tooltip = Page.Locator("#w-fct .bit-ttp-wrp.bit-ttp-vis");
        await Expect(tooltip).ToHaveCountAsync(1);

        await PressEscapeAndExpectOpen();
        await Expect(tooltip).ToHaveCountAsync(0);
        await Expect(FcDialog).ToHaveCountAsync(1);

        await PressEscapeAndExpectOpen();
        await Expect(FcDialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task FullCalendar_AddDialog_AModifiedEscapeIsLeftAlone()
    {
        await OpenPage("none", "fc");

        await OpenFcAddDialog();

        await Page.Keyboard.PressAsync("Shift+Escape");
        await Page.WaitForTimeoutAsync(300);
        await Expect(FcDialog).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(FcDialog).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task FullCalendar_DateTimePicker_ClosesItsPopupFirst(string surface)
    {
        await OpenSurface(surface, "fc");

        await OpenFcAddDialog();
        var trigger = Page.Locator(".bit-bfc-dtp-trigger").First;
        await trigger.ClickAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");

        await PressEscapeAndExpectOpen();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(FcDialog).ToHaveCountAsync(1);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task FullCalendar_DetailsDialog_ClosesFirst(string surface)
    {
        await OpenSurface(surface, "fc");

        await Page.Locator(".bit-bfc-event-badge[data-bit-bfc-event=\"a\"]").ClickAsync();
        await Expect(FcDialog).ToHaveCountAsync(1);
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(FcDialog).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task FullCalendar_ScopeDialog_ClosesFirstAndTheDetailsOnTheNextPress(string surface)
    {
        await OpenSurface(surface, "fc");

        await Page.Locator(".bit-bfc-event-badge[data-bit-bfc-event^=\"r@\"]").First.ClickAsync();
        await Expect(FcDialog).ToHaveCountAsync(1);
        await Settle();
        await FcDialog.Locator(".bit-bfc-btn-danger").ClickAsync();
        var scope = Page.Locator(".bit-bfc-scope-dialog");
        await Expect(scope).ToHaveCountAsync(1);
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(scope).ToHaveCountAsync(0);
        await Expect(FcDialog).ToHaveCountAsync(1);

        await FcDialog.FocusAsync();
        await PressEscapeAndExpectOpen();
        await Expect(FcDialog).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task FullCalendar_ListDialog_ClosesFirst(string surface)
    {
        await OpenSurface(surface, "fc");

        await Page.Locator(".bit-bfc-month-more").First.ClickAsync();
        await Expect(FcDialog).ToHaveCountAsync(1);
        await Settle();
        await FcDialog.FocusAsync();

        await PressEscapeAndExpectOpen();
        await Expect(FcDialog).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task FullCalendar_Settings_ClosesFirst(string surface)
    {
        await OpenSurface(surface, "fc");

        var gear = Page.Locator(".bit-bfc-dropdown > button[aria-haspopup=dialog]");
        await gear.ClickAsync();
        await Expect(gear).ToHaveAttributeAsync("aria-expanded", "true");
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(gear).ToHaveAttributeAsync("aria-expanded", "false");
    }

    // ---- BitNavPanel ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task NavPanel_ClosesItsDrawerFirst(string surface)
    {
        await OpenSurface(surface, "npn");
        await Expect(Page.Locator("#v-npn")).ToHaveTextAsync("open");

        await Page.Locator(".bit-npn .bit-srb-inp").FocusAsync();

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-npn")).ToHaveTextAsync("closed");
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task NavPanel_EmptiesTheSearchFirstThenClosesTheDrawer(string surface)
    {
        await OpenSurface(surface, "npn");

        await FocusAndType(".bit-npn .bit-srb-inp", "set");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator(".bit-npn .bit-srb-inp")).ToHaveValueAsync("");
        await Expect(Page.Locator("#v-npn")).ToHaveTextAsync("open");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-npn")).ToHaveTextAsync("closed");
    }

    [TestMethod, DataRow("none"), DataRow("dialog")]
    public async Task NavPanel_ADatePickerInsideItClosesItsPopupFirst(string surface)
    {
        await OpenSurface(surface, "npn");

        var input = Page.Locator("#w-npn-dp .bit-dtp-inp");
        await input.ClickAsync();
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#v-npn")).ToHaveTextAsync("open");
    }

    [TestMethod, DataRow("none"), DataRow("dialog")]
    public async Task NavPanel_ATooltipInsideItGoesFirst(string surface)
    {
        await OpenSurface(surface, "npn");

        await Page.Locator("#npn-tip-btn").FocusAsync();
        var tooltip = Page.Locator("#w-npn-tt .bit-ttp-wrp.bit-ttp-vis");
        await Expect(tooltip).ToHaveCountAsync(1);

        await PressEscapeAndExpectOpen();
        await Expect(tooltip).ToHaveCountAsync(0);
        await Expect(Page.Locator("#v-npn")).ToHaveTextAsync("open");
    }

    [TestMethod]
    public async Task NavPanel_AModifiedEscapeIsLeftAlone()
    {
        await OpenPage("none", "npn");

        await Page.Locator(".bit-npn .bit-srb-inp").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Escape");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#v-npn")).ToHaveTextAsync("open");
    }

    // ---- BitPdfViewer ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task PdfViewer_ClosesItsFindBoxFirst(string surface)
    {
        await OpenSurface(surface, "pdv");
        await OpenPdfFind();

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator(".bit-pdv-search-box")).ToHaveCountAsync(0);

        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task PdfViewer_ClosesItsPropertiesFirst(string surface)
    {
        await OpenSurface(surface, "pdv");
        await WaitForPdf();

        await Page.Locator("button.bit-pdv-btn[title=\"Document properties\"]").ClickAsync();
        var properties = Page.Locator(".bit-pdv-dialog[role=dialog]");
        await Expect(properties).ToHaveCountAsync(1);
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(properties).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("Shift+Escape"), DataRow("Alt+Escape")]
    public async Task PdfViewer_AModifiedEscapeIsLeftAlone(string key)
    {
        await OpenPage("none", "pdv");
        await OpenPdfFind();

        await Page.Keyboard.PressAsync(key);
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator(".bit-pdv-search-box")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task PdfViewer_AModifiedEscapeClosesTheSurface()
    {
        await OpenSurface("dialog", "pdv");
        await OpenPdfFind();

        await Page.Keyboard.PressAsync("Shift+Escape");
        await Expect(Page.Locator("#surface-state")).ToHaveTextAsync("closed");
    }

    // ---- BitMap ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task Map_ClosesItsPopupFirst(string surface)
    {
        await OpenSurface(surface, "map");

        await Page.Locator(".bit-map-marker-table-action").First.ClickAsync();
        var popup = Page.Locator(".bit-map-popup[role=dialog]");
        await Expect(popup).ToHaveCountAsync(1);
        await Settle();
        await Page.Locator("#map-popup-btn").FocusAsync();

        await PressEscapeAndExpectOpen();
        await Expect(popup).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task Map_LeavesTheCanvasFirst(string surface)
    {
        await OpenSurface(surface, "map");

        var canvas = Page.Locator(".bit-map-canvas");
        await canvas.FocusAsync();
        await Expect(canvas).ToBeFocusedAsync();

        await PressEscapeAndExpectOpen();
        await Expect(canvas).Not.ToBeFocusedAsync();
    }

    // ---- BitChart ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task Chart_LetsGoOfItsKeyboardPositionFirst(string surface)
    {
        await OpenSurface(surface, "cht");

        var svg = Page.Locator(".bit-cht-svg");
        await svg.FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(svg).ToHaveAttributeAsync("data-bit-esc", "claim");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator(".bit-cht-focus-ring")).ToHaveCountAsync(0);
        await Expect(svg).Not.ToHaveAttributeAsync("data-bit-esc", "claim");

        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task Chart_DismissesATooltipThePointerOpenedFirst(string surface)
    {
        await OpenSurface(surface, "cht");

        // The tooltip is the pointer's while the focus is elsewhere in the surface.
        await Page.Locator("#surface-btn").FocusAsync();
        await Page.Locator(".bit-cht-svg g.bit-cht-el").First.HoverAsync();
        var tooltip = Page.Locator(".bit-cht-tt");
        await Expect(tooltip).ToHaveCountAsync(1);

        await PressEscapeAndExpectOpen();
        await Expect(tooltip).ToHaveCountAsync(0);

        await PressEscapeAndExpectClosed();
    }

    [TestMethod]
    public async Task Chart_LeavesAModifiedEscapeAndATooltipThePointerOpenedToTheSurface()
    {
        await OpenSurface("dialog", "cht");

        await Page.Locator("#surface-btn").FocusAsync();
        await Page.Locator(".bit-cht-svg g.bit-cht-el").First.HoverAsync();
        await Expect(Page.Locator(".bit-cht-tt")).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("Shift+Escape");
        await Expect(Page.Locator("#surface-state")).ToHaveTextAsync("closed");
    }

    // ---- BitDataGrid ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task DataGrid_RowEditWithoutCellNavigation_CancelsFirst(string surface)
    {
        await OpenSurface(surface, "dtg");

        await Page.Locator(".bit-dtg-cell-command button").First.ClickAsync();
        var editor = Page.Locator("input.bit-dtg-editor");
        await Expect(editor).ToHaveCountAsync(1);
        await Expect(editor).ToBeFocusedAsync();

        await PressEscapeAndExpectOpen();
        await Expect(editor).ToHaveCountAsync(0);

        // The focus is handed to the row's Edit button, still inside the surface.
        await Expect(Page.Locator(".bit-dtg-cell-command button").First).ToBeFocusedAsync();
        await PressEscapeAndExpectClosed();
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task DataGrid_RowEditWithCellNavigation_CancelsFirst(string surface)
    {
        await OpenSurface(surface, "dtg-nav");

        await Page.Locator(".bit-dtg-cell-command button").First.ClickAsync();
        var editor = Page.Locator("input.bit-dtg-editor");
        await Expect(editor).ToHaveCountAsync(1);
        await editor.FocusAsync();

        await PressEscapeAndExpectOpen();
        await Expect(editor).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task DataGrid_RowEdit_AModifiedEscapeIsLeftAlone()
    {
        await OpenPage("none", "dtg");

        await Page.Locator(".bit-dtg-cell-command button").First.ClickAsync();
        var editor = Page.Locator("input.bit-dtg-editor");
        await Expect(editor).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+Escape");
        await Page.WaitForTimeoutAsync(300);
        await Expect(editor).ToHaveCountAsync(1);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task DataGrid_ColumnChooser_ClosesFirst(string surface)
    {
        await OpenSurface(surface, "dtg");

        await Page.Locator(".bit-dtg-toolbar-end button.bit-dtg-btn[aria-controls]").ClickAsync();
        var chooser = Page.Locator(".bit-dtg-column-chooser");
        await Expect(chooser).ToHaveCountAsync(1);
        await chooser.Locator("input[type=checkbox]").First.FocusAsync();

        await PressEscapeAndExpectOpen();
        await Expect(chooser).ToHaveCountAsync(0);
    }

    // ---- BitMarkdownEditor ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task MarkdownEditor_ClosesItsFindPanelFirst(string surface)
    {
        await OpenSurface(surface, "mde");

        await Page.Locator("textarea.bit-mde-txa").FocusAsync();
        await Page.Keyboard.PressAsync("Control+f");
        var find = Page.Locator(".bit-mde-fnd");
        await Expect(find).ToHaveCountAsync(1);
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(find).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task MarkdownEditor_ClosesItsHelpFirst(string surface)
    {
        await OpenSurface(surface, "mde");

        await Page.Locator("button[data-cmd=help]").ClickAsync();
        var help = Page.Locator(".bit-mde-hcr[role=dialog]");
        await Expect(help).ToHaveCountAsync(1);
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(help).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task MarkdownEditor_LeavesFullScreenFirst(string surface)
    {
        await OpenSurface(surface, "mde");

        await Page.Locator("textarea.bit-mde-txa").FocusAsync();
        await Page.Keyboard.PressAsync("F11");
        await Expect(Page.Locator("#v-fs")).ToHaveTextAsync("on");
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator("#v-fs")).ToHaveTextAsync("off");
    }

    [TestMethod]
    public async Task MarkdownEditor_AModifiedEscapeIsLeftAlone()
    {
        await OpenPage("none", "mde");

        await Page.Locator("textarea.bit-mde-txa").FocusAsync();
        await Page.Keyboard.PressAsync("F11");
        await Expect(Page.Locator("#v-fs")).ToHaveTextAsync("on");
        await Settle();

        await Page.Keyboard.PressAsync("Shift+Escape");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#v-fs")).ToHaveTextAsync("on");

        await Page.Locator("button[data-cmd=help]").ClickAsync();
        var help = Page.Locator(".bit-mde-hcr[role=dialog]");
        await Expect(help).ToHaveCountAsync(1);
        await Settle();

        await Page.Keyboard.PressAsync("Shift+Escape");
        await Page.WaitForTimeoutAsync(300);
        await Expect(help).ToHaveCountAsync(1);
    }

    // ---- BitRichTextEditor ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task RichTextEditor_ClosesItsLinkPanelFirst(string surface)
    {
        await OpenSurface(surface, "rte");

        await Page.Locator("button[aria-label=\"Insert or edit link\"]").ClickAsync();
        var panel = Page.Locator("[id$=\"-link-panel\"]");
        await Expect(panel).ToHaveCountAsync(1);
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(panel).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task RichTextEditor_ClosesItsSlashMenuFirst(string surface)
    {
        await OpenSurface(surface, "rte");
        await OpenRteMenu("/");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator(".bit-rte-slash")).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task RichTextEditor_ClosesItsMentionMenuFirst(string surface)
    {
        await OpenSurface(surface, "rte");
        await OpenRteMenu("@");

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator(".bit-rte-slash")).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("/"), DataRow("@")]
    public async Task RichTextEditor_AModifiedEscapeIsLeftAloneByItsMenus(string trigger)
    {
        await OpenPage("none", "rte");
        await OpenRteMenu(trigger);

        await Page.Keyboard.PressAsync("Shift+Escape");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator(".bit-rte-slash")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task RichTextEditor_AModifiedEscapeInItsSlashMenuClosesTheSurface()
    {
        await OpenSurface("dialog", "rte");
        await OpenRteMenu("/");

        await Page.Keyboard.PressAsync("Shift+Escape");
        await Expect(Page.Locator("#surface-state")).ToHaveTextAsync("closed");
    }

    // ---- BitCalendar ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task Calendar_LeavesTheYearPickerThenTheMonthPickerFirst(string surface)
    {
        await OpenSurface(surface, "cal");

        await Page.Locator(".bit-cal-dwp .bit-cal-ptb").First.ClickAsync();
        await Expect(Page.Locator(".bit-cal-mwp")).ToHaveCountAsync(1);
        await Page.Locator(".bit-cal-mwp .bit-cal-ptb[title$=\"change year\"]").ClickAsync();
        await Expect(Page.Locator(".bit-cal-mwp .bit-cal-ptb[title$=\"change month\"]")).ToHaveCountAsync(1);
        await Page.Locator(".bit-cal-mwp .bit-cal-ptb").First.FocusAsync();

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator(".bit-cal-mwp .bit-cal-ptb[title$=\"change year\"]")).ToHaveCountAsync(1);

        await Page.Locator(".bit-cal-mwp .bit-cal-ptb").First.FocusAsync();
        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator(".bit-cal-mwp")).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task Calendar_HidesItsTimePickerFirst(string surface)
    {
        await OpenSurface(surface, "cal");

        await Page.Locator("button.bit-cal-nbt[title=\"Show time picker\"]").ClickAsync();
        await Expect(Page.Locator(".bit-cal-twp")).ToHaveCountAsync(1);
        await Page.Locator(".bit-cal-twp input.bit-cal-tin").First.FocusAsync();

        await PressEscapeAndExpectOpen();
        await Expect(Page.Locator(".bit-cal-twp")).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task Calendar_ClosesItsEventDialogFirst(string surface)
    {
        await OpenSurface(surface, "cal");

        await Page.Locator("button.bit-cal-dbt[aria-haspopup=dialog]").First.ClickAsync();
        var dialog = Page.Locator(".bit-cal-emc[role=dialog]");
        await Expect(dialog).ToHaveCountAsync(1);
        await Settle();

        await PressEscapeAndExpectOpen();
        await Expect(dialog).ToHaveCountAsync(0);
    }

    // ---- BitSnackBar ----

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task SnackBar_DismissesItsItemFirst(string surface)
    {
        await OpenSurface(surface, "snb");

        await Page.Locator("#btn-snb").ClickAsync();
        var item = Page.Locator(".bit-snb-itm");
        await Expect(item).ToHaveCountAsync(1);
        await item.Locator("button.bit-snb-cbt").FocusAsync();

        await PressEscapeAndExpectOpen();
        await Expect(item).ToHaveCountAsync(0);
    }

    [TestMethod, DataRow("dialog"), DataRow("modal"), DataRow("panel")]
    public async Task SnackBar_APersistentItemLeavesTheKeyToTheSurface(string surface)
    {
        await OpenSurface(surface, "snb");

        await Page.Locator("#btn-snb-persistent").ClickAsync();
        var item = Page.Locator(".bit-snb-itm");
        await Expect(item).ToHaveCountAsync(1);
        await item.FocusAsync();

        await PressEscapeAndExpectClosed();
    }

    // ---- helpers ----

    private ILocator FcDialog => Page.Locator(".bit-bfc-overlay > .bit-bfc-dialog");

    private async Task OpenFcAddDialog()
    {
        await Page.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        await Expect(FcDialog).ToHaveCountAsync(1);
        await Settle();
    }

    private async Task WaitForPdf()
    {
        await Expect(Page.Locator("button.bit-pdv-btn[title=\"Find in document\"]")).ToBeEnabledAsync(new() { Timeout = DefaultTimeout });
        await Settle();
    }

    private async Task OpenPdfFind()
    {
        await WaitForPdf();

        await Page.Locator("button.bit-pdv-btn[title=\"Find in document\"]").ClickAsync();
        var input = Page.Locator(".bit-pdv-search-input");
        await Expect(input).ToHaveCountAsync(1);
        await input.FocusAsync();
        await Settle();
    }

    private async Task OpenRteMenu(string trigger)
    {
        var editor = Page.Locator(".bit-rte-edt");
        await editor.ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync(trigger);

        var menu = Page.Locator(".bit-rte-slash");
        await Expect(menu).ToHaveCountAsync(1);
        await Expect(menu.Locator("input.bit-rte-inp")).ToBeFocusedAsync();
        await Settle();
    }

    private async Task OpenPage(string surface, string part)
    {
        await Page.GotoAsync($"{BaseUrl}/regression/escape-claim-extras?surface={surface}&part={part}");
        await WaitForStatus("Ready");
        await Settle();
    }

    private async Task OpenSurface(string surface, string part)
    {
        await OpenPage(surface, part);

        if (surface is "none") return;

        await Page.Locator("#btn-open").ClickAsync();
        await WaitForStatus("Open");
        await Expect(Page.Locator("#surface-content")).ToBeVisibleAsync();
        await Settle();
    }

    // The components start listening in calls to the browser of their own once they have rendered, which nothing on
    // the page reports; a key pressed before they land is a key nothing was listening for yet.
    private Task Settle() => Page.WaitForTimeoutAsync(500);

    private async Task FocusAndType(string selector, string text)
    {
        var input = Page.Locator(selector);
        await input.FillAsync(text);
        await Expect(input).ToBeFocusedAsync();
    }

    // On the page itself (?surface=none) there is no surface to stay open, so only the press itself is checked.
    private async Task PressEscapeAndExpectOpen()
    {
        await Page.Keyboard.PressAsync("Escape");

        // Give a wrongly routed press the time to have closed the surface.
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#surface-state")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("^(open|none)$"));
    }

    private async Task PressEscapeAndExpectClosed()
    {
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#surface-state")).ToHaveTextAsync("closed");
    }
}
