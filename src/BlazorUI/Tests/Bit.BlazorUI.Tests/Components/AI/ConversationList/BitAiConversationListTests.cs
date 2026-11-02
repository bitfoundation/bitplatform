using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ConversationList;

[TestClass]
public class BitAiConversationListTests : BitAiTestBase
{
    private static IReadOnlyList<BitAiConversationSummary> Conversations()
    {
        var now = DateTimeOffset.Now;

        return
        [
            new BitAiConversationSummary("old", "Old trip", now.AddDays(-3), "a while ago"),
            new BitAiConversationSummary("new", "New trip", now, "just now"),
            new BitAiConversationSummary("untitled", null, now.AddSeconds(-1)),
        ];
    }

    [TestMethod]
    public void BitAiConversationListShouldBeANavigationNamedFromTheStringTable()
    {
        var component = RenderComponent<BitAiConversationList>();

        var root = component.Find("nav.bit-aicvl");

        Assert.AreEqual("Conversations", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAiConversationListShouldGroupTheConversationsByRecencyNewestFirst()
    {
        var component = RenderComponent<BitAiConversationList>(p => p.Add(c => c.Conversations, Conversations()));

        var headers = component.FindAll(".bit-aicvl-grp").Select(h => h.TextContent).ToArray();
        CollectionAssert.AreEqual(new[] { "Today", "Previous 7 days" }, headers);

        var lists = component.FindAll(".bit-aicvl-lst");
        Assert.HasCount(2, lists);
        Assert.AreEqual(component.FindAll(".bit-aicvl-grp")[0].Id, lists[0].GetAttribute("aria-labelledby"));

        var titles = component.FindAll(".bit-aicvl-ttl").Select(t => t.TextContent).ToArray();
        CollectionAssert.AreEqual(new[] { "New trip", "Untitled", "Old trip" }, titles);
    }

    [TestMethod]
    public void TheOpenConversationIsMarkedAsCurrent()
    {
        var component = RenderComponent<BitAiConversationList>(p => p
            .Add(c => c.Conversations, Conversations())
            .Add(c => c.SelectedId, "old"));

        var current = component.FindAll("button[aria-current='true']");

        Assert.HasCount(1, current);
        Assert.IsTrue(current[0].ClassList.Contains("bit-aicvl-sel"));
        Assert.AreEqual("Old trip", current[0].TextContent.Trim());
        Assert.AreEqual("a while ago", current[0].GetAttribute("title"));
    }

    [TestMethod]
    public void ChoosingAConversationRaisesOnSelect()
    {
        BitAiConversationSummary? chosen = null;

        var component = RenderComponent<BitAiConversationList>(p => p
            .Add(c => c.Conversations, Conversations())
            .Add(c => c.OnSelect, s => chosen = s));

        component.FindAll(".bit-aicvl-ibt")[2].Click();

        Assert.AreEqual("old", chosen?.ConversationId);
    }

    [TestMethod]
    public void TheDeleteButtonsAreShownOnlyWithAHandlerAndNameTheirConversation()
    {
        var without = RenderComponent<BitAiConversationList>(p => p.Add(c => c.Conversations, Conversations()));

        Assert.IsEmpty(without.FindAll("button[title='Delete']"));

        BitAiConversationSummary? deleted = null;
        var with = RenderComponent<BitAiConversationList>(p => p
            .Add(c => c.Conversations, Conversations())
            .Add(c => c.OnDelete, s => deleted = s));

        var buttons = with.FindAll("button[title='Delete']");
        Assert.HasCount(3, buttons);
        Assert.AreEqual("Delete New trip", buttons[0].GetAttribute("aria-label"));
        Assert.AreEqual("Delete Untitled", buttons[1].GetAttribute("aria-label"));

        buttons[0].Click();

        Assert.AreEqual("new", deleted?.ConversationId);
    }

    // An app that takes a deleted conversation out of the list in its handler, the way a list over a store does.
    private sealed class DeletingHost : ComponentBase
    {
        private List<BitAiConversationSummary> _conversations = [];

        [Parameter] public IReadOnlyList<BitAiConversationSummary> Initial { get; set; } = [];

        protected override void OnInitialized() => _conversations = [.. Initial];

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitAiConversationList>(0);
            builder.AddComponentParameter(1, nameof(BitAiConversationList.Conversations), _conversations);
            builder.AddComponentParameter(2, nameof(BitAiConversationList.OnDelete), EventCallback.Factory.Create<BitAiConversationSummary>(this, Delete));
            builder.CloseComponent();
        }

        private void Delete(BitAiConversationSummary summary) =>
            _conversations = _conversations.Where(c => c.ConversationId != summary.ConversationId).ToList();
    }

    [TestMethod]
    public void DeletingAConversationHandsTheFocusToTheOneInItsPlaceAndAnEmptiedListToTheNewButton()
    {
        var component = RenderComponent<DeletingHost>(p => p.Add(c => c.Initial, Conversations()));

        string FocusedId() => ((ElementReference)Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Last().Arguments[0]!).Id;
        // The reference the list holds to the button of a conversation.
        string ItemButtonId(string conversationId)
        {
            var list = component.FindComponent<BitAiConversationList>().Instance;
            var field = typeof(BitAiConversationList).GetField("_itemButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            return ((Dictionary<string, ElementReference>)field.GetValue(list)!)[conversationId].Id;
        }

        // "New trip" is first: "Untitled", next in the list, takes its place and the focus.
        component.FindAll("button[title='Delete']")[0].Click();
        Assert.AreEqual("Untitled", component.FindAll(".bit-aicvl-ibt")[0].TextContent.Trim());
        Assert.AreEqual(ItemButtonId("untitled"), FocusedId());

        // The last one goes: the focus goes to the one now last.
        component.FindAll("button[title='Delete']")[1].Click();
        Assert.AreEqual(ItemButtonId("untitled"), FocusedId());

        // None is left: the focus goes to the new-conversation button.
        component.FindAll("button[title='Delete']")[0].Click();
        Assert.IsEmpty(component.FindAll(".bit-aicvl-ibt"));
        Assert.AreEqual(component.FindComponent<BitActionButton>().Instance.RootElement.Id, FocusedId());
    }

    [TestMethod]
    public void AConversationTheAppKeepsKeepsTheFocusWhereItIs()
    {
        var component = RenderComponent<BitAiConversationList>(p => p
            .Add(c => c.Conversations, Conversations())
            .Add(c => c.OnDelete, _ => { }));

        component.FindAll("button[title='Delete']")[0].Click();
        component.Render(p => p.Add(c => c.SelectedId, "old"));

        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void TheDaysAreCountedInTheTimeZoneGiven()
    {
        // As far ahead of UTC as any zone is, so its days start at a time no test machine's local zone starts them.
        var zone = TimeZoneInfo.CreateCustomTimeZone("bit-test+14", TimeSpan.FromHours(14), "UTC+14", "UTC+14");
        var startOfToday = new DateTimeOffset(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date, TimeSpan.FromHours(14));

        var component = RenderComponent<BitAiConversationList>(p => p
            .Add(c => c.TimeZone, zone)
            .Add(c => c.Conversations, new List<BitAiConversationSummary>
            {
                new("today", "Just after midnight", startOfToday),
                new("yesterday", "Just before midnight", startOfToday.AddMinutes(-1)),
            }));

        CollectionAssert.AreEqual(new[] { "Today", "Yesterday" }, component.FindAll(".bit-aicvl-grp").Select(h => h.TextContent).ToArray());
    }

    // A list with no zone of its own counts its days in the zone the options in force show the times of the messages in.
    [TestMethod]
    public void TheDaysAreCountedInTheTimeZoneOfTheOptionsWhenTheListHasNone()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("bit-test+14", TimeSpan.FromHours(14), "UTC+14", "UTC+14");
        var startOfToday = new DateTimeOffset(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date, TimeSpan.FromHours(14));

        var component = RenderComponent<BitAiConversationList>(p => p
            .AddCascadingValue(BitAiUiOptions.Default with { TimeZone = zone })
            .Add(c => c.Conversations, new List<BitAiConversationSummary>
            {
                new("today", "Just after midnight", startOfToday),
                new("yesterday", "Just before midnight", startOfToday.AddMinutes(-1)),
            }));

        CollectionAssert.AreEqual(new[] { "Today", "Yesterday" }, component.FindAll(".bit-aicvl-grp").Select(h => h.TextContent).ToArray());
    }

    [TestMethod]
    public void EachGroupSaysItIsAListWhateverItsMarkersLookLike()
    {
        var component = RenderComponent<BitAiConversationList>(p => p.Add(c => c.Conversations, Conversations()));

        Assert.IsTrue(component.FindAll("ul.bit-aicvl-lst").All(l => l.GetAttribute("role") == "list"));
    }

    [TestMethod]
    public void TheNewConversationButtonIsShownUnlessAskedNotTo()
    {
        var created = 0;

        var component = RenderComponent<BitAiConversationList>(p => p.Add(c => c.OnNew, () => created++));

        var button = component.Find(".bit-aicvl > button.bit-acb");
        Assert.AreEqual("New conversation", button.TextContent.Trim());

        button.Click();
        Assert.AreEqual(1, created);

        component.Render(p => p.Add(c => c.ShowNewButton, false));

        Assert.IsEmpty(component.FindAll(".bit-acb"));
    }

    [TestMethod]
    public void ADisabledListLetsNothingBeChosen()
    {
        var chosen = 0;

        var component = RenderComponent<BitAiConversationList>(p => p
            .Add(c => c.Conversations, Conversations())
            .Add(c => c.Disabled, true)
            .Add(c => c.OnSelect, _ => chosen++)
            .Add(c => c.OnDelete, _ => chosen++)
            .Add(c => c.OnNew, () => chosen++));

        Assert.IsTrue(component.Find(".bit-aicvl").ClassList.Contains("bit-dis"));

        var item = component.Find(".bit-aicvl-ibt");
        Assert.IsTrue(item.HasAttribute("disabled"));
        Assert.AreEqual("true", item.GetAttribute("aria-disabled"));

        component.Find("button[title='Delete']").Click();
        component.Find(".bit-acb").Click();

        Assert.AreEqual(0, chosen);
    }

    [TestMethod]
    public void BitAiConversationListShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitAiConversationList>(p => p
            .Add(c => c.Conversations, Conversations())
            .Add(c => c.SelectedId, "new")
            .Add(c => c.Classes, new() { Root = "custom-root", GroupHeader = "custom-group", List = "custom-list", Item = "custom-item", ItemButton = "custom-button", SelectedItemButton = "custom-selected", ItemTitle = "custom-title" })
            .Add(c => c.Styles, new() { Root = "color: red;", GroupHeader = "color: green;", ItemButton = "color: blue;", SelectedItemButton = "font-weight: bold;" }));

        Assert.IsTrue(component.Find(".bit-aicvl").ClassList.Contains("custom-root"));
        StringAssert.Contains(component.Find(".bit-aicvl").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-aicvl-grp").ClassList.Contains("custom-group"));
        Assert.AreEqual("color: green;", component.Find(".bit-aicvl-grp").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aicvl-lst").ClassList.Contains("custom-list"));
        Assert.IsTrue(component.Find(".bit-aicvl-itm").ClassList.Contains("custom-item"));
        Assert.IsTrue(component.Find(".bit-aicvl-ttl").ClassList.Contains("custom-title"));

        var selected = component.Find(".bit-aicvl-sel");
        Assert.IsTrue(selected.ClassList.Contains("custom-button"));
        Assert.IsTrue(selected.ClassList.Contains("custom-selected"));
        StringAssert.Contains(selected.GetAttribute("style"), "color: blue;");
        StringAssert.Contains(selected.GetAttribute("style"), "font-weight: bold;");

        var other = component.FindAll(".bit-aicvl-ibt").First(b => b.ClassList.Contains("bit-aicvl-sel") is false);
        Assert.IsFalse(other.ClassList.Contains("custom-selected"));
        Assert.AreEqual("color: blue;", other.GetAttribute("style"));
    }

    [TestMethod]
    public void BitAiConversationListShouldRespectClassAndStyle()
    {
        var component = RenderComponent<BitAiConversationList>(p => p
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: tomato;"));

        var root = component.Find(".bit-aicvl");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "color: tomato;");
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiConversationListShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitAiConversationList>(p => p.Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aicvl").GetAttribute("style");

        if (expectedStyle.Length == 0)
        {
            Assert.IsTrue(style is null || style.Contains("visibility:hidden") is false);
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "bit-rtl")]
    [DataRow(BitDir.Ltr, null)]
    public void BitAiConversationListShouldRespectDir(BitDir dir, string? expectedClass)
    {
        var component = RenderComponent<BitAiConversationList>(p => p.Add(c => c.Dir, dir));

        var root = component.Find(".bit-aicvl");

        Assert.AreEqual(dir.ToString().ToLower(), root.GetAttribute("dir"));
        Assert.AreEqual(expectedClass is not null, root.ClassList.Contains("bit-rtl"));
    }
}
