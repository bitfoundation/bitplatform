using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;
using Bunit.Extensions;

namespace Bit.BlazorUI.Tests.Components.Notifications.SnackBar;

[TestClass]
public class BitSnackBarTests : BunitTestContext
{
    [TestMethod,
         DataRow(BitSnackBarPosition.TopStart),
         DataRow(BitSnackBarPosition.TopCenter),
         DataRow(BitSnackBarPosition.TopEnd),
         DataRow(BitSnackBarPosition.BottomStart),
         DataRow(BitSnackBarPosition.BottomCenter),
         DataRow(BitSnackBarPosition.BottomEnd),
         DataRow(null)
    ]
    public void BitSnackBarPositionTest(BitSnackBarPosition? position)
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            if (position.HasValue) parameters.Add(p => p.Position, position.Value);
        });

        var element = com.Find(".bit-snb");

        var positionClass = position switch
        {
            BitSnackBarPosition.TopStart => "bit-snb-tst",
            BitSnackBarPosition.TopCenter => "bit-snb-tcn",
            BitSnackBarPosition.TopEnd => "bit-snb-ten",
            BitSnackBarPosition.BottomStart => "bit-snb-bst",
            BitSnackBarPosition.BottomCenter => "bit-snb-bcn",
            BitSnackBarPosition.BottomEnd => "bit-snb-ben",
            _ => "bit-snb-ben",
        };

        Assert.IsTrue(element.ClassList.Contains(positionClass));
    }

    [TestMethod,
         DataRow("title", "body"),
         DataRow("title", "")
    ]
    public async Task BitSnackBarShowTest(string title, string body)
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show(title, body);

        var item = com.Find(".bit-snb-itm");
        Assert.IsNotNull(item);

        var titleElement = com.Find(".bit-snb-ttl");
        Assert.AreEqual(title, titleElement.InnerHtml);

        if (string.IsNullOrEmpty(body) is false)
        {
            var bodyElement = com.Find(".bit-snb-bdy");
            Assert.AreEqual(body, bodyElement.InnerHtml);
        }
        else
        {
            Assert.AreEqual(0, com.FindAll(".bit-snb-bdy").Count);
        }
    }

    [TestMethod]
    public async Task BitSnackBarShowReturnsTheShownItemTest()
    {
        var com = RenderComponent<BitSnackBar>();

        var item = await com.Instance.Show("title", "body", BitColor.Success);

        Assert.AreEqual("title", item.Title);
        Assert.AreEqual("body", item.Body);
        Assert.AreEqual(BitColor.Success, item.Color);
        Assert.AreEqual(1, com.Instance.Items.Count);
        Assert.AreSame(item, com.Instance.Items[0]);
    }

    [TestMethod]
    public async Task BitSnackBarShowingTheSameItemTwiceIsNoOpTest()
    {
        var com = RenderComponent<BitSnackBar>();

        var item = new BitSnackBarItem { Title = "title" };

        await com.Instance.Show(item);
        var again = await com.Instance.Show(item);

        Assert.AreSame(item, again);
        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod,
         DataRow(true),
         DataRow(false)
    ]
    public async Task BitSnackBarAutoDismissTest(bool autoDismiss)
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(p => p.AutoDismiss, autoDismiss);
                parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
                parameters.Add(p => p.TransitionDuration, 0);
            }
        );

        await com.Instance.Show("title");

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);

        if (autoDismiss)
        {
            com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count), TimeSpan.FromSeconds(5));
        }
        else
        {
            await Task.Delay(TimeSpan.FromMilliseconds(600));
            Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
        }
    }

    [TestMethod]
    public async Task BitSnackBarAutoDismissTimeOfZeroKeepsTheItemTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.Zero);
        });

        await com.Instance.Show("title");

        await Task.Delay(TimeSpan.FromMilliseconds(300));

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
        Assert.AreEqual(0, com.FindAll(".bit-snb-prb").Count);
    }

    [TestMethod]
    public async Task BitSnackBarPerItemAutoDismissTimeTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMinutes(5));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("long lived");
        await com.Instance.Show("short lived", autoDismissTime: TimeSpan.FromMilliseconds(300));

        Assert.AreEqual(2, com.FindAll(".bit-snb-itm").Count);

        com.WaitForAssertion(() => Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count), TimeSpan.FromSeconds(5));

        Assert.AreEqual("long lived", com.Instance.Items[0].Title);
    }

    [TestMethod]
    public async Task BitSnackBarProgressBarDurationTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromSeconds(7));
        });

        await com.Instance.Show("title");

        var progress = com.Find(".bit-snb-prb");

        Assert.IsTrue(progress.GetAttribute("style")!.Contains("animation-duration:7s"));
        Assert.AreEqual("true", progress.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public async Task BitSnackBarHideProgressTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMinutes(1));
            parameters.Add(p => p.HideProgress, true);
        });

        await com.Instance.Show("title");

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
        Assert.AreEqual(0, com.FindAll(".bit-snb-prb").Count);
    }

    [TestMethod]
    public async Task BitSnackBarPersistentItemHasNoProgressBarTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
        });

        await com.Instance.Show("title", persistent: true);

        Assert.AreEqual(0, com.FindAll(".bit-snb-prb").Count);

        await Task.Delay(TimeSpan.FromMilliseconds(600));

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod,
         DataRow("title", BitColor.Primary, "bit-snb-pri"),
         DataRow("title", BitColor.Secondary, "bit-snb-sec"),
         DataRow("title", BitColor.Tertiary, "bit-snb-ter"),
         DataRow("title", BitColor.Info, "bit-snb-inf"),
         DataRow("title", BitColor.Success, "bit-snb-suc"),
         DataRow("title", BitColor.Warning, "bit-snb-wrn"),
         DataRow("title", BitColor.SevereWarning, "bit-snb-swr"),
         DataRow("title", BitColor.Error, "bit-snb-err"),
         DataRow("title", BitColor.PrimaryBackground, "bit-snb-pbg"),
         DataRow("title", BitColor.SecondaryBackground, "bit-snb-sbg"),
         DataRow("title", BitColor.TertiaryBackground, "bit-snb-tbg"),
         DataRow("title", BitColor.PrimaryForeground, "bit-snb-pfg"),
         DataRow("title", BitColor.SecondaryForeground, "bit-snb-sfg"),
         DataRow("title", BitColor.TertiaryForeground, "bit-snb-tfg"),
         DataRow("title", BitColor.PrimaryBorder, "bit-snb-pbr"),
         DataRow("title", BitColor.SecondaryBorder, "bit-snb-sbr"),
         DataRow("title", BitColor.TertiaryBorder, "bit-snb-tbr"),
         DataRow("title", null, "bit-snb-inf")
    ]
    public async Task BitColorTest(string title, BitColor? color, string colorClass)
    {
        var com = RenderComponent<BitSnackBar>();

        if (color.HasValue)
        {
            await com.Instance.Show(title, color: color.Value);
        }
        else
        {
            await com.Instance.Show(new BitSnackBarItem { Title = title });
        }

        var element = com.Find(".bit-snb-itm");

        Assert.IsTrue(element.ClassList.Contains(colorClass));
    }

    [TestMethod,
         DataRow(BitVariant.Fill, "bit-snb-fil"),
         DataRow(BitVariant.Outline, "bit-snb-otl"),
         DataRow(BitVariant.Text, "bit-snb-txt"),
         DataRow(null, "bit-snb-fil")
    ]
    public async Task BitSnackBarVariantTest(BitVariant? variant, string variantClass)
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            if (variant.HasValue) parameters.Add(p => p.Variant, variant.Value);
        });

        await com.Instance.Show("title");

        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains(variantClass));
    }

    [TestMethod,
         DataRow(BitSize.Small, "bit-snb-sm"),
         DataRow(BitSize.Medium, "bit-snb-md"),
         DataRow(BitSize.Large, "bit-snb-lg"),
         DataRow(null, "bit-snb-md")
    ]
    public async Task BitSnackBarSizeTest(BitSize? size, string sizeClass)
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            if (size.HasValue) parameters.Add(p => p.Size, size.Value);
        });

        await com.Instance.Show("title");

        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains(sizeClass));
    }

    [TestMethod,
        DataRow("title")
    ]
    public async Task BitSnackBarCloseButtonTest(string title)
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        await com.Instance.Show(title);

        var closeButton = com.Find(".bit-snb-cbt");

        var itemsBeforeClose = com.FindAll(".bit-snb-itm");
        Assert.AreEqual(1, itemsBeforeClose.Count);

        closeButton.Click();

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count));
    }

    [TestMethod]
    public async Task BitSnackBarCloseMethodTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        var item = await com.Instance.Show("title");

        await com.Instance.Close(item);

        Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count);
        Assert.AreEqual(0, com.Instance.Items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarClearTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("first");
        await com.Instance.Show("second");
        await com.Instance.Show("third");

        Assert.AreEqual(3, com.FindAll(".bit-snb-itm").Count);

        await com.Instance.Clear();

        Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count);
        Assert.AreEqual(0, com.Instance.Items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarUpdateTest()
    {
        var com = RenderComponent<BitSnackBar>();

        var item = await com.Instance.Show("Uploading...", "file.txt", BitColor.Info);

        item.Title = "Upload complete";
        item.Color = BitColor.Success;

        await com.Instance.Update(item);

        Assert.AreEqual("Upload complete", com.Find(".bit-snb-ttl").InnerHtml);
        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-suc"));
    }

    [TestMethod]
    public async Task BitSnackBarUpdateRestartsTheProgressBarTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMinutes(1));
        });

        var item = await com.Instance.Show("title");

        var before = com.Find(".bit-snb-prb");

        await com.Instance.Update(item);

        var after = com.Find(".bit-snb-prb");

        // A CSS animation does not start over because its element was re-rendered, so the bar has to be a new
        // element for the restarted countdown to be drawn from the beginning again.
        Assert.IsFalse(ReferenceEquals(before, after));
    }

    [TestMethod]
    public async Task BitSnackBarUpdateClearsThePausedStateTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMinutes(1));
        });

        var item = await com.Instance.Show("title");

        await com.Instance.Pause(item);

        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-pau"));

        await com.Instance.Update(item);

        Assert.IsFalse(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-pau"));
    }

    [TestMethod]
    public async Task BitSnackBarUpdateOfAnUnknownItemIsNoOpTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Update(new BitSnackBarItem { Title = "nothing" });

        Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public async Task BitSnackBarTransitionDurationKeepsTheItemWhileItLeavesTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 400));

        var item = await com.Instance.Show("title");

        var closing = com.Instance.Close(item);

        // The item is still in the DOM, marked as on its way out, until the exit animation has played.
        com.WaitForAssertion(() => Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-dsm")));

        await closing;

        Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public void BitSnackBarTransitionDurationTokenTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 350));

        Assert.IsTrue(com.Find(".bit-snb").GetAttribute("style")!.Contains("--bit-snb-dur-full:350ms"));
    }

    [TestMethod,
         DataRow(true),
         DataRow(false)
    ]
    public async Task BitSnackBarPersistentTest(bool persistent)
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Persistent, persistent));

        await com.Instance.Show("title");

        Assert.AreEqual(persistent ? 0 : 1, com.FindAll(".bit-snb-cbt").Count);
    }

    [TestMethod]
    public async Task BitSnackBarPerItemPersistentTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("dismissible");
        await com.Instance.Show("persistent", persistent: true);

        var items = com.FindAll(".bit-snb-itm");

        Assert.AreEqual(2, items.Count);
        Assert.AreEqual(1, items[0].QuerySelectorAll(".bit-snb-cbt").Length);
        Assert.AreEqual(0, items[1].QuerySelectorAll(".bit-snb-cbt").Length);
    }

    [TestMethod]
    public async Task BitSnackBarMaxItemsTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 2);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("first");
        await com.Instance.Show("second");
        await com.Instance.Show("third");

        Assert.AreEqual(2, com.FindAll(".bit-snb-itm").Count);
        Assert.AreEqual("second", com.Instance.Items[0].Title);
        Assert.AreEqual("third", com.Instance.Items[1].Title);
    }

    [TestMethod]
    public async Task BitSnackBarMaxItemsWithNewestOnTopDropsTheOldestTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 2);
            parameters.Add(p => p.NewestOnTop, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("first");
        await com.Instance.Show("second");
        await com.Instance.Show("third");

        Assert.AreEqual(2, com.Instance.Items.Count);
        Assert.AreEqual("third", com.Instance.Items[0].Title);
        Assert.AreEqual("second", com.Instance.Items[1].Title);
    }

    [TestMethod,
         DataRow(0),
         DataRow(-1)
    ]
    public async Task BitSnackBarNonPositiveMaxItemsMeansNoCapTest(int maxItems)
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.MaxItems, maxItems));

        await com.Instance.Show("first");
        await com.Instance.Show("second");
        await com.Instance.Show("third");

        Assert.AreEqual(3, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public async Task BitSnackBarNewestOnTopTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.NewestOnTop, true));

        await com.Instance.Show("first");
        await com.Instance.Show("second");

        var titles = com.FindAll(".bit-snb-ttl").Select(t => t.InnerHtml).ToArray();

        Assert.AreEqual("second", titles[0]);
        Assert.AreEqual("first", titles[1]);
    }

    [TestMethod]
    public async Task BitSnackBarPreventDuplicatesTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.PreventDuplicates, true));

        var first = await com.Instance.Show("title", "body", BitColor.Info);
        var second = await com.Instance.Show("title", "body", BitColor.Info);

        Assert.AreSame(first, second);
        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public async Task BitSnackBarPreventDuplicatesAllowsDifferentItemsTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.PreventDuplicates, true));

        await com.Instance.Show("title", "body", BitColor.Info);
        await com.Instance.Show("title", "body", BitColor.Error);
        await com.Instance.Show("title", "other body", BitColor.Info);

        Assert.AreEqual(3, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public async Task BitSnackBarDuplicatesAreAllowedByDefaultTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title", "body");
        await com.Instance.Show("title", "body");

        Assert.AreEqual(2, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public async Task BitSnackBarOnShowAndOnDismissTest()
    {
        BitSnackBarItem? shown = null;
        BitSnackBarItem? dismissed = null;

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.TransitionDuration, 0);
            parameters.Add(p => p.OnShow, (BitSnackBarItem i) => shown = i);
            parameters.Add(p => p.OnDismiss, (BitSnackBarItem i) => dismissed = i);
        });

        var item = await com.Instance.Show("title");

        Assert.AreSame(item, shown);
        Assert.IsNull(dismissed);

        await com.Instance.Close(item);

        Assert.AreSame(item, dismissed);
    }

    [TestMethod]
    public async Task BitSnackBarItemOnDismissTest()
    {
        BitSnackBarItem? dismissed = null;

        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        var item = new BitSnackBarItem
        {
            Title = "title",
            OnDismiss = i => { dismissed = i; return Task.CompletedTask; }
        };

        await com.Instance.Show(item);
        await com.Instance.Close(item);

        Assert.AreSame(item, dismissed);
    }

    [TestMethod]
    public async Task BitSnackBarOnItemClickTest()
    {
        BitSnackBarItem? clicked = null;
        BitSnackBarItem? itemClicked = null;

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.TransitionDuration, 0);
            parameters.Add(p => p.OnItemClick, (BitSnackBarItem i) => clicked = i);
        });

        var item = new BitSnackBarItem
        {
            Title = "title",
            OnClick = i => { itemClicked = i; return Task.CompletedTask; }
        };

        await com.Instance.Show(item);

        com.Find(".bit-snb-itm").Click();

        Assert.AreSame(item, clicked);
        Assert.AreSame(item, itemClicked);

        // Without DismissOnClick a click leaves the item where it is.
        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public async Task BitSnackBarDismissOnClickTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.DismissOnClick, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-clk"));

        com.Find(".bit-snb-itm").Click();

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count));
    }

    [TestMethod]
    public async Task BitSnackBarDismissOnClickSkipsPersistentItemsTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.DismissOnClick, true);
            parameters.Add(p => p.Persistent, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        com.Find(".bit-snb-itm").Click();

        await Task.Delay(50);

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
        Assert.IsFalse(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-clk"));
    }

    [TestMethod]
    public async Task BitSnackBarEscapeKeyDismissesTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        await com.Instance.Show("title");

        com.Find(".bit-snb-itm").KeyDown("Escape");

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count));
    }

    [TestMethod]
    public async Task BitSnackBarEscapeKeyLeavesPersistentItemsTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.Persistent, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        com.Find(".bit-snb-itm").KeyDown("Escape");

        await Task.Delay(50);

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public async Task BitSnackBarOtherKeysDoNotDismissTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        await com.Instance.Show("title");

        com.Find(".bit-snb-itm").KeyDown("Enter");

        await Task.Delay(50);

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public async Task BitSnackBarPauseOnHoverTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        com.Find(".bit-snb-itm").MouseEnter();

        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-pau"));

        await Task.Delay(TimeSpan.FromMilliseconds(600));

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);

        com.Find(".bit-snb-itm").MouseLeave();

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarPauseOnHoverDisabledTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.PauseOnHover, false);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        com.Find(".bit-snb-itm").MouseEnter();

        Assert.IsFalse(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-pau"));

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarFocusPausesTheCountdownTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        com.Find(".bit-snb-itm").FocusIn();

        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-pau"));

        await Task.Delay(TimeSpan.FromMilliseconds(600));

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);

        com.Find(".bit-snb-itm").FocusOut();

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarPauseAndResumeMethodsTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        await com.Instance.Pause(item);

        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-pau"));

        await Task.Delay(TimeSpan.FromMilliseconds(600));

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);

        await com.Instance.Resume(item);

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarResumeDoesNotOverrideAHoverPauseTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        com.Find(".bit-snb-itm").MouseEnter();

        // A countdown is held back for as long as any one reason to hold it back stands.
        await com.Instance.Resume(item);

        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-pau"));

        await Task.Delay(TimeSpan.FromMilliseconds(600));

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);

        com.Find(".bit-snb-itm").MouseLeave();

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarUpdateOfAHoveredItemStartsPausedTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        com.Find(".bit-snb-itm").MouseEnter();

        await com.Instance.Update(item);

        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-pau"));

        await Task.Delay(TimeSpan.FromMilliseconds(600));

        Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public void BitSnackBarRootRegionTest()
    {
        var com = RenderComponent<BitSnackBar>();

        var root = com.Find(".bit-snb");

        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("Notifications", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSnackBarRootAriaLabelTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.AriaLabel, "Alerts"));

        Assert.AreEqual("Alerts", com.Find(".bit-snb").GetAttribute("aria-label"));
    }

    [TestMethod,
         DataRow(BitColor.Info, "status", "polite"),
         DataRow(BitColor.Success, "status", "polite"),
         DataRow(BitColor.Primary, "status", "polite"),
         DataRow(BitColor.Warning, "alert", "assertive"),
         DataRow(BitColor.SevereWarning, "alert", "assertive"),
         DataRow(BitColor.Error, "alert", "assertive")
    ]
    public async Task BitSnackBarItemLiveRegionTest(BitColor color, string role, string live)
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title", "body", color);

        var item = com.Find(".bit-snb-itm");

        // The item keeps the role its color implies, but the announcer speaks for it - so it is explicitly not a
        // live region of its own, or the message would be read twice.
        Assert.AreEqual(role, item.GetAttribute("role"));
        Assert.AreEqual("off", item.GetAttribute("aria-live"));
        Assert.IsFalse(item.HasAttribute("aria-atomic"));

        var announcer = com.FindAll(".bit-snb-ann")[live == "assertive" ? 1 : 0];

        Assert.AreEqual(live, announcer.GetAttribute("aria-live"));
        StringAssert.Contains(announcer.TextContent, "title. body");
    }

    [TestMethod]
    public async Task BitSnackBarRoleOverrideTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Role, "log"));

        await com.Instance.Show("title", "body", BitColor.Error);

        Assert.AreEqual("log", com.Find(".bit-snb-itm").GetAttribute("role"));
        Assert.AreEqual("off", com.Find(".bit-snb-itm").GetAttribute("aria-live"));

        // A log is polite, so an item that asks for one is announced by the polite region.
        StringAssert.Contains(com.FindAll(".bit-snb-ann")[0].TextContent, "title. body");
        Assert.AreEqual(string.Empty, com.FindAll(".bit-snb-ann")[1].TextContent);
    }

    [TestMethod]
    public async Task BitSnackBarItemRoleOverrideTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Role, "log"));

        await com.Instance.Show(new BitSnackBarItem { Title = "title", Color = BitColor.Info, Role = "alert" });

        Assert.AreEqual("alert", com.Find(".bit-snb-itm").GetAttribute("role"));
        Assert.AreEqual("off", com.Find(".bit-snb-itm").GetAttribute("aria-live"));

        StringAssert.Contains(com.FindAll(".bit-snb-ann")[1].TextContent, "title");
    }

    [TestMethod]
    public async Task BitSnackBarDismissButtonAccessibleNameTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title");

        var button = com.Find(".bit-snb-cbt");

        Assert.AreEqual("Close", button.GetAttribute("aria-label"));
        Assert.AreEqual("Close", button.GetAttribute("title"));
        Assert.AreEqual("button", button.GetAttribute("type"));
        Assert.AreEqual("true", com.Find(".bit-snb-cbt > i").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public async Task BitSnackBarDismissAriaLabelTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.DismissAriaLabel, "بستن"));

        await com.Instance.Show("title");

        Assert.AreEqual("بستن", com.Find(".bit-snb-cbt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitSnackBarDismissButtonComesAfterTheContentTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title");

        var header = com.Find(".bit-snb-hdr");

        // The notification is read before the button that throws it away, and tabbed to in the same order.
        Assert.IsTrue(header.Children[0].ClassList.Contains("bit-snb-ttl"));
        Assert.IsTrue(header.Children[^1].ClassList.Contains("bit-snb-cbt"));
    }

    [TestMethod,
         DataRow(BitColor.Info, "bit-icon--Info"),
         DataRow(BitColor.Success, "bit-icon--Completed"),
         DataRow(BitColor.Warning, "bit-icon--Info"),
         DataRow(BitColor.SevereWarning, "bit-icon--Warning"),
         DataRow(BitColor.Error, "bit-icon--ErrorBadge")
    ]
    public async Task BitSnackBarShowIconTest(BitColor color, string iconClass)
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ShowIcon, true));

        await com.Instance.Show("title", "body", color);

        var icon = com.Find(".bit-snb-ico");

        Assert.IsTrue(icon.ClassList.Contains(iconClass));
        Assert.AreEqual("true", com.Find(".bit-snb-ict").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public async Task BitSnackBarNoIconByDefaultTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title");

        Assert.AreEqual(0, com.FindAll(".bit-snb-ico").Count);
    }

    [TestMethod]
    public async Task BitSnackBarIconNameTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ShowIcon, true);
            parameters.Add(p => p.IconName, "Ringer");
        });

        await com.Instance.Show("title", "body", BitColor.Error);

        Assert.IsTrue(com.Find(".bit-snb-ico").ClassList.Contains("bit-icon--Ringer"));
    }

    [TestMethod]
    public async Task BitSnackBarIconWithCssClassesTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ShowIcon, true);
            parameters.Add(p => p.Icon, BitIconInfo.Fa("solid circle-info"));
        });

        await com.Instance.Show("title");

        var icon = com.Find(".bit-snb-ico");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-circle-info"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon"));
    }

    [TestMethod]
    public async Task BitSnackBarPerItemIconTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ShowIcon, true);
            parameters.Add(p => p.IconName, "Ringer");
        });

        await com.Instance.Show(new BitSnackBarItem { Title = "a", IconName = "Rocket" });
        await com.Instance.Show(new BitSnackBarItem { Title = "b", HideIcon = true });

        var icons = com.FindAll(".bit-snb-ico");

        Assert.AreEqual(1, icons.Count);
        Assert.IsTrue(icons[0].ClassList.Contains("bit-icon--Rocket"));
    }

    [TestMethod,
         DataRow("title", "Go"),
         DataRow("title", "Cancel")
    ]
    public async Task BitSnackBarDismissIconNameTest(string title, string iconName)
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(x => x.DismissIconName, iconName);
            }
        );

        await com.Instance.Show(title);

        var closeButtonIcon = com.Find(".bit-snb-cbt > .bit-icon");

        Assert.IsTrue(closeButtonIcon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod,
        DataRow("title", "fa-solid fa-xmark"),
        DataRow("title", "bi bi-x-lg")
    ]
    public async Task BitSnackBarDismissIconParameterWithCssClassesTest(string title, string cssClasses)
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(x => x.DismissIcon, (BitIconInfo)cssClasses!);
            }
        );

        await com.Instance.Show(title);

        var closeButtonIcon = com.Find(".bit-snb-cbt > i");

        var classes = cssClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var cls in classes)
        {
            Assert.IsTrue(closeButtonIcon.ClassList.Contains(cls), $"Dismiss icon should contain class '{cls}'");
        }
    }

    [TestMethod,
        DataRow("title")
    ]
    public async Task BitSnackBarDismissIconInfoCssHelperTest(string title)
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(x => x.DismissIcon, BitIconInfo.Css("fa-solid fa-xmark"));
            }
        );

        await com.Instance.Show(title);

        var closeButtonIcon = com.Find(".bit-snb-cbt > i");

        Assert.IsTrue(closeButtonIcon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(closeButtonIcon.ClassList.Contains("fa-xmark"));
    }

    [TestMethod,
        DataRow("title")
    ]
    public async Task BitSnackBarDismissIconInfoFaHelperTest(string title)
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(x => x.DismissIcon, BitIconInfo.Fa("solid xmark"));
            }
        );

        await com.Instance.Show(title);

        var closeButtonIcon = com.Find(".bit-snb-cbt > i");

        Assert.IsTrue(closeButtonIcon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(closeButtonIcon.ClassList.Contains("fa-xmark"));
    }

    [TestMethod,
        DataRow("title")
    ]
    public async Task BitSnackBarDismissIconInfoBiHelperTest(string title)
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(x => x.DismissIcon, BitIconInfo.Bi("x-lg"));
            }
        );

        await com.Instance.Show(title);

        var closeButtonIcon = com.Find(".bit-snb-cbt > i");

        Assert.IsTrue(closeButtonIcon.ClassList.Contains("bi"));
        Assert.IsTrue(closeButtonIcon.ClassList.Contains("bi-x-lg"));
    }

    [TestMethod,
        DataRow("title")
    ]
    public async Task BitSnackBarDismissIconTakesPrecedenceOverDismissIconNameTest(string title)
    {
        var com = RenderComponent<BitSnackBar>(
            parameters =>
            {
                parameters.Add(x => x.DismissIcon, BitIconInfo.Fa("solid xmark"));
                parameters.Add(x => x.DismissIconName, "Cancel");
            }
        );

        await com.Instance.Show(title);

        var closeButtonIcon = com.Find(".bit-snb-cbt > i");

        // DismissIcon should take precedence
        Assert.IsTrue(closeButtonIcon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(closeButtonIcon.ClassList.Contains("fa-xmark"));

        // Should not contain DismissIconName classes
        Assert.IsFalse(closeButtonIcon.ClassList.Contains("bit-icon"));
        Assert.IsFalse(closeButtonIcon.ClassList.Contains("bit-icon--Cancel"));
    }

    [TestMethod,
        DataRow("title")
    ]
    public async Task BitSnackBarTitleTemplateTest(string title)
    {
        RenderFragment<string> titleTemplate = (text) => (builder) =>
        {
            builder.AddMarkupContent(0, $"<span>{text}</span>");
        };
        var com = RenderComponent<BitSnackBar>(parameters => parameters
            .Add(p => p.TitleTemplate, titleTemplate)
        );

        await com.Instance.Show(title);

        var titleTemplateElement = com.Find(".bit-snb-hdr");

        var expectedHtml = $@"<span>{title}</span>
                              <button diff:ignore></button>";

        titleTemplateElement.InnerHtml.MarkupMatches(expectedHtml);
    }

    [TestMethod,
        DataRow("title", "body")
    ]
    public async Task BitSnackBarBodyTemplateTest(string title, string body)
    {
        RenderFragment<string> bodyTemplate = (text) => (builder) =>
        {
            builder.AddMarkupContent(0, $"<p>{text}</p>");
        };
        var com = RenderComponent<BitSnackBar>(parameters => parameters
            .Add(p => p.BodyTemplate, bodyTemplate)
        );

        await com.Instance.Show(title, body);

        var itemTemplateElement = com.Find(".bit-snb-itm");

        var expectedHtml = $@"<div diff:ignore></div>
                              <p>{body}</p>";

        itemTemplateElement.InnerHtml.MarkupMatches(expectedHtml);
    }

    [TestMethod]
    public async Task BitSnackBarActionsTemplateTest()
    {
        RenderFragment<BitSnackBarItem> actionsTemplate = item => builder =>
        {
            builder.AddMarkupContent(0, $"<button class=\"undo\">Undo {item.Title}</button>");
        };

        var com = RenderComponent<BitSnackBar>(parameters => parameters
            .Add(p => p.ActionsTemplate, actionsTemplate)
        );

        await com.Instance.Show("title", "body");

        var actions = com.Find(".bit-snb-act");

        Assert.AreEqual("Undo title", actions.QuerySelector(".undo")!.TextContent);
    }

    [TestMethod]
    public async Task BitSnackBarNoActionAreaWithoutTemplateTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title", "body");

        Assert.AreEqual(0, com.FindAll(".bit-snb-act").Count);
    }

    [TestMethod]
    public async Task BitSnackBarItemActionsTakePrecedenceTest()
    {
        RenderFragment<BitSnackBarItem> actionsTemplate = item => builder =>
        {
            builder.AddMarkupContent(0, "<span class=\"host\">host</span>");
        };

        var com = RenderComponent<BitSnackBar>(parameters => parameters
            .Add(p => p.ActionsTemplate, actionsTemplate)
        );

        await com.Instance.Show(new BitSnackBarItem
        {
            Title = "title",
            Actions = builder => builder.AddMarkupContent(0, "<span class=\"own\">own</span>")
        });

        Assert.AreEqual(1, com.FindAll(".bit-snb-act .own").Count);
        Assert.AreEqual(0, com.FindAll(".bit-snb-act .host").Count);
    }

    [TestMethod]
    public async Task BitSnackBarTemplateTest()
    {
        RenderFragment<BitSnackBarItem> template = item => builder =>
        {
            builder.AddMarkupContent(0, $"<div class=\"custom\">{item.Title}</div>");
        };

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.Template, template);
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMinutes(1));
        });

        await com.Instance.Show("title", "body");

        Assert.AreEqual("title", com.Find(".custom").TextContent);

        // The template takes the place of the header, body and actions, and keeps the countdown bar.
        Assert.AreEqual(0, com.FindAll(".bit-snb-hdr").Count);
        Assert.AreEqual(0, com.FindAll(".bit-snb-bdy").Count);
        Assert.AreEqual(1, com.FindAll(".bit-snb-prb").Count);
    }

    [TestMethod,
         DataRow(true),
         DataRow(false)
    ]
    public async Task BitSnackBarMultilineTest(bool multiline)
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Multiline, multiline));

        await com.Instance.Show("title", "body");

        Assert.AreEqual(multiline is false, com.Find(".bit-snb-ttl").ClassList.Contains("bit-snb-elp"));
        Assert.AreEqual(multiline is false, com.Find(".bit-snb-bdy").ClassList.Contains("bit-snb-elp"));
    }

    [TestMethod]
    public async Task BitSnackBarTitleCarriesItsFullTextAsATooltipTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("a very long title");

        Assert.AreEqual("a very long title", com.Find(".bit-snb-ttl").GetAttribute("title"));
    }

    [TestMethod]
    public async Task BitSnackBarItemCssClassAndStyleTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title", "body", cssClass: "custom-class", cssStyle: "color: red");

        var item = com.Find(".bit-snb-itm");

        Assert.IsTrue(item.ClassList.Contains("custom-class"));
        Assert.IsTrue(item.GetAttribute("style")!.Contains("color: red"));
    }

    [TestMethod]
    public async Task BitSnackBarItemStyleIsJoinedWithTheStylesContainerTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters
            .Add(p => p.Styles, new BitSnackBarClassStyles { Container = "border: 1px solid red" })
        );

        await com.Instance.Show("title", "body", cssStyle: "color: blue");

        var style = com.Find(".bit-snb-itm").GetAttribute("style")!;

        // Two parts in one style attribute are only two declarations while a semicolon stands between them.
        Assert.IsTrue(style.Contains("border: 1px solid red;color: blue"), style);
    }

    [TestMethod]
    public async Task BitSnackBarClassesTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ShowIcon, true);
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMinutes(1));
            parameters.Add(p => p.ActionsTemplate, (RenderFragment<BitSnackBarItem>)(item => builder => builder.AddMarkupContent(0, "<span>a</span>")));
            parameters.Add(p => p.Classes, new BitSnackBarClassStyles
            {
                Root = "custom-root",
                Container = "custom-container",
                Header = "custom-header",
                IconContainer = "custom-icon-container",
                Icon = "custom-icon",
                Title = "custom-title",
                Body = "custom-body",
                Actions = "custom-actions",
                DismissButton = "custom-dismiss-button",
                DismissIcon = "custom-dismiss-icon",
                ProgressBar = "custom-progress-bar"
            });
        });

        await com.Instance.Show("title", "body");

        Assert.IsTrue(com.Find(".bit-snb").ClassList.Contains("custom-root"));
        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("custom-container"));
        Assert.IsTrue(com.Find(".bit-snb-hdr").ClassList.Contains("custom-header"));
        Assert.IsTrue(com.Find(".bit-snb-ict").ClassList.Contains("custom-icon-container"));
        Assert.IsTrue(com.Find(".bit-snb-ico").ClassList.Contains("custom-icon"));
        Assert.IsTrue(com.Find(".bit-snb-ttl").ClassList.Contains("custom-title"));
        Assert.IsTrue(com.Find(".bit-snb-bdy").ClassList.Contains("custom-body"));
        Assert.IsTrue(com.Find(".bit-snb-act").ClassList.Contains("custom-actions"));
        Assert.IsTrue(com.Find(".bit-snb-cbt").ClassList.Contains("custom-dismiss-button"));
        Assert.IsTrue(com.Find(".bit-snb-cbt > i").ClassList.Contains("custom-dismiss-icon"));
        Assert.IsTrue(com.Find(".bit-snb-prb").ClassList.Contains("custom-progress-bar"));
    }

    [TestMethod]
    public async Task BitSnackBarStylesTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ShowIcon, true);
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMinutes(1));
            parameters.Add(p => p.ActionsTemplate, (RenderFragment<BitSnackBarItem>)(item => builder => builder.AddMarkupContent(0, "<span>a</span>")));
            parameters.Add(p => p.Styles, new BitSnackBarClassStyles
            {
                Root = "z-index: 1",
                Container = "color: red",
                Header = "color: green",
                IconContainer = "color: blue",
                Icon = "font-size: 1rem",
                Title = "font-weight: 700",
                Body = "font-style: italic",
                Actions = "gap: 1rem",
                DismissButton = "opacity: 0.5",
                DismissIcon = "font-size: 2rem",
                ProgressBar = "background-color: red"
            });
        });

        await com.Instance.Show("title", "body");

        Assert.IsTrue(com.Find(".bit-snb").GetAttribute("style")!.Contains("z-index: 1"));
        Assert.IsTrue(com.Find(".bit-snb-itm").GetAttribute("style")!.Contains("color: red"));
        Assert.IsTrue(com.Find(".bit-snb-hdr").GetAttribute("style")!.Contains("color: green"));
        Assert.IsTrue(com.Find(".bit-snb-ict").GetAttribute("style")!.Contains("color: blue"));
        Assert.IsTrue(com.Find(".bit-snb-ico").GetAttribute("style")!.Contains("font-size: 1rem"));
        Assert.IsTrue(com.Find(".bit-snb-ttl").GetAttribute("style")!.Contains("font-weight: 700"));
        Assert.IsTrue(com.Find(".bit-snb-bdy").GetAttribute("style")!.Contains("font-style: italic"));
        Assert.IsTrue(com.Find(".bit-snb-act").GetAttribute("style")!.Contains("gap: 1rem"));
        Assert.IsTrue(com.Find(".bit-snb-cbt").GetAttribute("style")!.Contains("opacity: 0.5"));
        Assert.IsTrue(com.Find(".bit-snb-cbt > i").GetAttribute("style")!.Contains("font-size: 2rem"));
        Assert.IsTrue(com.Find(".bit-snb-prb").GetAttribute("style")!.Contains("background-color: red"));
    }

    [TestMethod,
         DataRow(BitDir.Ltr),
         DataRow(BitDir.Rtl),
         DataRow(BitDir.Auto),
         DataRow(null)
    ]
    public void BitSnackBarDirTest(BitDir? dir)
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            if (dir.HasValue) parameters.Add(p => p.Dir, dir.Value);
        });

        var root = com.Find(".bit-snb");

        if (dir is null)
        {
            Assert.IsFalse(root.HasAttribute("dir"));
            Assert.IsFalse(root.ClassList.Contains("bit-rtl"));
        }
        else
        {
            Assert.AreEqual(dir.Value.ToString().ToLower(), root.GetAttribute("dir"));
            Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
        }
    }

    [TestMethod,
         DataRow(BitVisibility.Visible, ""),
         DataRow(BitVisibility.Hidden, "visibility:hidden"),
         DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitSnackBarVisibilityTest(BitVisibility visibility, string expectedStyle)
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Visibility, visibility));

        var style = com.Find(".bit-snb").GetAttribute("style") ?? "";

        if (expectedStyle.HasValue())
        {
            Assert.IsTrue(style.Contains(expectedStyle), style);
        }
        else
        {
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
    }

    [TestMethod]
    public async Task BitSnackBarShowThrowsOnNullItemTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => com.Instance.Show(null!));
    }

    [TestMethod]
    public async Task BitSnackBarItemDataIsCarriedAlongTest()
    {
        object? payload = null;

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.TransitionDuration, 0);
            parameters.Add(p => p.OnDismiss, (BitSnackBarItem i) => payload = i.Data);
        });

        var item = new BitSnackBarItem { Title = "title", Data = 42 };

        await com.Instance.Show(item);
        await com.Instance.Close(item);

        Assert.AreEqual(42, payload);
    }

    [TestMethod]
    public async Task BitSnackBarNonLiveRoleHasNoAriaLiveTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Role, "presentation"));

        await com.Instance.Show("title", "body", BitColor.Error);

        var item = com.Find(".bit-snb-itm");

        Assert.AreEqual("presentation", item.GetAttribute("role"));
        Assert.IsFalse(item.HasAttribute("aria-live"));
        Assert.IsFalse(item.HasAttribute("aria-atomic"));
    }

    [TestMethod]
    public async Task BitSnackBarMultilineTitleHasNoTooltipTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Multiline, true));

        await com.Instance.Show("a very long title");

        Assert.IsFalse(com.Find(".bit-snb-ttl").HasAttribute("title"));
    }

    [TestMethod]
    public async Task BitSnackBarItemsIsASnapshotTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        var first = await com.Instance.Show("first");
        await com.Instance.Show("second");

        var snapshot = com.Instance.Items;

        await com.Instance.Close(first);

        Assert.AreEqual(2, snapshot.Count);
        Assert.AreEqual(1, com.Instance.Items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarPauseOnPageHiddenWithoutTheServicesStillCountsDownTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.PauseOnPageHidden, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarShowFromABackgroundThreadTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await Task.Run(() => com.Instance.Show("title", "body"));

        com.WaitForAssertion(() => Assert.AreEqual(1, com.FindAll(".bit-snb-itm").Count));
    }

    [TestMethod]
    public async Task BitSnackBarMaxItemsWhileAnItemIsLeavingTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 2);
            parameters.Add(p => p.TransitionDuration, 300);
        });

        var first = await com.Instance.Show("first");
        await com.Instance.Show("second");

        // The first item is still on screen playing its exit animation while a third arrives.
        var closing = com.Instance.Close(first);

        await com.Instance.Show("third");

        await closing;

        com.WaitForAssertion(() => Assert.AreEqual(2, com.Instance.Items.Count), TimeSpan.FromSeconds(5));

        Assert.AreEqual("second", com.Instance.Items[0].Title);
        Assert.AreEqual("third", com.Instance.Items[1].Title);
    }

    [TestMethod]
    public async Task BitSnackBarDisposeCancelsTheCountdownTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(200));
        });

        await com.Instance.Show("title");

        await ((IAsyncDisposable)com.Instance).DisposeAsync();

        // Nothing should throw once the countdown of a disposed snack bar would have elapsed.
        await Task.Delay(TimeSpan.FromMilliseconds(500));
    }

    [TestMethod]
    public async Task BitSnackBarDismissReasonOfTheDismissButtonTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        var item = await com.Instance.Show("title");

        com.Find(".bit-snb-cbt").Click();

        com.WaitForAssertion(() => Assert.AreEqual(BitSnackBarDismissReason.DismissButton, item.DismissReason));
    }

    [TestMethod]
    public async Task BitSnackBarDismissReasonOfTheEscapeKeyTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        var item = await com.Instance.Show("title");

        com.Find(".bit-snb-itm").KeyDown("Escape");

        com.WaitForAssertion(() => Assert.AreEqual(BitSnackBarDismissReason.Escape, item.DismissReason));
    }

    [TestMethod]
    public async Task BitSnackBarDismissReasonOfTheEscapeKeyOnTheDismissButtonTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        var item = await com.Instance.Show("title");

        com.Find(".bit-snb-cbt").KeyDown("Escape");

        com.WaitForAssertion(() => Assert.AreEqual(BitSnackBarDismissReason.Escape, item.DismissReason));
        Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count);
    }

    [TestMethod]
    public async Task BitSnackBarDismissReasonOfAClickTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.DismissOnClick, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        com.Find(".bit-snb-itm").Click();

        com.WaitForAssertion(() => Assert.AreEqual(BitSnackBarDismissReason.Click, item.DismissReason));
    }

    [TestMethod]
    public async Task BitSnackBarDismissReasonOfTheCountdownTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(100));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        com.WaitForAssertion(() => Assert.AreEqual(BitSnackBarDismissReason.Timeout, item.DismissReason), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarDismissReasonOfCloseAndClearTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        var closed = await com.Instance.Show("closed");
        var cleared = await com.Instance.Show("cleared");

        await com.Instance.Close(closed);
        await com.Instance.Clear();

        Assert.AreEqual(BitSnackBarDismissReason.Close, closed.DismissReason);
        Assert.AreEqual(BitSnackBarDismissReason.Clear, cleared.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarDismissReasonOfMaxItemsTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var first = await com.Instance.Show("first");
        await com.Instance.Show("second");

        Assert.AreEqual(BitSnackBarDismissReason.MaxItems, first.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarDismissReasonIsNullWhileTheItemIsShowingTest()
    {
        var com = RenderComponent<BitSnackBar>();

        var item = await com.Instance.Show("title");

        Assert.IsNull(item.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarRootIsAProgrammaticFocusTargetTest()
    {
        var com = RenderComponent<BitSnackBar>();

        Assert.AreEqual("-1", com.Find(".bit-snb").GetAttribute("tabindex"));

        await com.Instance.Show("title");

        // A plain notification adds no tab stop of its own; whatever focusable content it holds is the way in.
        Assert.IsFalse(com.Find(".bit-snb-itm").HasAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitSnackBarClickableItemIsATabStopTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.DismissOnClick, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        Assert.AreEqual("0", com.Find(".bit-snb-itm").GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitSnackBarEnterActivatesAClickableItemTest()
    {
        var clicked = 0;

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitSnackBarItem _) => clicked++);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        com.Find(".bit-snb-itm").KeyDown("Enter");

        com.WaitForAssertion(() => Assert.AreEqual(1, clicked));
    }

    [TestMethod]
    public async Task BitSnackBarEnterOnTheDismissButtonDoesNotClickTheItemTest()
    {
        var clicked = 0;

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.OnItemClick, (BitSnackBarItem _) => clicked++);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        com.Find(".bit-snb-cbt").KeyDown("Enter");

        await Task.Delay(50);

        Assert.AreEqual(0, clicked);
    }

    [TestMethod]
    public async Task BitSnackBarBodyCarriesItsFullTextAsATooltipTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title", "a very long body");

        Assert.AreEqual("a very long body", com.Find(".bit-snb-bdy").GetAttribute("title"));
    }

    [TestMethod]
    public async Task BitSnackBarMultilineBodyHasNoTooltipTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Multiline, true));

        await com.Instance.Show("title", "a very long body");

        Assert.IsFalse(com.Find(".bit-snb-bdy").HasAttribute("title"));
    }

    [TestMethod]
    public async Task BitSnackBarUpdateOfALeavingItemIsNoOpTest()
    {
        var dismissed = 0;

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(100));
            parameters.Add(p => p.TransitionDuration, 200);
            parameters.Add(p => p.OnDismiss, (BitSnackBarItem _) => dismissed++);
        });

        var item = await com.Instance.Show("title");

        var closing = com.Instance.Close(item);

        // An item whose exit animation is playing is past the point of taking a new countdown, so this must
        // neither keep it on screen nor dismiss it a second time.
        await com.Instance.Update(item);

        await closing;

        await Task.Delay(300);

        Assert.AreEqual(0, com.Instance.Items.Count);
        Assert.AreEqual(1, dismissed);
        Assert.AreEqual(BitSnackBarDismissReason.Close, item.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarPauseOnWindowBlurWithoutTheServicesStillCountsDownTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.PauseOnWindowBlur, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-itm").Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitSnackBarNoHotkeyIsRegisteredByDefaultTest()
    {
        RenderComponent<BitSnackBar>();

        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.SnackBars.registerHotkey"));
    }

    [TestMethod]
    public void BitSnackBarHotkeyIsRegisteredTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Hotkey, ["F8"]));

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.SnackBars.registerHotkey");

        Assert.AreEqual(com.Find(".bit-snb").Id, invocation.Arguments[0]);
        CollectionAssert.AreEqual(new[] { "F8" }, (string[])invocation.Arguments[1]!);
    }

    [TestMethod]
    public void BitSnackBarClearingTheHotkeyUnregistersItTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Hotkey, ["F8"]));

        com.Render(parameters => parameters.Add(p => p.Hotkey, (string[]?)null));

        Assert.IsTrue(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.SnackBars.unregisterHotkey"));
    }

    [TestMethod]
    public async Task BitSnackBarFocusAsyncDoesNotThrowTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.FocusAsync();
    }

    [TestMethod]
    public async Task BitSnackBarQueueOverflowHoldsTheExtraItemsBackTest()
    {
        var shown = new List<string>();

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 2);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.Queue);
            parameters.Add(p => p.TransitionDuration, 0);
            parameters.Add(p => p.OnShow, (BitSnackBarItem i) => shown.Add(i.Title));
        });

        var first = await com.Instance.Show("first");
        await com.Instance.Show("second");
        var third = await com.Instance.Show("third");

        Assert.AreEqual(2, com.Instance.Items.Count);
        Assert.AreEqual(1, com.Instance.Queued.Count);
        Assert.AreEqual("third", com.Instance.Queued[0].Title);
        CollectionAssert.AreEqual(new[] { "first", "second" }, shown);

        // The room the first item gives up goes to the one that was waiting for it.
        await com.Instance.Close(first);

        com.WaitForAssertion(() => Assert.AreEqual(0, com.Instance.Queued.Count));
        Assert.AreEqual(2, com.Instance.Items.Count);
        Assert.AreEqual("third", com.Instance.Items[1].Title);
        CollectionAssert.AreEqual(new[] { "first", "second", "third" }, shown);
        Assert.AreSame(third, com.Instance.Items[1]);
    }

    [TestMethod]
    public async Task BitSnackBarQueueOverflowKeepsTheOldestOnScreenTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.Queue);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var first = await com.Instance.Show("first");
        await com.Instance.Show("second");

        // Unlike DismissOldest, nothing is taken away to make room.
        Assert.AreEqual(1, com.Instance.Items.Count);
        Assert.AreSame(first, com.Instance.Items[0]);
        Assert.IsNull(first.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarRaisingMaxItemsDrainsTheQueueTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.Queue);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("first");
        await com.Instance.Show("second");

        Assert.AreEqual(1, com.Instance.Queued.Count);

        com.Render(parameters =>
        {
            parameters.Add(p => p.MaxItems, 3);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.Queue);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        com.WaitForAssertion(() => Assert.AreEqual(0, com.Instance.Queued.Count));
        Assert.AreEqual(2, com.Instance.Items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarClosingAQueuedItemTakesItOutOfTheQueueTest()
    {
        var dismissed = 0;

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.Queue);
            parameters.Add(p => p.TransitionDuration, 0);
            parameters.Add(p => p.OnDismiss, (BitSnackBarItem _) => dismissed++);
        });

        await com.Instance.Show("first");
        var queued = await com.Instance.Show("second");

        await com.Instance.Close(queued);

        Assert.AreEqual(0, com.Instance.Queued.Count);
        Assert.AreEqual(BitSnackBarDismissReason.Close, queued.DismissReason);

        // It was never shown, so OnShow never fired for it and neither does OnDismiss.
        Assert.AreEqual(0, dismissed);
    }

    [TestMethod]
    public async Task BitSnackBarClearAlsoDropsTheQueueTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.Queue);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("first");
        var queued = await com.Instance.Show("second");

        await com.Instance.Clear();

        Assert.AreEqual(0, com.Instance.Items.Count);
        Assert.AreEqual(0, com.Instance.Queued.Count);
        Assert.AreEqual(BitSnackBarDismissReason.Clear, queued.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarPreventDuplicatesAlsoLooksAtTheQueueTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.Queue);
            parameters.Add(p => p.PreventDuplicates, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("first");
        var queued = await com.Instance.Show("second");
        var again = await com.Instance.Show("second");

        Assert.AreSame(queued, again);
        Assert.AreEqual(1, com.Instance.Queued.Count);
    }

    [TestMethod]
    public async Task BitSnackBarDismissOldestIsTheDefaultOverflowTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var first = await com.Instance.Show("first");
        await com.Instance.Show("second");

        Assert.AreEqual(0, com.Instance.Queued.Count);
        Assert.AreEqual(1, com.Instance.Items.Count);
        Assert.AreEqual(BitSnackBarDismissReason.MaxItems, first.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarClearWaitsForAnItemThatIsAlreadyLeavingTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 200));

        var item = await com.Instance.Show("title");

        // The exit animation of this one is playing when the Clear arrives.
        var closing = com.Instance.Close(item);

        await com.Instance.Clear();

        Assert.AreEqual(0, com.Instance.Items.Count);

        await closing;
    }

    [TestMethod]
    public void BitSnackBarOffsetTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Offset, "3rem"));

        StringAssert.Contains(com.Find(".bit-snb").GetAttribute("style"), "--bit-snb-off:3rem");
    }

    [TestMethod]
    public void BitSnackBarNoOffsetByDefaultTest()
    {
        var com = RenderComponent<BitSnackBar>();

        Assert.IsFalse(com.Find(".bit-snb").GetAttribute("style")!.Contains("--bit-snb-off"));
    }

    [TestMethod]
    public async Task BitSnackBarShowAfterDisposeIsNoOpTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await ((IAsyncDisposable)com.Instance).DisposeAsync();

        var item = await com.Instance.Show("title");

        Assert.IsNotNull(item);
        Assert.AreEqual(0, com.Instance.Items.Count);

        // None of the other public entry points throw on a disposed snack bar either.
        await com.Instance.Close(item);
        await com.Instance.Clear();
        await com.Instance.Update(item);
        await com.Instance.Pause(item);
        await com.Instance.Resume(item);
    }

    [TestMethod]
    public void BitSnackBarAnnouncersAreOnThePageBeforeAnythingIsShownTest()
    {
        var com = RenderComponent<BitSnackBar>();

        var announcers = com.FindAll(".bit-snb-ann");

        // A live region has to already be there when its content changes, so both are rendered with the host.
        Assert.AreEqual(2, announcers.Count);
        Assert.AreEqual("status", announcers[0].GetAttribute("role"));
        Assert.AreEqual("polite", announcers[0].GetAttribute("aria-live"));
        Assert.AreEqual("false", announcers[0].GetAttribute("aria-atomic"));
        Assert.AreEqual("alert", announcers[1].GetAttribute("role"));
        Assert.AreEqual("assertive", announcers[1].GetAttribute("aria-live"));
        Assert.AreEqual("false", announcers[1].GetAttribute("aria-atomic"));
        Assert.AreEqual(string.Empty, announcers[0].TextContent);
        Assert.AreEqual(string.Empty, announcers[1].TextContent);
    }

    [TestMethod]
    public async Task BitSnackBarAnnouncementTextTakesOverTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show(new BitSnackBarItem
        {
            Title = "Deleted",
            Body = "report.pdf",
            AnnouncementText = "report.pdf was deleted, undo is available"
        });

        StringAssert.Contains(com.FindAll(".bit-snb-ann")[0].TextContent, "report.pdf was deleted, undo is available");
    }

    [TestMethod]
    public async Task BitSnackBarAnItemWithNoTextAnnouncesItselfTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            RenderFragment<BitSnackBarItem> template = _ => builder =>
            {
                builder.AddMarkupContent(0, "<span>drawn by a template</span>");
            };

            parameters.Add(p => p.Template, template);
        });

        await com.Instance.Show(new BitSnackBarItem());

        // There is nothing for the announcer to say, so the item stays its own live region rather than going unheard.
        Assert.AreEqual(string.Empty, com.FindAll(".bit-snb-ann")[0].TextContent);
        Assert.AreEqual("polite", com.Find(".bit-snb-itm").GetAttribute("aria-live"));
        Assert.AreEqual("true", com.Find(".bit-snb-itm").GetAttribute("aria-atomic"));
    }

    [TestMethod]
    public async Task BitSnackBarANonLiveRoleAnnouncesNothingTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.Role, "presentation"));

        await com.Instance.Show("title", "body", BitColor.Error);

        Assert.AreEqual(string.Empty, com.FindAll(".bit-snb-ann")[0].TextContent);
        Assert.AreEqual(string.Empty, com.FindAll(".bit-snb-ann")[1].TextContent);
        Assert.IsFalse(com.Find(".bit-snb-itm").HasAttribute("aria-live"));
    }

    [TestMethod]
    public async Task BitSnackBarTheSameMessageTwiceIsAnnouncedTwiceTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("Saved");
        await com.Instance.Show(new BitSnackBarItem { Title = "Saved" });

        // Each announcement is a node of its own, so a repeat is not swallowed as "the text did not change".
        Assert.AreEqual(2, com.FindAll(".bit-snb-ann")[0].Children.Length);
    }

    [TestMethod]
    public async Task BitSnackBarAnAnnouncementIsRetiredAfterItIsReadTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("Saved");

        Assert.AreEqual(1, com.FindAll(".bit-snb-ann")[0].Children.Length);

        // It is taken back out so the region does not become a transcript a screen reader user can walk into.
        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-snb-ann")[0].Children.Length),
                             TimeSpan.FromSeconds(10));
    }

    [TestMethod]
    public async Task BitSnackBarAQueuedItemIsAnnouncedWhenItReachesTheScreenTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.Queue);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var first = await com.Instance.Show("first");
        await com.Instance.Show("second");

        Assert.AreEqual(1, com.FindAll(".bit-snb-ann")[0].Children.Length);

        await com.Instance.Close(first);

        com.WaitForAssertion(() => Assert.AreEqual(2, com.FindAll(".bit-snb-ann")[0].Children.Length));
    }

    [TestMethod]
    public async Task BitSnackBarSwipeIsOffByDefaultTest()
    {
        var com = RenderComponent<BitSnackBar>();

        await com.Instance.Show("title");

        // A snack bar that never asked for a swipe reaches the script not once, not even to unregister.
        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier.StartsWith("BitBlazorUI.SnackBars.")));

        // The id the swipe script reads is on the item whether or not the swipe is registered, so turning the
        // feature on needs no re-render of what is already showing.
        Assert.IsTrue(com.Find(".bit-snb-itm").HasAttribute("data-bit-snb-id"));
    }

    [TestMethod]
    public void BitSnackBarSwipeIsRegisteredTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.SwipeToDismiss, true);
            parameters.Add(p => p.SwipeThreshold, 80);
        });

        var invocation = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.SnackBars.registerSwipe");

        Assert.AreEqual(com.Find(".bit-snb").Id, invocation.Arguments[0]);
        Assert.AreEqual(80, invocation.Arguments[1]);
    }

    [TestMethod]
    public void BitSnackBarTurningTheSwipeOffUnregistersItTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.SwipeToDismiss, true));

        com.Render(parameters => parameters.Add(p => p.SwipeToDismiss, false));

        Assert.IsTrue(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.SnackBars.unregisterSwipe"));
    }

    [TestMethod]
    public async Task BitSnackBarSwipeDismissesTheItemTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.SwipeToDismiss, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        await com.Instance._SwipeDismissed(item.Id.ToString());

        Assert.AreEqual(0, com.Instance.Items.Count);
        Assert.AreEqual(BitSnackBarDismissReason.Swipe, item.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarSwipeLeavesPersistentItemsTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.SwipeToDismiss, true);
            parameters.Add(p => p.Persistent, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        // A persistent item carries no id for the script to pick up, and refuses the request even if one arrives.
        Assert.IsFalse(com.Find(".bit-snb-itm").HasAttribute("data-bit-snb-id"));

        await com.Instance._SwipeDismissed(item.Id.ToString());

        Assert.AreEqual(1, com.Instance.Items.Count);
        Assert.IsNull(item.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarSwipeOfAnUnknownItemIsNoOpTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.SwipeToDismiss, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        await com.Instance._SwipeDismissed(Guid.NewGuid().ToString());
        await com.Instance._SwipeDismissed("not-a-guid");

        Assert.AreEqual(1, com.Instance.Items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarUpdateAnnouncesTheNewStateTest()
    {
        var com = RenderComponent<BitSnackBar>();

        var item = await com.Instance.Show(new BitSnackBarItem { Title = "Uploading", Persistent = true });

        Assert.AreEqual(1, com.FindAll(".bit-snb-ann")[0].Children.Length);

        item.Title = "Upload complete";

        await com.Instance.Update(item);

        // The change is a new report, so it is heard as well as seen.
        Assert.AreEqual(2, com.FindAll(".bit-snb-ann")[0].Children.Length);
        StringAssert.Contains(com.FindAll(".bit-snb-ann")[0].TextContent, "Upload complete");
    }

    [TestMethod]
    public async Task BitSnackBarTurningTheQueueOffStillDrainsItTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.MaxItems, 2);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.Queue);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var first = await com.Instance.Show("first");
        await com.Instance.Show("second");
        await com.Instance.Show("third");

        Assert.AreEqual(1, com.Instance.Queued.Count);

        com.Render(parameters =>
        {
            parameters.Add(p => p.MaxItems, 2);
            parameters.Add(p => p.Overflow, BitSnackBarOverflow.DismissOldest);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        // What was already queued is not stranded by the mode changing under it.
        await com.Instance.Close(first);

        com.WaitForAssertion(() => Assert.AreEqual(0, com.Instance.Queued.Count));
        Assert.AreEqual(2, com.Instance.Items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarDisposeUnregistersTheScriptTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.Hotkey, ["F8"]);
            parameters.Add(p => p.SwipeToDismiss, true);
        });

        await ((IAsyncDisposable)com.Instance).DisposeAsync();

        Assert.IsTrue(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.SnackBars.unregisterHotkey"));
        Assert.IsTrue(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.SnackBars.unregisterSwipe"));
    }

    [TestMethod]
    public async Task BitSnackBarHideDismissButtonKeepsTheCountdownTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.HideDismissButton, true);
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        Assert.AreEqual(0, com.FindAll(".bit-snb-cbt").Count);

        // Unlike Persistent, the item still counts down and still draws the bar that shows it.
        Assert.AreEqual(1, com.FindAll(".bit-snb-prb").Count);

        com.WaitForAssertion(() => Assert.AreEqual(0, com.Instance.Items.Count), TimeSpan.FromSeconds(5));
        Assert.AreEqual(BitSnackBarDismissReason.Timeout, item.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarHideDismissButtonStillAnswersEscapeTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.HideDismissButton, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        com.Find(".bit-snb-itm").KeyDown("Escape");

        com.WaitForAssertion(() => Assert.AreEqual(BitSnackBarDismissReason.Escape, item.DismissReason));
    }

    [TestMethod]
    public async Task BitSnackBarPerItemHideDismissButtonTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        await com.Instance.Show(new BitSnackBarItem { Title = "with a button" });
        await com.Instance.Show(new BitSnackBarItem { Title = "without one", HideDismissButton = true });

        Assert.AreEqual(2, com.FindAll(".bit-snb-itm").Count);
        Assert.AreEqual(1, com.FindAll(".bit-snb-cbt").Count);
    }

    [TestMethod]
    public async Task BitSnackBarHideDismissButtonIsStillSwipeableTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.HideDismissButton, true);
            parameters.Add(p => p.SwipeToDismiss, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        Assert.IsTrue(com.Find(".bit-snb-itm").HasAttribute("data-bit-snb-id"));

        await com.Instance._SwipeDismissed(item.Id.ToString());

        Assert.AreEqual(0, com.Instance.Items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarPauseOnPageHiddenHoldsTheCountdownTest()
    {
        var visibility = new BitPageVisibility(new TestJsRuntime());
        Context.Services.AddSingleton(visibility);

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.PauseOnPageHidden, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        await visibility._VisibilityChanged(true);

        await Task.Delay(600);

        // The countdown of a notification in a background tab is held rather than spent before it is looked at.
        Assert.AreEqual(1, com.Instance.Items.Count);
        Assert.IsTrue(com.Find(".bit-snb-itm").ClassList.Contains("bit-snb-pau"));

        await visibility._VisibilityChanged(false);

        com.WaitForAssertion(() => Assert.AreEqual(0, com.Instance.Items.Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarPauseOnWindowBlurHoldsTheCountdownTest()
    {
        var visibility = new BitPageVisibility(new TestJsRuntime());
        Context.Services.AddSingleton(visibility);

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.PauseOnWindowBlur, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        await visibility._WindowFocusChanged(true);

        await Task.Delay(600);

        Assert.AreEqual(1, com.Instance.Items.Count);

        await visibility._WindowFocusChanged(false);

        com.WaitForAssertion(() => Assert.AreEqual(0, com.Instance.Items.Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarTheWindowAndThePageHoldTheCountdownIndependentlyTest()
    {
        var visibility = new BitPageVisibility(new TestJsRuntime());
        Context.Services.AddSingleton(visibility);

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.PauseOnPageHidden, true);
            parameters.Add(p => p.PauseOnWindowBlur, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        await visibility._VisibilityChanged(true);
        await visibility._WindowFocusChanged(true);

        // Letting go of one reason while the other still stands does not let the countdown go.
        await visibility._VisibilityChanged(false);

        await Task.Delay(600);

        Assert.AreEqual(1, com.Instance.Items.Count);

        await visibility._WindowFocusChanged(false);

        com.WaitForAssertion(() => Assert.AreEqual(0, com.Instance.Items.Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarTurningPauseOnPageHiddenOffLetsTheCountdownGoTest()
    {
        var visibility = new BitPageVisibility(new TestJsRuntime());
        Context.Services.AddSingleton(visibility);

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.PauseOnPageHidden, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await com.Instance.Show("title");

        await visibility._VisibilityChanged(true);

        await Task.Delay(400);

        Assert.AreEqual(1, com.Instance.Items.Count);

        // The next visibility event might never come, so turning the guard off has to let go of what it is holding.
        com.Render(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.PauseOnPageHidden, false);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        com.WaitForAssertion(() => Assert.AreEqual(0, com.Instance.Items.Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarAPauseFromCodeSurvivesTheHoverLeavingTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.AutoDismiss, true);
            parameters.Add(p => p.AutoDismissTime, TimeSpan.FromMilliseconds(300));
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await com.Instance.Show("title");

        await com.Instance.Pause(item);

        com.Find(".bit-snb-itm").MouseEnter();
        com.Find(".bit-snb-itm").MouseLeave();

        await Task.Delay(600);

        // A hold the code asked for is only let go by the code.
        Assert.AreEqual(1, com.Instance.Items.Count);

        await com.Instance.Resume(item);

        com.WaitForAssertion(() => Assert.AreEqual(0, com.Instance.Items.Count), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task BitSnackBarShowingAnItemAgainClearsItsDismissReasonTest()
    {
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.TransitionDuration, 0));

        var item = await com.Instance.Show(new BitSnackBarItem { Title = "title" });

        await com.Instance.Close(item);

        Assert.AreEqual(BitSnackBarDismissReason.Close, item.DismissReason);

        await com.Instance.Show(item);

        // The reason answers "why is this gone", so an item that is back on screen has none.
        Assert.IsNull(item.DismissReason);
    }
}
