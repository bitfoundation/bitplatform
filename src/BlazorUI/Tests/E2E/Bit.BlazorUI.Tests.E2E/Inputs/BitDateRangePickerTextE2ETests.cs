namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// BitDateRangePicker's text in a real browser (/e2e/date-range-picker/text): DateFormat, ValueFormat, NoDateText and
/// the default format with a time; a typed range (an open end, a reversed pair, a range that moves the open calendar);
/// the custom and the default messages of an unreadable range, a blocked day and a range too long inside an EditForm;
/// the de-DE and the fa-IR (Persian calendar, right to left) cultures; and a TimeZone of the picker's own.
/// </summary>
[TestClass]
public class BitDateRangePickerTextE2ETests : BitDatePickersE2ETestBase
{
    private const string Route = "/e2e/date-range-picker/text";

    private ILocator Input(string rootId) => Page.Locator($"#{rootId} .bit-dtrp-inp");

    private static ILocator Title(ILocator callout) => callout.Locator(".bit-dtrp-dwp .bit-dtrp-pkh").First.Locator(".bit-dtrp-pkt, .bit-dtrp-ptb").First;

    private async Task TypeAsync(string rootId, string text)
    {
        await Input(rootId).FillAsync(text);
        await Input(rootId).BlurAsync();
    }

    [TestMethod]
    public async Task Formats_DateFormatValueFormatNoDateTextAndTheTime()
    {
        await OpenAsync(Route);

        await Expect(Input("drp-fmt")).ToHaveValueAsync("Start: 10=06(24) - End: 14=06(24)");
        await Expect(Input("drp-valuefmt")).ToHaveValueAsync("Dep: 6/10/2024, Arr: 6/14/2024");
        await Expect(Input("drp-nodate")).ToHaveValueAsync("6/10/2024 - open ended");
        await Expect(Input("drp-timefmt")).ToHaveValueAsync("Start: 6/10/2024 8:30 - End: 6/14/2024 17:45");
    }

    [TestMethod]
    public async Task TextInput_ATypedRangeIsReadOpenEndedOrReversed()
    {
        await OpenAsync(Route);

        var input = Input("drp-text");

        await Expect(input).Not.ToHaveAttributeAsync("readonly", "");
        await Expect(input).ToHaveClassAsync(HasClass("bit-dtrp-ein"));
        await Expect(input).ToHaveAttributeAsync("placeholder", "dd/MM/yyyy - dd/MM/yyyy");

        await TypeAsync("drp-text", "10/06/2024 - 14/06/2024");
        await Expect(Page.Locator("#text-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-14 00:00");

        await TypeAsync("drp-text", "--- - 20/06/2024");
        await Expect(Page.Locator("#text-value")).ToHaveTextAsync("- / 2024-06-20 00:00");
        await Expect(input).ToHaveValueAsync("--- - 20/06/2024");

        await TypeAsync("drp-text", "14/07/2024 - 10/07/2024");
        await Expect(Page.Locator("#text-value")).ToHaveTextAsync("2024-07-10 00:00 / 2024-07-14 00:00");
        await Expect(input).ToHaveValueAsync("10/07/2024 - 14/07/2024");
    }

    [TestMethod]
    public async Task TextInput_AnEditableFieldKeepsTheFocusAndTheCalendarFollowsTheText()
    {
        await OpenAsync(Route);

        var input = Input("drp-text");
        var pid = await PickerIdAsync("drp-text");

        await input.ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(input).ToBeFocusedAsync();

        await input.FillAsync("10/08/2025 - 12/08/2025");
        await input.DispatchEventAsync("change");

        await Expect(Page.Locator("#text-value")).ToHaveTextAsync("2025-08-10 00:00 / 2025-08-12 00:00");
        await Expect(Title(Callout(pid))).ToHaveTextAsync("August 2025");
        await Expect(Day(pid, 2025, 8, 10)).ToHaveClassAsync(HasClass("bit-dtrp-dss"));
    }

    [TestMethod]
    public async Task TextInput_EachKindOfBadRangeFailsWithItsOwnMessage()
    {
        await OpenAsync(Route);

        var message = Page.Locator("#msg-message");

        await TypeAsync("drp-text-msg", "abc");
        await Expect(message).ToHaveTextAsync("Write the range as dd/MM/yyyy - dd/MM/yyyy.");
        await Expect(Page.Locator("#drp-text-msg")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Input("drp-text-msg")).ToHaveAttributeAsync("aria-invalid", "true");

        await TypeAsync("drp-text-msg", "15/06/2024 - 17/06/2024");
        await Expect(message).ToHaveTextAsync("Weekends are not bookable.");

        await TypeAsync("drp-text-msg", "03/06/2024 - 14/06/2024");
        await Expect(message).ToHaveTextAsync("A booking cannot be longer than a week.");
        await Expect(Page.Locator("#msg-value")).ToHaveTextAsync("null");

        await TypeAsync("drp-text-msg", "10/06/2024 - 12/06/2024");
        await Expect(message).ToHaveTextAsync("");
        await Expect(Page.Locator("#msg-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-12 00:00");
        await Expect(Page.Locator("#drp-text-msg")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task TextInput_TheDefaultMessagesNameTheField()
    {
        await OpenAsync(Route);

        var message = Page.Locator("#default-message");

        await TypeAsync("drp-text-default", "abc");
        await Expect(message).ToHaveTextAsync("The Stay field is not valid.");

        await TypeAsync("drp-text-default", "20/05/2024 - 22/05/2024");
        await Expect(message).ToHaveTextAsync("The Stay field is out of the allowed range.");

        await TypeAsync("drp-text-default", "14/06/2024 - 16/06/2024");
        await Expect(message).ToHaveTextAsync("The Stay field contains a date that is not available.");
    }

    [TestMethod]
    public async Task TextInput_NoValidateLeavesUnreadableTextUnreported()
    {
        await OpenAsync(Route);

        await TypeAsync("drp-novalidate", "10/06/2024 - 12/06/2024");
        await Expect(Page.Locator("#novalidate-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-12 00:00");

        await TypeAsync("drp-novalidate", "not a range");
        await Expect(Page.Locator("#novalidate-message")).ToHaveTextAsync("");
        await Expect(Input("drp-novalidate")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#drp-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));
    }

    [TestMethod]
    public async Task Culture_GermanFormatsAndNames()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-de");

        await TypeAsync("drp-de", "10.06.2024 - 14.06.2024");
        await Expect(Page.Locator("#de-value")).ToHaveTextAsync("2024-06-10 00:00 / 2024-06-14 00:00");

        await Input("drp-de").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Title(Callout(pid))).ToHaveTextAsync("Juni 2024");
        await Expect(Callout(pid).Locator(".bit-dtrp-dgh [role=columnheader]").First).ToHaveAttributeAsync("aria-label", "Montag");
        await Expect(Day(pid, 2024, 6, 10)).ToHaveAttributeAsync("aria-label", "Montag, 10. Juni 2024");
    }

    [TestMethod]
    public async Task Culture_PersianCalendarWritesTheRangeAndLaysOutRightToLeft()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#drp-fa");
        var pid = await PickerIdAsync("drp-fa");

        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));

        await Page.Locator("#btn-fa-set").ClickAsync();
        await Expect(Input("drp-fa")).ToHaveValueAsync("شروع: 1403/03/21, پایان: 1403/03/25");

        await Input("drp-fa").ClickAsync();
        await Expect(Callout(pid)).ToBeVisibleAsync();
        await Expect(Callout(pid)).ToHaveClassAsync(HasClass("bit-dtrp-rtl"));
        await Expect(Callout(pid)).ToHaveCSSAsync("direction", "rtl");
        await Expect(Title(Callout(pid))).ToHaveTextAsync("خرداد 1403");
        await Expect(Day(pid, 2024, 6, 10)).ToHaveTextAsync("21");
        await Expect(Callout(pid).Locator(".bit-dtrp-gtb[title='برو به امروز']")).ToHaveCountAsync(1);

        // The focus lands on the start of the range, and the arrow pointing left moves on to the next day.
        await Expect(Day(pid, 2024, 6, 10)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day(pid, 2024, 6, 11)).ToBeFocusedAsync();

        var first = await BoxAsync(Day(pid, 2024, 6, 10));
        var next = await BoxAsync(Day(pid, 2024, 6, 11));
        Assert.IsTrue(next.X < first.X, "The days of a week run from right to left.");
    }

    [TestMethod]
    public async Task TimeZone_TheRangeIsProducedInTheZoneOfThePicker()
    {
        await OpenAsync(Route);

        var pid = await PickerIdAsync("drp-tz");

        await Input("drp-tz").ClickAsync();
        await Day(pid, 2024, 6, 10).ClickAsync();
        await Day(pid, 2024, 6, 14).ClickAsync();

        await Expect(Page.Locator("#tz-value")).ToHaveTextAsync("2024-06-10 00:00 +09:00 / 2024-06-14 23:59 +09:00");
    }
}
