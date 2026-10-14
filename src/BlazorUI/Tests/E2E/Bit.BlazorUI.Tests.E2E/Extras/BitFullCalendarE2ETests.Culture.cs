using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Cultures and directions of BitFullCalendar (/e2e/full-calendar/culture): Persian (fa-IR, the Persian calendar, right to
/// left, translated through Texts), Arabic (ar-SA, the Hijri calendar), an explicit Dir="BitDir.Rtl", the culture deciding
/// the direction when Dir is unset whatever the page's, and Dir="BitDir.Auto" - with the layout, the chevrons and the
/// arrow keys following the reading direction.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private const string CulturePage = "/e2e/full-calendar/culture";

    private static async Task AssertRightToLeftLayoutAsync(ILocator calendar)
    {
        await Expect(calendar).ToHaveAttributeAsync("dir", "rtl");
        await Expect(calendar).ToHaveClassAsync(HasClass("bit-bfc-rtl"));
        Assert.AreEqual("rtl", await calendar.EvaluateAsync<string>("e => getComputedStyle(e).direction"));

        // the first weekday sits on the right, the next button on the left and its chevron mirrored
        var headers = calendar.Locator(".bit-bfc-month-header-cell");
        var first = (await headers.First.BoundingBoxAsync())!;
        var last = (await headers.Last.BoundingBoxAsync())!;
        Assert.IsTrue(first.X > last.X, "the week runs from the right");
        var previous = (await calendar.Locator(".bit-bfc-date-nav .bit-bfc-btn-nav").First.BoundingBoxAsync())!;
        var next = (await calendar.Locator(".bit-bfc-date-nav .bit-bfc-btn-nav").Last.BoundingBoxAsync())!;
        Assert.IsTrue(next.X < previous.X, "next is on the left");
        Assert.AreEqual("matrix(-1, 0, 0, 1, 0, 0)", await calendar.Locator(".bit-bfc-date-nav .bit-bfc-btn-nav svg").First.EvaluateAsync<string>("e => getComputedStyle(e).transform"));
    }

    [TestMethod]
    public async Task Culture_Persian_RendersThePersianCalendarRightToLeft_InItsOwnWords()
    {
        await OpenAsync(CulturePage);
        var cal = Page.Locator("#fa-cal");

        await AssertRightToLeftLayoutAsync(cal);
        await Expect(Title(cal)).ToHaveTextAsync("1404 تیر");
        await Expect(cal.Locator(".bit-bfc-view-tab")).ToHaveTextAsync(["روز", "هفته", "ماه", "سال", "برنامه"]);
        await Expect(cal.Locator(".bit-bfc-mode-tab")).ToHaveTextAsync(["رویدادها", "خط زمانی"]);
        await Expect(cal.Locator(".bit-bfc-header-left > .bit-bfc-btn").First).ToHaveTextAsync("امروز");
        await Expect(cal.Locator(".bit-bfc-month-header-cell").First).ToHaveTextAsync("ش");
        await Expect(cal.Locator(".bit-bfc-month-cell-day.today")).ToHaveTextAsync("25");
        // the month is Tir: June 22 to July 22
        await Expect(Page.Locator("#fa-range")).ToHaveTextAsync("2025-06-22|2025-07-22");

        await cal.Locator(".bit-bfc-btn-nav").Last.ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("1404 مرداد");
        await Expect(Page.Locator("#fa-range")).ToHaveTextAsync("2025-07-23|2025-08-22");
    }

    [TestMethod]
    public async Task Culture_Persian_TheArrowKeysFollowTheReadingDirection()
    {
        await OpenAsync(CulturePage);
        var cal = Page.Locator("#fa-cal");
        var stop = cal.Locator(".bit-bfc-month-grid [data-bit-bfc-roving][tabindex='0']");
        await Expect(stop).ToHaveAttributeAsync("id", new Regex("-20250622$"));
        await stop.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Page.Locator(":focus")).ToHaveAttributeAsync("id", new Regex("-20250623$"));
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Page.Locator(":focus")).ToHaveAttributeAsync("id", new Regex("-20250624$"));
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Page.Locator(":focus")).ToHaveAttributeAsync("id", new Regex("-20250623$"));

        await cal.Locator(".bit-bfc-view-tab.active").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(cal.Locator(".bit-bfc-view-tab.active")).ToHaveTextAsync("سال");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(cal.Locator(".bit-bfc-view-tab.active")).ToHaveTextAsync("ماه");
    }

    [TestMethod]
    public async Task Culture_Persian_TheWeekTheDialogAndTheTimeline_ReadRightToLeft()
    {
        await OpenAsync(CulturePage);
        var cal = Page.Locator("#fa-cal");

        await SwitchViewAsync(cal, "هفته");
        await Expect(cal.Locator(".bit-bfc-week-header-day-num")).ToHaveTextAsync(["21", "22", "23", "24", "25", "26", "27"]);
        await Expect(cal.Locator(".bit-bfc-week-header-day-num.today")).ToHaveTextAsync("25");
        var firstDay = (await cal.Locator(".bit-bfc-week-header-day").First.BoundingBoxAsync())!;
        var lastDay = (await cal.Locator(".bit-bfc-week-header-day").Last.BoundingBoxAsync())!;
        Assert.IsTrue(firstDay.X > lastDay.X, "the week runs from the right");

        // Alt+ArrowLeft moves an event to the next day of a right-to-left week
        await Block(cal, "f1").FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowLeft");
        await Expect(Block(cal, "f1")).ToBeFocusedAsync();
        // the Persian week runs Saturday to Friday: Wednesday is its fifth column, Thursday its sixth
        await Expect(cal.Locator(".bit-bfc-week-day-col").Nth(5).Locator("[data-bit-bfc-event='f1']")).ToBeAttachedAsync();
        var moved = (await Block(cal, "f1").BoundingBoxAsync())!;
        var todayColumn = (await cal.Locator(".bit-bfc-today-col").BoundingBoxAsync())!;
        Assert.IsTrue(moved.X < todayColumn.X, "the event moved one column to the left, to the next day");

        await cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(cal);
        await Expect(dialog.Locator("h3")).ToHaveTextAsync("افزودن رویداد جدید");
        await Expect(dialog.GetByPlaceholder("عنوان رویداد")).ToBeVisibleAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "ایجاد رویداد" })).ToBeVisibleAsync();
        Assert.AreEqual("rtl", await dialog.EvaluateAsync<string>("e => getComputedStyle(e).direction"));
        await dialog.GetByRole(AriaRole.Button, new() { Name = "انصراف" }).ClickAsync();

        await cal.Locator(".bit-bfc-mode-tab", new() { HasText = "خط زمانی" }).ClickAsync();
        var gutter = (await cal.Locator(".bit-bfc-tl-resource-cell").First.BoundingBoxAsync())!;
        var row = (await cal.Locator(".bit-bfc-tl-scroll").BoundingBoxAsync())!;
        Assert.IsTrue(gutter.X + gutter.Width > row.X + row.Width - 2, "the resource gutter sits on the right");
    }

    [TestMethod]
    public async Task Culture_Arabic_RendersTheHijriCalendarRightToLeft()
    {
        await OpenAsync(CulturePage);
        var cal = Page.Locator("#ar-cal");

        await AssertRightToLeftLayoutAsync(cal);
        await Expect(Title(cal)).ToHaveTextAsync("محرم 1447 بعد الهجرة");
        await Expect(cal.Locator(".bit-bfc-month-cell-day.today")).ToHaveTextAsync("21");
        await Expect(Badge(cal, "s1")).ToBeVisibleAsync();

        await cal.Locator(".bit-bfc-btn-nav").Last.ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("صفر 1447 بعد الهجرة");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_FlipsAnEnglishCalendar_AndItsKeys()
    {
        await OpenAsync(CulturePage);
        var cal = Page.Locator("#rtl-cal");

        await AssertRightToLeftLayoutAsync(cal);
        await Expect(cal).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Title(cal)).ToHaveTextAsync("July 2025");

        var stop = cal.Locator(".bit-bfc-month-grid [data-bit-bfc-roving][tabindex='0']");
        await stop.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocusedLabelAsync("Add event, Wednesday, July 2, 2025");

        await SwitchViewAsync(cal, "Week");
        await Block(cal, "r1").FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowLeft");
        await Expect(Block(cal, "r1")).ToBeFocusedAsync();
        await Expect(cal.Locator(".bit-bfc-week-day-col").Nth(4).Locator("[data-bit-bfc-event='r1']")).ToBeAttachedAsync();

        await cal.Locator(".bit-bfc-mode-tab", new() { HasText = "Timeline" }).ClickAsync();
        var gutter = (await cal.Locator(".bit-bfc-tl-resource-cell").First.BoundingBoxAsync())!;
        var scroller = (await cal.Locator(".bit-bfc-tl-scroll").BoundingBoxAsync())!;
        Assert.IsTrue(gutter.X + gutter.Width > scroller.X + scroller.Width - 2, "the resource gutter sits on the right");
    }

    [TestMethod]
    public async Task Rtl_WithDirUnset_TheCultureDecidesTheDirection_WhateverThePages()
    {
        await OpenAsync(CulturePage);

        // English in a right-to-left page stays left to right; Persian in a left-to-right page is right to left
        var english = Page.Locator("#inherited-en-cal");
        await Expect(english).ToHaveAttributeAsync("dir", "ltr");
        Assert.AreEqual("ltr", await english.EvaluateAsync<string>("e => getComputedStyle(e).direction"));
        var first = (await english.Locator(".bit-bfc-month-header-cell").First.BoundingBoxAsync())!;
        var last = (await english.Locator(".bit-bfc-month-header-cell").Last.BoundingBoxAsync())!;
        Assert.IsTrue(first.X < last.X);

        await AssertRightToLeftLayoutAsync(Page.Locator("#inherited-fa-cal"));
    }

    [TestMethod]
    public async Task Rtl_DirAuto_LeavesTheDirectionToTheBrowser()
    {
        await OpenAsync(CulturePage);
        var cal = Page.Locator("#auto-cal");

        await Expect(cal).ToHaveAttributeAsync("dir", "auto");
        Assert.AreEqual("rtl", await cal.EvaluateAsync<string>("e => getComputedStyle(e).direction"));
        await Expect(cal).ToHaveClassAsync(HasClass("bit-bfc-rtl"));
        await Expect(cal.Locator(".bit-bfc-view-tab.active")).ToHaveTextAsync("ماه");
    }
}
