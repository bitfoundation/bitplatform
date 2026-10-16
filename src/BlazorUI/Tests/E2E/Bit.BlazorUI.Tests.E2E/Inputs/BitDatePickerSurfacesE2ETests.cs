namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// BitDatePicker inside the surfaces that close on Escape (/e2e/date-picker/surfaces): in a BitDialog, a BitModal and
/// a BitPanel the first Escape closes only the picker's callout - from the day grid, and from an editable field that
/// kept the focus - and hands the focus back to the field inside the surface, which stays open and closes on the next
/// press. A pick inside the dialog works like anywhere else.
/// </summary>
[TestClass]
public class BitDatePickerSurfacesE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-picker/surfaces";

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtp-inp");

    [TestMethod]
    public async Task Dialog_TheFirstEscapeClosesOnlyThePickerAndTheNextOneTheDialog()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await SettleAsync(Page.Locator(".bit-dlg"));
        await Page.WaitForFunctionAsync("() => document.querySelector('.bit-dlg')?.contains(document.activeElement)");
        await Expect(Input("dp-dialog")).ToBeVisibleAsync();

        var pid = await PickerIdAsync("dp-dialog");
        await Input("dp-dialog").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("dp-dialog")).ToBeFocusedAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Input("dp-dialog")).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
        await Expect(Input("dp-dialog")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Dialog_AnEditableFieldThatKeptTheFocusClosesItsCalloutFirstToo()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await SettleAsync(Page.Locator(".bit-dlg"));
        await Page.WaitForFunctionAsync("() => document.querySelector('.bit-dlg')?.contains(document.activeElement)");

        var pid = await PickerIdAsync("dp-dialog-text");
        await Input("dp-dialog-text").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Input("dp-dialog-text")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Dialog_APickInsideTheDialogSetsTheValueAndLeavesTheDialogOpen()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await SettleAsync(Page.Locator(".bit-dlg"));
        await Page.WaitForFunctionAsync("() => document.querySelector('.bit-dlg')?.contains(document.activeElement)");

        var pid = await PickerIdAsync("dp-dialog");
        await Input("dp-dialog").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await SettleAsync(Callout(pid));

        // The callout floats over the dialog, placed against the field inside it.
        var field = await BoxAsync(Page.Locator("#dp-dialog .bit-dtp-wrp"));
        var callout = await BoxAsync(Callout(pid));
        var where = $"field {field.X},{field.Y} {field.Width}x{field.Height}; callout {callout.X},{callout.Y} {callout.Width}x{callout.Height}";
        Assert.AreEqual(field.X, callout.X, 2, where);
        Assert.IsTrue(Math.Abs(callout.Y - (field.Y + field.Height + 1)) <= 2 || Math.Abs((callout.Y + callout.Height) - (field.Y - 1)) <= 2,
                      $"The callout touches the field, below or above it: {where}");

        await Day(pid, 2024, 6, 21).ClickAsync();

        await Expect(Page.Locator("#dialog-value")).ToHaveTextAsync("2024-06-21 00:00");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Input("dp-dialog")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Modal_TheFirstEscapeClosesOnlyThePicker()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-modal").ClickAsync();
        await SettleAsync(Page.Locator(".bit-mdl"));

        var pid = await PickerIdAsync("dp-modal");
        await Input("dp-modal").ClickAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("True");
        await Expect(Input("dp-modal")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Panel_TheFirstEscapeClosesOnlyThePicker()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-panel").ClickAsync();
        await SettleAsync(Page.Locator(".bit-pnl-cnt"));

        var pid = await PickerIdAsync("dp-panel");
        await Input("dp-panel").ClickAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#panel-open")).ToHaveTextAsync("True");
        await Expect(Input("dp-panel")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#panel-open")).ToHaveTextAsync("False");
    }
}
