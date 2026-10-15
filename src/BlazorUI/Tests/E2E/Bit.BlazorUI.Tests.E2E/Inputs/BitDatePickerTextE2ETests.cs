namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// BitDatePicker's text in a real browser (/e2e/date-picker/text): DateFormat, AllowTextInput (an editable field that
/// keeps the focus when it opens, the format as its accessible description, typed text moving the calendar, a time
/// typed in either clock format, a typed month), the custom and the default messages of an unreadable, out-of-range
/// and disabled date inside an EditForm, the en-US, de-DE and fa-IR (Persian calendar, right to left) cultures, and a
/// TimeZone of the picker's own.
/// </summary>
[TestClass]
public class BitDatePickerTextE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-picker/text";

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtp-inp");

    private static ILocator Title(ILocator callout) => callout.Locator(".bit-dtp-dwp .bit-dtp-pkh").First.Locator(".bit-dtp-pkt, .bit-dtp-ptb").First;

    [TestMethod]
    public async Task DateFormat_TheValueIsWrittenWithIt()
    {
        await OpenAsync(Route);

        await Expect(Input("dp-format")).ToHaveValueAsync("20=06(24)");

        var pid = await PickerIdAsync("dp-format");
        await Input("dp-format").ClickAsync();
        await Day(pid, 2024, 6, 3).ClickAsync();

        await Expect(Input("dp-format")).ToHaveValueAsync("03=06(24)");
        await Expect(Page.Locator("#format-value")).ToHaveTextAsync("2024-06-03 00:00");
    }

    [TestMethod]
    public async Task TextInput_AnEditableFieldKeepsTheFocusAndIsDescribedByItsFormat()
    {
        await OpenAsync(Route);

        var input = Input("dp-text");
        var pid = await PickerIdAsync("dp-text");

        await Expect(input).Not.ToHaveAttributeAsync("readonly", "");
        await Expect(input).ToHaveClassAsync(HasClass("bit-dtp-ein"));
        await Expect(input).ToHaveAttributeAsync("placeholder", "dd/MM/yyyy");
        await Expect(input).ToHaveAttributeAsync("aria-describedby", $"{pid}-format");
        await Expect(Page.Locator($"#{pid}-format")).ToHaveTextAsync("Write it as dd/MM/yyyy");

        await input.ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(input).ToBeFocusedAsync();

        // Typed while open, the calendar follows the text.
        await input.FillAsync("15/08/2025");
        await Expect(Page.Locator("#text-value")).ToHaveTextAsync("2025-08-15 00:00");
        await Expect(Title(Callout(pid))).ToHaveTextAsync("August 2025");
        await Expect(Day(pid, 2025, 8, 15)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Callout(pid).Locator(".bit-dtp-mwp .bit-dtp-ptb")).ToHaveTextAsync("2025");
    }

    [TestMethod]
    public async Task TextInput_EnterIsLeftToTheTextAndArrowDownOpensIntoTheGrid()
    {
        await OpenAsync(Route);

        var input = Input("dp-text");
        var pid = await PickerIdAsync("dp-text");

        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await input.FillAsync("01/06/2024");
        await Expect(Page.Locator("#text-value")).ToHaveTextAsync("2024-06-01 00:00");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Day(pid, 2024, 6, 1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TextInput_EachKindOfBadTextFailsWithItsOwnMessage()
    {
        await OpenAsync(Route);

        var input = Input("dp-text-msg");
        var message = Page.Locator("#msg-message");

        await input.FillAsync("abc");
        await Expect(message).ToHaveTextAsync("The date format must be dd/MM/yyyy.");
        await Expect(input).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#dp-text-msg")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(input).ToHaveValueAsync("abc");

        await input.FillAsync("01/01/2030");
        await Expect(message).ToHaveTextAsync("Only the days around today are allowed.");

        await input.FillAsync("15/06/2024");
        await Expect(message).ToHaveTextAsync("That day of the week is not available.");
        await Expect(Page.Locator("#msg-value")).ToHaveTextAsync("null");

        await input.FillAsync("13/06/2024");
        await Expect(message).ToHaveTextAsync("");
        await Expect(Page.Locator("#msg-value")).ToHaveTextAsync("2024-06-13 00:00");
        await Expect(input).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [TestMethod]
    public async Task TextInput_TheDefaultMessagesNameTheField()
    {
        await OpenAsync(Route);

        var input = Input("dp-text-default");
        var message = Page.Locator("#default-message");

        await input.FillAsync("not a date");
        await Expect(message).ToHaveTextAsync("The Appointment field is not valid.");

        await input.FillAsync("5/1/2024");
        await Expect(message).ToHaveTextAsync("The Appointment field is out of the allowed range.");

        await input.FillAsync("6/14/2024");
        await Expect(message).ToHaveTextAsync("The Appointment field is not an allowed date.");

        await input.FillAsync("7/4/2024");
        await Expect(message).ToHaveTextAsync("");
        await Expect(Page.Locator("#default-value")).ToHaveTextAsync("2024-07-04 00:00");
    }

    [TestMethod]
    public async Task TextInput_NoValidateLeavesUnreadableTextUnreported()
    {
        await OpenAsync(Route);

        var input = Input("dp-novalidate");

        await input.FillAsync("7/4/2024");
        await Expect(Page.Locator("#novalidate-value")).ToHaveTextAsync("2024-07-04 00:00");

        await input.FillAsync("not a date");
        await input.BlurAsync();
        await Expect(Page.Locator("#novalidate-message")).ToHaveTextAsync("");
        await Expect(input).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#dp-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task TextInput_ATimeTypedInEitherClockOrLeftOutIsReadAndRewritten()
    {
        await OpenAsync(Route);

        var input = Input("dp-text-time");

        await input.FillAsync("6/15/2024 14:45");
        await Expect(Page.Locator("#time-text-value")).ToHaveTextAsync("2024-06-15 14:45");

        await input.FillAsync("6/16/2024 2:45 PM");
        await Expect(Page.Locator("#time-text-value")).ToHaveTextAsync("2024-06-16 14:45");
        await input.BlurAsync();
        await Expect(input).ToHaveValueAsync("6/16/2024 14:45");

        // A day without a time keeps the time the picker shows.
        await input.FillAsync("6/17/2024");
        await Expect(Page.Locator("#time-text-value")).ToHaveTextAsync("2024-06-17 14:45");
    }

    [TestMethod]
    public async Task TextInput_ATypedMonthSelectsItsFirstDay()
    {
        await OpenAsync(Route);

        await Input("dp-month-text").FillAsync("August 2024");
        await Expect(Page.Locator("#month-text-value")).ToHaveTextAsync("2024-08-01 00:00");
    }

    [TestMethod]
    public async Task Culture_GermanFormatsNamesAndFirstDayOfTheWeek()
    {
        await OpenAsync(Route);

        var input = Input("dp-de");
        var pid = await PickerIdAsync("dp-de");

        await input.ScrollIntoViewIfNeededAsync();
        await input.FillAsync("15.06.2024");
        await Expect(Page.Locator("#de-value")).ToHaveTextAsync("2024-06-15 00:00");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("Juni 2024");
        await Expect(Callout(pid).Locator(".bit-dtp-dgh [role=columnheader]").First).ToHaveAttributeAsync("aria-label", "Montag");
        await Expect(Day(pid, 2024, 6, 15)).ToHaveAttributeAsync("aria-label", "Samstag, 15. Juni 2024");
        await Expect(Day(pid, 2024, 6, 15)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(pid, 2024, 6, 16)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(input).ToHaveValueAsync("16.06.2024");
    }

    [TestMethod]
    public async Task Culture_PersianCalendarReadsWritesAndLaysOutRightToLeft()
    {
        // The ICU data of .NET's WebAssembly runtime carries no fa locale - neither the sharded set it loads by default nor
        // the full one - so under WebAssembly fa-IR has no Farsi names and reads left to right whatever the component does.

        await OpenAsync(Route);

        var root = Page.Locator("#dp-fa");
        var input = Input("dp-fa");
        var pid = await PickerIdAsync("dp-fa");

        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

        await Page.Locator("#btn-fa-set").ClickAsync();
        await Expect(input).ToHaveValueAsync("1403/03/26");

        await input.ScrollIntoViewIfNeededAsync();
        await input.FillAsync("1403/04/10");
        await Expect(Page.Locator("#fa-value")).ToHaveTextAsync("2024-06-30 00:00");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Callout(pid)).ToHaveClassAsync(HasClass("bit-dtp-rtl"));
        await Expect(Callout(pid)).ToHaveCSSAsync("direction", "rtl");
        await Expect(Title(Callout(pid))).ToHaveTextAsync(IsWasm ? "Tir 1403" : "تیر 1403");
        await Expect(Callout(pid).Locator(".bit-dtp-gtb[title='برو به امروز']")).ToHaveCountAsync(1);
        await Expect(Day(pid, 2024, 6, 30)).ToBeFocusedAsync();
        await Expect(Day(pid, 2024, 6, 30)).ToHaveTextAsync("10");

        // Right to left, the arrow pointing left moves on to the next day.
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day(pid, 2024, 7, 1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(input).ToHaveValueAsync("1403/04/11");
        await Expect(Page.Locator("#fa-value")).ToHaveTextAsync("2024-07-01 00:00");
    }

    [TestMethod]
    public async Task TimeZone_TheValueIsReadAndWrittenInTheZoneOfThePicker()
    {
        await OpenAsync(Route);

        var input = Input("dp-tz");
        var pid = await PickerIdAsync("dp-tz");

        await input.ClickAsync();
        await Day(pid, 2024, 6, 20).ClickAsync();
        await Expect(Page.Locator("#tz-value")).ToHaveTextAsync("2024-06-20 00:00 +09:00");

        await Page.Keyboard.PressAsync("Escape");
        await Page.Locator("#btn-tz-set").ClickAsync();
        await Expect(input).ToHaveValueAsync("6/21/2024 8:00");

        await input.ClickAsync();
        await Expect(Day(pid, 2024, 6, 21)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Callout(pid).Locator(".bit-dtp-tin[aria-label='Hour']")).ToHaveValueAsync("8");
    }
}
