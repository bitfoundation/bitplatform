namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// BitDateRangePicker inside the surfaces that close on Escape (/e2e/date-range-picker/surfaces): in a BitDialog, a
/// BitModal and a BitPanel the first Escape closes only the picker's callout - rolling back a range nobody applied -
/// and the surface closes on the next press; a range picked inside the dialog works like anywhere else.
/// </summary>
[TestClass]
public class BitDateRangePickerSurfacesE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-range-picker/surfaces";

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtrp-inp");

    [TestMethod]
    public async Task Dialog_TheFirstEscapeClosesOnlyThePickerAndTheNextOneTheDialog()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await SettleAsync(Page.Locator(".bit-dlg"));
        await Page.WaitForFunctionAsync("() => document.querySelector('.bit-dlg')?.contains(document.activeElement)");

        var pid = await PickerIdAsync("drp-dialog");
        await Input("drp-dialog").ClickAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Input("drp-dialog")).ToBeFocusedAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Dialog_ARangePickedInsideItIsKeptAndAnUnappliedOneRolledBackByEscape()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await SettleAsync(Page.Locator(".bit-dlg"));
        await Page.WaitForFunctionAsync("() => document.querySelector('.bit-dlg')?.contains(document.activeElement)");

        var pid = await PickerIdAsync("drp-dialog");
        await Input("drp-dialog").ClickAsync();
        await SettleAsync(Callout(pid));

        var field = await BoxAsync(Page.Locator("#drp-dialog .bit-dtrp-wrp"));
        var callout = await BoxAsync(Callout(pid));
        Assert.AreEqual(field.X, callout.X, 2);

        await Day(pid, 2024, 6, 10).ClickAsync();
        await Day(pid, 2024, 6, 12).ClickAsync();
        await Expect(Page.Locator("#dialog-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-12 23:59");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        var apply = await PickerIdAsync("drp-dialog-apply");
        await Input("drp-dialog-apply").ClickAsync();
        await SettleAsync(Callout(apply));
        await Day(apply, 2024, 6, 10).ClickAsync();
        await Day(apply, 2024, 6, 12).ClickAsync();
        await Expect(Page.Locator("#apply-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-12 23:59");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(apply)).ToBeHiddenAsync();
        await Expect(Page.Locator("#apply-value")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Modal_TheFirstEscapeClosesOnlyThePicker()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-modal").ClickAsync();
        await SettleAsync(Page.Locator(".bit-mdl"));

        var pid = await PickerIdAsync("drp-modal");
        await Input("drp-modal").ClickAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("True");
        await Expect(Input("drp-modal")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#modal-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Panel_TheFirstEscapeClosesOnlyThePicker()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-panel").ClickAsync();
        await SettleAsync(Page.Locator(".bit-pnl-cnt"));

        var pid = await PickerIdAsync("drp-panel");
        await Input("drp-panel").ClickAsync();
        await Expect(Day(pid, Today)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Callout(pid)).ToBeHiddenAsync();
        await Expect(Page.Locator("#panel-open")).ToHaveTextAsync("True");
        await Expect(Input("drp-panel")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#panel-open")).ToHaveTextAsync("False");
    }
}
