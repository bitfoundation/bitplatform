namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitPopConfirm at /e2e/pop-confirm in a real browser: the popup opened from its trigger as a named and described
/// alertdialog, with the focus on Cancel and kept inside it; answered with Confirm, Cancel, Escape or a click outside of
/// it, each once and each handing the focus back to the trigger; opened and answered from the keyboard alone; and a
/// disabled one that never opens while its trigger keeps working.
/// </summary>
[TestClass]
public class BitPopConfirmE2ETests : E2ETestBase
{
    private const string Route = "/e2e/pop-confirm";

    private ILocator Trigger => Page.Locator("#pcf-trigger");

    private ILocator Popup => Page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete the file?" });

    private ILocator Cancel => Popup.GetByRole(AriaRole.Button, new() { Name = "Cancel" });

    private ILocator Confirm => Popup.GetByRole(AriaRole.Button, new() { Name = "Delete" });

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private async Task OpenPopupAsync()
    {
        await Trigger.ClickAsync();

        await Expect(Popup).ToBeVisibleAsync();
        await Expect(Cancel).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TheTrigger_OpensANamedAlertDialog_WithTheFocusOnCancel_KeptInsideIt()
    {
        await OpenAsync(Route);

        await OpenPopupAsync();

        await Expect(Text("pcf-open")).ToHaveTextAsync("True");
        await Expect(Popup).ToHaveAccessibleDescriptionAsync("It cannot be brought back.");
        await Expect(Popup).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(Trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await AssertNoAxeViolationsAsync("[role=alertdialog][aria-labelledby='pcf-ttl']");

        // Tab and Shift+Tab go round the two buttons, never out to the page behind the popup.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Confirm).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Cancel).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Confirm).ToBeFocusedAsync();

        await Expect(Text("pcf-answers")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Confirm_AnswersYes_AndHandsTheFocusBackToTheTrigger()
    {
        await OpenAsync(Route);
        await OpenPopupAsync();

        await Confirm.ClickAsync();

        await Expect(Popup).ToBeHiddenAsync();
        await Expect(Text("pcf-answers")).ToHaveTextAsync("confirm");
        await Expect(Text("pcf-open")).ToHaveTextAsync("False");
        await Expect(Trigger).ToBeFocusedAsync();
        await Expect(Trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Cancel_AnswersNo_AndHandsTheFocusBackToTheTrigger()
    {
        await OpenAsync(Route);
        await OpenPopupAsync();

        await Cancel.ClickAsync();

        await Expect(Popup).ToBeHiddenAsync();
        await Expect(Text("pcf-answers")).ToHaveTextAsync("cancel");
        await Expect(Trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Escape_AnswersNo_AndHandsTheFocusBackToTheTrigger()
    {
        await OpenAsync(Route);
        await OpenPopupAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Popup).ToBeHiddenAsync();
        await Expect(Text("pcf-answers")).ToHaveTextAsync("cancel");
        await Expect(Trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AClickOutsideOfThePopup_AnswersNo_WithoutReachingThePage()
    {
        await OpenAsync(Route);

        // The overlay covers the page button once the popup is open, which is the point: the click is aimed at where
        // the button is, and lands on the overlay instead.
        var button = (await Text("btn-page").BoundingBoxAsync())!;

        await OpenPopupAsync();

        await Page.Mouse.ClickAsync(button.X + button.Width / 2, button.Y + button.Height / 2);

        await Expect(Popup).ToBeHiddenAsync();
        await Expect(Text("pcf-answers")).ToHaveTextAsync("cancel");
        await Expect(Text("pcf-page-clicks")).ToHaveTextAsync("0");
        await Expect(Trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TheKeyboardAlone_OpensThePopup_AndAnswersEachOpeningOnce()
    {
        await OpenAsync(Route);

        await Trigger.FocusAsync();

        // Enter on the trigger asks, and an Enter right after it lands on Cancel, not on Delete.
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Popup).ToBeVisibleAsync();
        await Expect(Cancel).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Popup).ToBeHiddenAsync();
        await Expect(Trigger).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Space");
        await Expect(Cancel).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Confirm).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Popup).ToBeHiddenAsync();
        await Expect(Text("pcf-answers")).ToHaveTextAsync("cancel,confirm");
        await Expect(Trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task ADisabledPopConfirm_NeverOpens_AndLeavesItsTriggerWorking()
    {
        await OpenAsync(Route);

        var trigger = Page.Locator("#pcf-dis-trigger");

        await trigger.ClickAsync();
        await trigger.ClickAsync();

        await Expect(Text("pcf-dis-clicks")).ToHaveTextAsync("2");
        await Expect(Page.GetByRole(AriaRole.Alertdialog, new() { Name = "Never asked" })).ToBeHiddenAsync();
        await Expect(trigger).ToHaveCSSAsync("pointer-events", "auto");
    }
}
