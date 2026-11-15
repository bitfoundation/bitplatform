using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AvatarGroup;

[TestClass]
public class BitAvatarGroupTests : BunitTestContext
{
    private static List<BitAvatarGroupItem> People() =>
    [
        new() { Key = "a", Text = "Annie Lindqvist", Presence = BitPersonaPresence.Online },
        new() { Key = "b", Text = "Aaron Reid", ImageUrl = "/b.png" },
        new() { Key = "c", Text = "Alex Lundberg", Title = "Alex (lead)" },
        new() { Key = "d", Text = "Roko Kolar" },
        new() { Key = "e", Text = "Christian Bergqvist" },
    ];

    private IRenderedComponent<BitAvatarGroup> Render(Action<ComponentParameterCollectionBuilder<BitAvatarGroup>>? more = null, List<BitAvatarGroupItem>? items = null)
    {
        return RenderComponent<BitAvatarGroup>(parameters =>
        {
            parameters.Add(p => p.Items, items ?? People());
            more?.Invoke(parameters);
        });
    }

    private static List<AngleSharp.Dom.IElement> Avatars(IRenderedComponent<BitAvatarGroup> com) => com.FindAll(".bit-avg > .bit-avg-itm").ToList();

    private static AngleSharp.Dom.IElement? Overflow(IRenderedComponent<BitAvatarGroup> com) => com.FindAll(".bit-avg > .bit-avg-ovf").SingleOrDefault();

    [TestMethod]
    public void BitAvatarGroupShouldRenderANamedGroupOfCoinOnlyPersonas()
    {
        var com = Render(p => p.Add(x => x.AriaLabel, "Team"));

        var root = com.Find(".bit-avg");

        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.AreEqual("Team", root.GetAttribute("aria-label"));
        Assert.IsTrue(root.ClassList.Contains("bit-avg-s32"));
        Assert.IsFalse(root.ClassList.Contains("bit-avg-spr"));

        var avatars = Avatars(com);

        Assert.AreEqual(5, avatars.Count);
        Assert.IsNull(Overflow(com));

        // Each persona shows its coin only, and is named after its person - with its presence - as the picture it is.
        Assert.IsTrue(avatars.All(a => a.ClassList.Contains("bit-prs-s32")));
        Assert.AreEqual(0, com.FindAll(".bit-prs-ptx").Count);
        Assert.AreEqual("img", avatars[0].GetAttribute("role"));
        StringAssert.StartsWith(avatars[0].GetAttribute("aria-label"), "Annie Lindqvist, ");
        Assert.AreEqual("Aaron Reid", avatars[1].GetAttribute("aria-label"));
        Assert.AreEqual("AL", avatars[0].QuerySelector(".bit-prs-ini")!.TextContent);
        Assert.IsNotNull(avatars[1].QuerySelector("img[src='/b.png']"));

        // The tooltip is the title of the item, or its name.
        Assert.AreEqual("Annie Lindqvist", avatars[0].GetAttribute("title"));
        Assert.AreEqual("Alex (lead)", avatars[2].GetAttribute("title"));

        // Nothing to do with a click, so nothing is a control.
        Assert.AreEqual(0, com.FindAll("button, a").Count);
    }

    [TestMethod]
    public void BitAvatarGroupShouldCountTheAvatarsPastMaxDisplayedInTheOverflowCoin()
    {
        var com = Render(p => p.Add(x => x.MaxDisplayed, 3));

        var avatars = Avatars(com);

        CollectionAssert.AreEqual(new[] { "Annie Lindqvist", "Aaron Reid", "Alex (lead)" }, avatars.Select(a => a.GetAttribute("title")).ToArray());

        var overflow = Overflow(com)!;

        Assert.AreSame(overflow, com.Find(".bit-avg").LastElementChild);
        Assert.AreEqual("+2", overflow.QuerySelector(".bit-prs-ini")!.TextContent);
        Assert.AreEqual("img", overflow.GetAttribute("role"));
        Assert.AreEqual("2 more: Roko Kolar, Christian Bergqvist", overflow.GetAttribute("aria-label"));
        Assert.AreEqual("Roko Kolar, Christian Bergqvist", overflow.GetAttribute("title"));
        Assert.IsTrue(overflow.ClassList.Contains("bit-prs-s32"));
    }

    [TestMethod]
    [DataRow(5)]
    [DataRow(9)]
    public void BitAvatarGroupShouldDrawEveryAvatarWhenThereAreNoMoreThanMaxDisplayed(int max)
    {
        var com = Render(p => p.Add(x => x.MaxDisplayed, max));

        Assert.AreEqual(5, Avatars(com).Count);
        Assert.IsNull(Overflow(com));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    public void BitAvatarGroupWithNoRoomShouldCountEveryAvatarInTheOverflowCoin(int max)
    {
        var com = Render(p => p.Add(x => x.MaxDisplayed, max));

        Assert.AreEqual(0, Avatars(com).Count);
        Assert.AreEqual("+5", Overflow(com)!.QuerySelector(".bit-prs-ini")!.TextContent);
    }

    [TestMethod]
    public void BitAvatarGroupShouldNameTheOverflowCoinWithTheFormatItIsGiven()
    {
        var com = Render(p =>
        {
            p.Add(x => x.MaxDisplayed, 4);
            p.Add(x => x.OverflowAriaLabelFormat, "and {0} other ({1})");
        });

        Assert.AreEqual("and 1 other (Christian Bergqvist)", Overflow(com)!.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitAvatarGroupWithOnItemClickShouldTurnEveryAvatarIntoAButton()
    {
        var clicked = new List<BitAvatarGroupItem>();
        var items = People();

        var com = Render(p => p.Add(x => x.OnItemClick, (BitAvatarGroupItem item) => clicked.Add(item)), items);

        var buttons = com.FindAll(".bit-avg-itm button.bit-prs-cne");

        Assert.AreEqual(5, buttons.Count);
        Assert.AreEqual("Aaron Reid", buttons[1].GetAttribute("aria-label"));

        // A coin that is a button carries no overlay to reveal, which would announce it as "Edit image".
        Assert.AreEqual(0, com.FindAll(".bit-prs-imo").Count);

        buttons[1].Click();
        buttons[3].Click();

        CollectionAssert.AreEqual(new[] { items[1], items[3] }, clicked);
    }

    [TestMethod]
    public void BitAvatarGroupShouldMakeAnItemWithAnHrefALink_UnlessItIsDisabled()
    {
        var com = Render(items:
        [
            new() { Text = "Annie Lindqvist", Href = "/people/annie", Target = "_blank" },
            new() { Text = "Aaron Reid", Href = "/people/aaron", IsDisabled = true },
        ]);

        var links = com.FindAll(".bit-avg-itm a");

        Assert.AreEqual(2, links.Count);
        Assert.AreEqual("/people/annie", links[0].GetAttribute("href"));
        Assert.AreEqual("_blank", links[0].GetAttribute("target"));
        Assert.AreEqual("Annie Lindqvist, AL", links[0].GetAttribute("aria-label"));
        Assert.IsNull(links[1].GetAttribute("href"));
        Assert.AreEqual("true", links[1].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitAvatarGroupWithOnOverflowClickShouldTurnTheOverflowCoinIntoAButtonThatHandsOverTheHiddenItems()
    {
        IReadOnlyList<BitAvatarGroupItem>? hidden = null;
        var items = People();

        var com = Render(p =>
        {
            p.Add(x => x.MaxDisplayed, 2);
            p.Add(x => x.OnOverflowClick, (IReadOnlyList<BitAvatarGroupItem> h) => hidden = h);
        }, items);

        // The avatars themselves stay pictures.
        Assert.AreEqual(1, com.FindAll("button").Count);

        var button = Overflow(com)!.QuerySelector("button")!;

        Assert.AreEqual("3 more: Alex Lundberg, Roko Kolar, Christian Bergqvist", button.GetAttribute("aria-label"));

        button.Click();

        CollectionAssert.AreEqual(items.Skip(2).ToArray(), hidden!.ToArray());
    }

    [TestMethod]
    public void DisabledBitAvatarGroupShouldDisableEveryAvatarAndTheOverflowCoin()
    {
        var com = Render(p =>
        {
            p.Add(x => x.Disabled, true);
            p.Add(x => x.MaxDisplayed, 2);
            p.Add(x => x.OnItemClick, (BitAvatarGroupItem _) => { });
            p.Add(x => x.OnOverflowClick, (IReadOnlyList<BitAvatarGroupItem> _) => { });
        });

        var buttons = com.FindAll("button");

        Assert.AreEqual(3, buttons.Count);
        Assert.IsTrue(buttons.All(b => b.HasAttribute("disabled") && b.GetAttribute("aria-disabled") == "true"));
        Assert.IsTrue(com.Find(".bit-avg").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    [DataRow(BitPersonaSize.Size24, "24")]
    [DataRow(BitPersonaSize.Size48, "48")]
    [DataRow(BitPersonaSize.Size120, "120")]
    [DataRow(BitPersonaSize.Size8, "24")]
    public void BitAvatarGroupShouldDrawItsAvatarsInItsSize(BitPersonaSize size, string drawn)
    {
        var com = Render(p =>
        {
            p.Add(x => x.Size, size);
            p.Add(x => x.MaxDisplayed, 3);
        });

        Assert.IsTrue(com.Find(".bit-avg").ClassList.Contains($"bit-avg-s{drawn}"));
        Assert.IsTrue(com.FindAll(".bit-avg > .bit-prs").All(a => a.ClassList.Contains($"bit-prs-s{drawn}")));

        // Every one of them still has its coin, the smallest size included.
        Assert.AreEqual(4, com.FindAll(".bit-prs-imc").Count);
    }

    [TestMethod]
    public void SpreadBitAvatarGroupShouldCarryItsClass()
    {
        var com = Render(p => p.Add(x => x.Spread, true));

        Assert.IsTrue(com.Find(".bit-avg").ClassList.Contains("bit-avg-spr"));
    }

    [TestMethod]
    public void BitAvatarGroupShouldGiveTheItemsTheirCoinColorsAndPickTheOthers()
    {
        var com = Render(items:
        [
            new() { Text = "Annie Lindqvist", CoinColor = BitColor.Error },
            new() { Text = "Aaron Reid" },
        ]);

        var avatars = Avatars(com);

        Assert.IsTrue(avatars[0].ClassList.Contains("bit-prs-err"));

        // A coin without a color of its own is given one picked from its name, so neighbours tell apart: the class a
        // persona of the same name is given by AutoCoinColor, and a plain one is not.
        string[] Classes(bool auto) => RenderComponent<BitPersona>(p =>
        {
            p.Add(x => x.PrimaryText, "Aaron Reid");
            p.Add(x => x.AutoCoinColor, auto);
        }).Find(".bit-prs").ClassList.ToArray();

        var picked = Classes(auto: true).Except(Classes(auto: false)).ToArray();

        Assert.AreEqual(1, picked.Length);
        Assert.IsTrue(avatars[1].ClassList.Contains(picked[0]));
    }

    [TestMethod]
    public void BitAvatarGroupShouldApplyTheClassesAndStylesOfItsPartsAndItems()
    {
        var com = Render(p =>
        {
            p.Add(x => x.MaxDisplayed, 1);
            p.Add(x => x.Classes, new BitAvatarGroupClassStyles { Root = "c-root", Item = "c-item", Overflow = "c-overflow" });
            p.Add(x => x.Styles, new BitAvatarGroupClassStyles { Root = "margin:1px", Item = "margin:2px", Overflow = "margin:3px" });
        }, [new() { Text = "Annie Lindqvist", Class = "own", Style = "padding:4px" }, new() { Text = "Aaron Reid" }]);

        var root = com.Find(".bit-avg");
        var avatar = Avatars(com).Single();
        var overflow = Overflow(com)!;

        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");

        Assert.IsTrue(avatar.ClassList.Contains("c-item"));
        Assert.IsTrue(avatar.ClassList.Contains("own"));
        StringAssert.Contains(avatar.GetAttribute("style"), "margin:2px");
        StringAssert.Contains(avatar.GetAttribute("style"), "padding:4px");

        Assert.IsTrue(overflow.ClassList.Contains("c-overflow"));
        Assert.IsFalse(overflow.ClassList.Contains("c-item"));
        StringAssert.Contains(overflow.GetAttribute("style"), "margin:3px");
    }

    [TestMethod]
    public void BitAvatarGroupShouldKeepTheAvatarsOfTheItemsItKeeps()
    {
        var items = People();
        var com = Render(items: items);

        var aaron = com.FindComponents<BitPersona>()[1].Instance;

        com.Render(p => p.Add(x => x.Items, items.Skip(1).ToList()));

        var personas = com.FindComponents<BitPersona>();

        Assert.AreEqual(4, personas.Count);
        Assert.AreSame(aaron, personas[0].Instance, "the avatar of a kept item is the same persona, keyed by the item's Key");
    }
}
