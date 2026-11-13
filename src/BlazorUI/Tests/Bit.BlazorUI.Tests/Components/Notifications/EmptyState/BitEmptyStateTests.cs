using System;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.EmptyState;

[TestClass, DoNotParallelize]
public class BitEmptyStateTests : BunitTestContext
{
    private sealed class GermanStrings : BitStrings
    {
        public override string NoItemsFound => "Keine Einträge";
    }

    [TestCleanup]
    public void ResetStrings() => BitStrings.Current = null;

    private IRenderedComponent<BitEmptyState> Render(Action<ComponentParameterCollectionBuilder<BitEmptyState>>? parameters = null)
        => RenderComponent<BitEmptyState>(p => parameters?.Invoke(p));

    [TestMethod]
    public void BitEmptyStateShouldSayItHasNoItemsWhenGivenNothing()
    {
        var com = Render();

        var root = com.Find(".bit-ems");

        // A title in the default words and nothing else: no icon, no line under it, no actions.
        Assert.AreEqual(1, root.Children.Length);
        Assert.AreEqual("No items found", com.Find(".bit-ems-ttl").TextContent.Trim());
        Assert.AreEqual(0, com.FindAll(".bit-ems-ico, .bit-ems-dsc, .bit-ems-con").Count);

        // Plain content: neither a live region nor a heading, so it neither interrupts a screen reader nor adds an entry
        // to the outline of the page from inside a list.
        Assert.IsNull(root.GetAttribute("role"));
        Assert.IsNull(root.GetAttribute("aria-live"));
        Assert.IsNull(com.Find(".bit-ems-ttl").GetAttribute("role"));
        Assert.IsNull(com.Find(".bit-ems-ttl").GetAttribute("aria-level"));
    }

    [TestMethod]
    public void BitEmptyStateShouldSayTheTranslatedWordsOfTheStrings()
    {
        BitStrings.Current = new GermanStrings();

        var com = Render();

        Assert.AreEqual("Keine Einträge", com.Find(".bit-ems-ttl").TextContent.Trim());

        com.Render(p => p.Add(x => x.Title, "Leer"));

        Assert.AreEqual("Leer", com.Find(".bit-ems-ttl").TextContent.Trim(), "the title of the app wins over the strings");
    }

    [TestMethod]
    public void BitEmptyStateShouldRenderItsTitleDescriptionAndActionsInTheirOrder()
    {
        var com = Render(p =>
        {
            p.Add(x => x.IconName, "Inbox");
            p.Add(x => x.Title, "No messages");
            p.Add(x => x.Description, "When someone writes to you, it shows up here.");
            p.AddChildContent("<button class=\"compose\">Write a message</button>");
        });

        var parts = com.Find(".bit-ems").Children.Select(c => c.ClassList[0]).ToArray();
        CollectionAssert.AreEqual(new[] { "bit-ems-ico", "bit-ems-ttl", "bit-ems-dsc", "bit-ems-con" }, parts);

        Assert.AreEqual("No messages", com.Find(".bit-ems-ttl").TextContent.Trim());
        Assert.AreEqual("When someone writes to you, it shows up here.", com.Find(".bit-ems-dsc").TextContent.Trim());
        Assert.IsNotNull(com.Find(".bit-ems-con > button.compose"));
    }

    [TestMethod]
    public void BitEmptyStateShouldHideItsIconFromScreenReaders()
    {
        var com = Render(p => p.Add(x => x.IconName, "Inbox"));

        var icon = com.Find(".bit-ems-ico");

        Assert.AreEqual("I", icon.TagName);
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Inbox"));
    }

    [TestMethod]
    public void BitEmptyStateShouldPreferTheIconOverTheIconNameAndTheTemplateOverBoth()
    {
        var com = Render(p =>
        {
            p.Add(x => x.IconName, "Inbox");
            p.Add(x => x.Icon, BitIconInfo.Fa("solid folder-open"));
        });

        var icon = com.Find(".bit-ems-ico");
        Assert.IsTrue(icon.ClassList.Contains("fa-folder-open"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Inbox"));

        // A template is content of the app's own - an illustration with a text of its own, for all the component
        // knows - so it is read as it is written rather than hidden.
        com.Render(p => p.Add(x => x.IconTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<img class=\"art\" alt=\"An empty box\" />"))));

        var holder = com.Find(".bit-ems-ico");
        Assert.AreEqual("DIV", holder.TagName);
        Assert.IsNull(holder.GetAttribute("aria-hidden"));
        Assert.IsNotNull(holder.QuerySelector("img.art"));
        Assert.AreEqual(0, com.FindAll("i.bit-ems-ico").Count);
    }

    [TestMethod]
    public void BitEmptyStateShouldRenderItsTemplatesInPlaceOfItsTexts()
    {
        var com = Render(p =>
        {
            p.Add(x => x.Title, "No results");
            p.Add(x => x.Description, "Try another word.");
            p.Add(x => x.TitleTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "No results for <i>widget</i>")));
            p.Add(x => x.DescriptionTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<a href=\"/all\">Browse all</a>")));
        });

        Assert.AreEqual("No results for widget", com.Find(".bit-ems-ttl").TextContent.Trim());
        Assert.IsNotNull(com.Find(".bit-ems-ttl > i"));
        Assert.AreEqual("Browse all", com.Find(".bit-ems-dsc > a").TextContent);
        Assert.IsFalse(com.Find(".bit-ems").TextContent.Contains("Try another word."));
    }

    [TestMethod]
    public void BitEmptyStateShouldLeaveOutTheLineUnderTheTitleWhenItIsEmpty()
    {
        var com = Render(p => p.Add(x => x.Description, ""));

        Assert.AreEqual(0, com.FindAll(".bit-ems-dsc").Count);
    }

    [TestMethod]
    [DataRow(2, "2")]
    [DataRow(0, "1")]
    [DataRow(9, "6")]
    public void BitEmptyStateShouldMakeItsTitleAHeadingOfTheLevelAsked(int level, string expected)
    {
        var com = Render(p => p.Add(x => x.HeadingLevel, level));

        var title = com.Find(".bit-ems-ttl");

        Assert.AreEqual("heading", title.GetAttribute("role"));
        Assert.AreEqual(expected, title.GetAttribute("aria-level"), "the level is kept between 1 and 6");
    }

    [TestMethod]
    public void BitEmptyStateShouldApplyTheClassesAndStylesOfEachOfItsParts()
    {
        var com = Render(p =>
        {
            p.Add(x => x.IconName, "Inbox");
            p.Add(x => x.Description, "Line");
            p.AddChildContent("<button>Go</button>");
            p.Add(x => x.Class, "own-class");
            p.Add(x => x.Style, "margin:1px");
            p.Add(x => x.Classes, new BitEmptyStateClassStyles { Root = "c-root", Icon = "c-icon", Title = "c-title", Description = "c-desc", Content = "c-con" });
            p.Add(x => x.Styles, new BitEmptyStateClassStyles { Root = "padding:2px", Icon = "color:red", Title = "color:blue", Description = "color:green", Content = "gap:3px" });
        });

        var root = com.Find(".bit-ems");
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        Assert.IsTrue(root.ClassList.Contains("own-class"));
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");

        foreach (var (part, cls, style) in new[]
        {
            (".bit-ems-ico", "c-icon", "color:red"),
            (".bit-ems-ttl", "c-title", "color:blue"),
            (".bit-ems-dsc", "c-desc", "color:green"),
            (".bit-ems-con", "c-con", "gap:3px"),
        })
        {
            var element = com.Find(part);
            Assert.IsTrue(element.ClassList.Contains(cls), $"{part} lacks {cls}");
            Assert.AreEqual(style, element.GetAttribute("style"), $"the style of {part}");
        }
    }

    [TestMethod]
    public void BitEmptyStateShouldWriteItsDirection()
    {
        var com = Render(p => p.Add(x => x.Dir, BitDir.Rtl));

        Assert.AreEqual("rtl", com.Find(".bit-ems").GetAttribute("dir"));
    }
}
