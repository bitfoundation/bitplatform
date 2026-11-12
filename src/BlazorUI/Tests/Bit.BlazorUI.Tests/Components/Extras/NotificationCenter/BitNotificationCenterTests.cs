using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.NotificationCenter;

/// <summary>
/// BitNotificationCenter: a bell whose badge counts the notifications of BitSnackBarService.History not read yet, opening
/// a panel (the dialog of a BitDropMenu) that lists them newest first; opening it reads them.
/// </summary>
[TestClass]
public class BitNotificationCenterTests : BunitTestContext
{
    private BitSnackBarService RegisterService(int historyLimit = 0)
    {
        var service = new BitSnackBarService { HistoryLimit = historyLimit };
        Services.AddSingleton(service);
        RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));
        return service;
    }

    private static IElement Bell(IRenderedComponent<BitNotificationCenter> center) => center.Find(".bit-drm-btn");

    private static string[] ItemTitles(IRenderedComponent<BitNotificationCenter> center)
        => [.. center.FindAll(".bit-ntc-itm").Select(i => i.QuerySelector(".bit-ntc-itt")!.TextContent.Trim())];

    [TestMethod]
    public void BitNotificationCenterShouldBeABellThatOpensADialog()
    {
        RegisterService();
        var center = RenderComponent<BitNotificationCenter>();

        var bell = Bell(center);
        Assert.AreEqual("Notifications", bell.GetAttribute("aria-label"));
        Assert.AreEqual("dialog", bell.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", bell.GetAttribute("aria-expanded"));
        Assert.IsNotNull(bell.QuerySelector(".bit-ntc-icn.bit-icon--Ringer"));
        Assert.IsNull(bell.QuerySelector(".bit-ntc-bdg"), "nothing is unread");
        Assert.IsNull(bell.QuerySelector(".bit-drm-chv"), "a bell has no chevron");

        Assert.AreEqual("No notifications", center.Find(".bit-ntc-emp").TextContent.Trim());
        Assert.IsEmpty(center.FindAll(".bit-ntc-clr"), "there is nothing to clear");
    }

    [TestMethod]
    [DataRow(0, 50, 50, DisplayName = "Off: raised to the default")]
    [DataRow(10, 50, 50, DisplayName = "Lower: raised")]
    [DataRow(100, 50, 100, DisplayName = "Higher: kept")]
    [DataRow(0, 5, 5, DisplayName = "A limit of its own")]
    public void BitNotificationCenterShouldMakeTheServiceKeepItsHistory(int serviceLimit, int centerLimit, int expected)
    {
        var service = RegisterService(serviceLimit);

        RenderComponent<BitNotificationCenter>(parameters => parameters.Add(p => p.HistoryLimit, centerLimit));

        Assert.AreEqual(expected, service.HistoryLimit);
    }

    [TestMethod]
    public void BitNotificationCenterShouldRenderAnEmptyBellWithoutTheServices()
    {
        var center = RenderComponent<BitNotificationCenter>();

        Assert.AreEqual(0, center.Instance.UnreadCount);
        Assert.AreEqual("Notifications", Bell(center).GetAttribute("aria-label"));
        Assert.HasCount(1, center.FindAll(".bit-ntc-emp"));
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldCountWhatIsUnreadOnItsBadgeAndInTheBellsName()
    {
        var service = RegisterService();
        var center = RenderComponent<BitNotificationCenter>();

        await service.Info("first");
        await service.Error("second", "The server did not answer.");

        center.WaitForAssertion(() => Assert.AreEqual("2", center.Find(".bit-ntc-bdg").TextContent.Trim()));
        Assert.AreEqual("true", center.Find(".bit-ntc-bdg").GetAttribute("aria-hidden"), "the count is in the bell's name");
        Assert.AreEqual("Notifications, 2 unread", Bell(center).GetAttribute("aria-label"));
        Assert.AreEqual(2, center.Instance.UnreadCount);

        // Newest first, each drawn the way the snack bar drew it.
        CollectionAssert.AreEqual(new[] { "second", "first" }, ItemTitles(center));
        var error = center.FindAll(".bit-ntc-itm")[0];
        Assert.IsTrue(error.ClassList.Contains("bit-ntc-err"));
        Assert.IsNotNull(error.QuerySelector(".bit-ntc-iic.bit-icon--ErrorBadge"));
        Assert.AreEqual("The server did not answer.", error.QuerySelector(".bit-ntc-itb")!.TextContent.Trim());
        Assert.IsNull(center.FindAll(".bit-ntc-itm")[1].QuerySelector(".bit-ntc-itb"), "no body, no line for it");
        Assert.AreEqual("Notifications", center.Find(".bit-ntc-lst").GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldShowWhenEachArrived()
    {
        var service = RegisterService();
        var center = RenderComponent<BitNotificationCenter>(parameters => parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc));

        var item = await service.Info("first");

        var time = center.WaitForElement(".bit-ntc-tim");
        var shownAt = item.ShownAt!.Value;
        Assert.AreEqual("TIME", time.TagName);
        Assert.AreEqual(shownAt.ToString("O", CultureInfo.InvariantCulture), time.GetAttribute("datetime"));
        Assert.AreEqual(shownAt.UtcDateTime.ToString("t", CultureInfo.CurrentCulture), time.TextContent.Trim());
    }

    // Opening reads what was unread, which stays marked while the panel is open - as does what arrives meanwhile - and
    // is just read the next time.
    [TestMethod]
    public async Task BitNotificationCenterShouldReadWhatItListsWhenItOpens()
    {
        var service = RegisterService();
        var center = RenderComponent<BitNotificationCenter>();
        await service.Info("first");
        await service.Info("second");
        center.WaitForAssertion(() => Assert.AreEqual(2, center.Instance.UnreadCount));

        Bell(center).Click();

        Assert.AreEqual(0, center.Instance.UnreadCount);
        Assert.IsEmpty(center.FindAll(".bit-ntc-bdg"));
        Assert.AreEqual("Notifications", Bell(center).GetAttribute("aria-label"));
        Assert.HasCount(2, center.FindAll(".bit-ntc-itm.bit-ntc-new"));
        Assert.AreEqual("Unread:", center.Find(".bit-ntc-itm .bit-ntc-sro").TextContent.Trim());

        await service.Info("third");
        center.WaitForAssertion(() => Assert.HasCount(3, center.FindAll(".bit-ntc-itm.bit-ntc-new")));
        Assert.AreEqual(0, center.Instance.UnreadCount, "what arrives while it is open is read as it is listed");

        Bell(center).Click();
        Bell(center).Click();
        Assert.IsEmpty(center.FindAll(".bit-ntc-new"));
        Assert.IsEmpty(center.FindAll(".bit-ntc-sro"));
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldOpenAndReadThroughIsOpen()
    {
        var service = RegisterService();
        var opened = new System.Collections.Generic.List<bool>();
        var center = RenderComponent<BitNotificationCenter>(parameters =>
            parameters.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, opened.Add)));
        await service.Info("first");
        center.WaitForAssertion(() => Assert.AreEqual(1, center.Instance.UnreadCount));

        center.Render(parameters => parameters.Add(p => p.IsOpen, true));
        Assert.AreEqual(0, center.Instance.UnreadCount);
        Assert.AreEqual("true", Bell(center).GetAttribute("aria-expanded"));
        Assert.HasCount(1, center.FindAll(".bit-ntc-new"));

        Bell(center).Click();
        CollectionAssert.AreEqual(new[] { false }, opened);
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldMarkAllAsReadWithoutOpening()
    {
        var service = RegisterService();
        var center = RenderComponent<BitNotificationCenter>();
        await service.Info("first");
        center.WaitForAssertion(() => Assert.AreEqual(1, center.Instance.UnreadCount));

        await center.InvokeAsync(center.Instance.MarkAllAsRead);

        Assert.AreEqual(0, center.Instance.UnreadCount);
        Assert.IsEmpty(center.FindAll(".bit-ntc-bdg"));
        Bell(center).Click();
        Assert.IsEmpty(center.FindAll(".bit-ntc-new"), "nothing was unread when it opened");
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldClearTheHistory()
    {
        var service = RegisterService();
        var center = RenderComponent<BitNotificationCenter>();
        await service.Info("first");

        center.WaitForElement(".bit-ntc-clr").Click();

        Assert.IsEmpty(service.History);
        center.WaitForAssertion(() => Assert.HasCount(1, center.FindAll(".bit-ntc-emp")));
        Assert.IsEmpty(center.FindAll(".bit-ntc-clr"));
        Assert.AreEqual(0, center.Instance.UnreadCount);
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldShowNoTimeForAQueuedNotification()
    {
        var service = new BitSnackBarService { HistoryLimit = 10 };
        Services.AddSingleton(service);
        RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.OverflowBehavior, BitSnackBarOverflowBehavior.Queue);
        });
        var center = RenderComponent<BitNotificationCenter>();

        await service.Info("shown");
        await service.Info("queued");

        center.WaitForAssertion(() => Assert.HasCount(2, center.FindAll(".bit-ntc-itm")));
        Assert.IsNull(center.FindAll(".bit-ntc-itm")[0].QuerySelector(".bit-ntc-tim"), "it has not arrived yet");
        Assert.IsNotNull(center.FindAll(".bit-ntc-itm")[1].QuerySelector(".bit-ntc-tim"));
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldCapItsBadge()
    {
        var service = RegisterService(historyLimit: 150);
        var center = RenderComponent<BitNotificationCenter>();

        for (var i = 0; i < 100; i++) await service.Info($"#{i}");

        center.WaitForAssertion(() => Assert.AreEqual("99+", center.Find(".bit-ntc-bdg").TextContent.Trim()));
        Assert.AreEqual("Notifications, 100 unread", Bell(center).GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldTakeItsWords()
    {
        var service = RegisterService();
        var center = RenderComponent<BitNotificationCenter>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Inbox");
            parameters.Add(p => p.UnreadAriaLabelFormat, "{0} new");
            parameters.Add(p => p.HeaderText, "Your inbox");
            parameters.Add(p => p.EmptyText, "All caught up");
            parameters.Add(p => p.ClearAllText, "Empty it");
            parameters.Add(p => p.UnreadItemText, "New");
        });
        Assert.AreEqual("All caught up", center.Find(".bit-ntc-emp").TextContent.Trim());

        await service.Info("first");

        center.WaitForAssertion(() => Assert.AreEqual("Inbox, 1 new", Bell(center).GetAttribute("aria-label")));
        Assert.AreEqual("Your inbox", center.Find(".bit-ntc-htx").TextContent.Trim());
        Assert.AreEqual("Your inbox", center.Find(".bit-ntc-lst").GetAttribute("aria-label"));
        Assert.AreEqual("Empty it", center.Find(".bit-ntc-clr").TextContent.Trim());
        Bell(center).Click();
        Assert.AreEqual("New:", center.Find(".bit-ntc-sro").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldDrawEachNotificationThroughItsTemplate()
    {
        var service = RegisterService();
        var center = RenderComponent<BitNotificationCenter>(parameters =>
            parameters.Add(p => p.ItemTemplate, (RenderFragment<BitSnackBarItem>)(item => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom");
                builder.AddContent(2, $"{item.Title}!");
                builder.CloseElement();
            })));

        await service.Info("first");

        center.WaitForAssertion(() => Assert.AreEqual("first!", center.Find(".bit-ntc-itm .custom").TextContent));
        Assert.IsNull(center.Find(".bit-ntc-itm").QuerySelector(".bit-ntc-itt"));
    }

    [TestMethod]
    public async Task BitNotificationCenterShouldHandItsClassesAndStylesToItsParts()
    {
        var service = RegisterService();
        var center = RenderComponent<BitNotificationCenter>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitNotificationCenterClassStyles { Root = "c-root", Button = "c-btn", Badge = "c-bdg", Callout = "c-cal", Item = "c-itm", UnreadItem = "c-new" });
            parameters.Add(p => p.Styles, new BitNotificationCenterClassStyles { Root = "margin:1px", Badge = "color:red", Item = "padding:1px", UnreadItem = "padding:2px" });
        });
        await service.Info("first");
        center.WaitForAssertion(() => Assert.HasCount(1, center.FindAll(".bit-ntc-bdg")));

        Assert.IsTrue(center.Find(".bit-ntc").ClassList.Contains("c-root"));
        StringAssert.Contains(center.Find(".bit-ntc").GetAttribute("style"), "margin:1px");
        Assert.IsTrue(Bell(center).ClassList.Contains("c-btn"));
        Assert.IsTrue(Bell(center).ClassList.Contains("bit-ntc-btn"));
        Assert.IsTrue(center.Find(".bit-ntc-bdg").ClassList.Contains("c-bdg"));
        Assert.AreEqual("color:red", center.Find(".bit-ntc-bdg").GetAttribute("style"));
        Assert.IsTrue(center.Find(".bit-drm-cal").ClassList.Contains("c-cal"));
        Assert.IsTrue(center.Find(".bit-drm-cal").ClassList.Contains("bit-ntc-cal"));

        var item = center.Find(".bit-ntc-itm");
        Assert.IsTrue(item.ClassList.Contains("c-itm"));
        Assert.IsFalse(item.ClassList.Contains("c-new"));
        Assert.AreEqual("padding:1px", item.GetAttribute("style"));

        Bell(center).Click();
        item = center.Find(".bit-ntc-itm");
        Assert.IsTrue(item.ClassList.Contains("c-new"));
        Assert.AreEqual("padding:1px;padding:2px", item.GetAttribute("style"));
    }

    // The service outlives the center (a scoped or singleton service, a center on one page), so the center stops
    // listening to it as it goes rather than being kept alive by it.
    [TestMethod]
    public async Task BitNotificationCenterShouldLetGoOfTheServiceWhenDisposed()
    {
        var service = RegisterService();
        var center = RenderComponent<BitNotificationCenter>();
        Assert.AreEqual(1, HistoryListeners(service));

        await center.InvokeAsync(() => center.Instance.DisposeAsync().AsTask());

        Assert.AreEqual(0, HistoryListeners(service));
    }

    private static int HistoryListeners(BitSnackBarService service)
    {
        var field = typeof(BitSnackBarService).GetField(nameof(BitSnackBarService.OnHistoryChange), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (field.GetValue(service) as Delegate)?.GetInvocationList().Length ?? 0;
    }
}
