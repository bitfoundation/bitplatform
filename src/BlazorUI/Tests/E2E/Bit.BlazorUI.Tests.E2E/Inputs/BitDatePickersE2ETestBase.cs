using System.Globalization;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// What the tests of BitDatePicker and BitDateRangePicker share: both render their callout beside their root and
/// relocate it to the body while it is open, so a test reaches a callout, a day, a month or a year cell through the
/// ids the picker derives from its own internal id (read off the input it renders), never through the root.
/// </summary>
public abstract class BitDatePickersE2ETestBase : E2ETestBase
{
    /// <summary>
    /// The "today" every picker of the date picker pages is pinned to: Wednesday, June 12, 2024.
    /// </summary>
    protected static readonly DateTime Today = new(2024, 6, 12);

    /// <summary>
    /// The internal id of the picker whose root has the given id (DatePicker-xxxx or DateRangePicker-xxxx), which
    /// every part outside of the root - the callout, the overlay, the cells - is named after.
    /// </summary>
    protected async Task<string> PickerIdAsync(string rootId)
    {
        var inputId = await Page.Locator($"#{rootId} input").First.GetAttributeAsync("id");

        Assert.IsNotNull(inputId, $"The picker #{rootId} renders no input with an id.");

        return inputId![..^"-input".Length];
    }

    protected ILocator Callout(string pickerId) => Page.Locator($"#{pickerId}-callout");

    protected ILocator Overlay(string pickerId) => Page.Locator($"#{pickerId}-overlay");

    protected ILocator Day(string pickerId, DateTime date) => Page.Locator($"#{pickerId}-day-{date:yyyy-MM-dd}");

    protected ILocator Day(string pickerId, int year, int month, int day) => Day(pickerId, new DateTime(year, month, day));

    protected ILocator MonthCell(string pickerId, int month) => Page.Locator($"#{pickerId}-month-{month:D2}");

    protected ILocator YearCell(string pickerId, int year) => Page.Locator($"#{pickerId}-year-{year:D4}");

    /// <summary>
    /// Waits until the callout of every picker of the page is closed - its overlay gone - so a press that closed one
    /// has been handled before the next picker is reached for.
    /// </summary>
    protected Task NoCalloutOpenAsync()
    {
        return Expect(Page.Locator(".bit-dtp-ovl:visible, .bit-dtrp-ovl:visible")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Waits until no animation of the element or of anything inside it is running any more - the entry animation
    /// of a callout slides it into place, so it is measured once it has arrived there.
    /// </summary>
    protected async Task SettleAsync(ILocator locator)
    {
        await Expect(locator).ToBeVisibleAsync();

        var handle = await locator.ElementHandleAsync();

        await Page.WaitForFunctionAsync("el => el.getAnimations({ subtree: true }).every(a => a.playState !== 'running')", handle);
    }

    /// <summary>
    /// The bounding box of the element, failing the test when it has none (not rendered).
    /// </summary>
    protected static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();

        Assert.IsNotNull(box, "The element has no bounding box.");

        return box!;
    }

    /// <summary>
    /// Reads a computed style of the element the locator finds.
    /// </summary>
    protected static Task<string> StyleOfAsync(ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");
    }

    /// <summary>
    /// The computed value of a color declared through a custom property, resolved the way an element would.
    /// </summary>
    protected Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(@"color => {
            const probe = document.createElement('div');
            probe.style.backgroundColor = color;
            document.body.appendChild(probe);
            const value = getComputedStyle(probe).backgroundColor;
            probe.remove();
            return value;
        }", cssColor);
    }

    /// <summary>
    /// Whether the element currently holding the focus is inside the given element.
    /// </summary>
    protected Task<bool> FocusIsInsideAsync(string selector)
    {
        return Page.EvaluateAsync<bool>("sel => { const host = document.querySelector(sel); return !!host && host.contains(document.activeElement); }", selector);
    }

    /// <summary>
    /// The ISO-ish week number .NET gives the date under the rule and first day the picker is configured with.
    /// </summary>
    protected static int WeekOfYear(DateTime date, CalendarWeekRule rule, DayOfWeek firstDay)
    {
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, rule, firstDay);
    }
}
