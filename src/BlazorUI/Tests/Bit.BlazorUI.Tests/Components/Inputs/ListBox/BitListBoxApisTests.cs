using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ListBox;

/// <summary>
/// The two other ways a BitListBox is given its items, besides BitListBoxItem objects: objects of a custom class read
/// through NameSelectors (by name or by selector), and BitListBoxOption components declared in markup.
/// </summary>
[TestClass]
public class BitListBoxApisTests : BunitTestContext
{
    private const string ChildrenAttributes = "BitBlazorUI.Utils.getChildrenAttributes";

    public class Fruit
    {
        public string? Name { get; set; }
        public int Code { get; set; }
        public string? Note { get; set; }
        public string? Glyph { get; set; }
        public bool OutOfSeason { get; set; }
        public string? Id { get; set; }
        public string? Css { get; set; }
        public string? Inline { get; set; }
        public string? Tooltip { get; set; }
        public string? Label { get; set; }
    }

    private static readonly BitListBoxNameSelectors<Fruit, int> ByName = new()
    {
        Text = { Name = nameof(Fruit.Name) },
        Value = { Name = nameof(Fruit.Code) },
        Description = { Name = nameof(Fruit.Note) },
        IconName = { Name = nameof(Fruit.Glyph) },
        IsDisabled = { Name = nameof(Fruit.OutOfSeason) },
        Key = { Name = nameof(Fruit.Id) },
        Class = { Name = nameof(Fruit.Css) },
        Style = { Name = nameof(Fruit.Inline) },
        Title = { Name = nameof(Fruit.Tooltip) },
        AriaLabel = { Name = nameof(Fruit.Label) },
    };

    private static List<Fruit> Fruits() =>
    [
        new() { Name = "Apple", Code = 1, Note = "Red or green", Glyph = "Heart", Id = "apple", Css = "apple-class", Inline = "margin:1px", Tooltip = "An apple a day" },
        new() { Name = "Banana", Code = 2, OutOfSeason = true },
        new() { Name = "Cherry", Code = 3, Label = "Cherry, sweet" }
    ];

    private static string[] Texts<T>(IRenderedComponent<T> com) where T : IComponent =>
        com.FindAll(".bit-lbx-txt").Select(t => t.TextContent).ToArray();

    [TestMethod]
    public void BitListBoxShouldReadTheItemsOfACustomClassByTheNamesOfItsMembers()
    {
        int bound = 0;

        var com = RenderComponent<BitListBox<Fruit, int>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.NameSelectors, ByName);
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        CollectionAssert.AreEqual(new[] { "Apple", "Banana", "Cherry" }, Texts(com));

        var items = com.FindAll("li");

        Assert.AreEqual("Red or green", items[0].QuerySelector(".bit-lbx-dsc")!.TextContent);
        Assert.IsTrue(items[0].QuerySelector(".bit-lbx-ico")!.ClassList.Contains("bit-icon--Heart"));
        Assert.IsTrue(items[0].ClassList.Contains("apple-class"));
        Assert.AreEqual("margin:1px", items[0].GetAttribute("style"));
        Assert.AreEqual("An apple a day", items[0].GetAttribute("title"));
        Assert.AreEqual("true", items[1].GetAttribute("aria-disabled"));
        Assert.AreEqual("Cherry, sweet", items[2].GetAttribute("aria-label"));

        // The value is read through its name too, and the disabled Banana is passed by.
        items[2].Click();
        Assert.AreEqual(3, bound);

        com.Find("ul").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual(1, bound);
    }

    [TestMethod]
    public void BitListBoxShouldReadTheItemsOfACustomClassBySelectors()
    {
        IEnumerable<int>? bound = null;

        var selectors = new BitListBoxNameSelectors<Fruit, int>
        {
            Text = { Selector = f => f.Name?.ToUpperInvariant() },
            Value = { Selector = f => f.Code * 10 },
            IsDisabled = { Selector = f => f.OutOfSeason },
        };

        var com = RenderComponent<BitListBox<Fruit, int>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.NameSelectors, selectors);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Bind(p => p.Values, bound, v => bound = v);
        });

        CollectionAssert.AreEqual(new[] { "APPLE", "BANANA", "CHERRY" }, Texts(com));

        com.FindAll("li")[0].Click();
        com.FindAll("li")[1].Click();
        com.FindAll("li")[2].Click();

        CollectionAssert.AreEqual(new[] { 10, 30 }, bound!.ToArray());
    }

    [TestMethod]
    public void BitListBoxShouldReadNothingOfACustomClassWithoutNameSelectors()
    {
        var com = RenderComponent<BitListBox<Fruit, int>>(parameters => parameters.Add(p => p.Items, Fruits()));

        // The items are there, but with nothing to read their text by.
        Assert.AreEqual(3, com.FindAll("li[role=option]").Count);
        Assert.IsTrue(Texts(com).All(t => t.Length == 0));
    }

    [TestMethod]
    public void BitListBoxShouldDrawTheItemsOfItsOptions()
    {
        var com = RenderComponent<BitListBoxOptionsTest>(parameters => parameters.Add(p => p.Value, "cherry"));

        CollectionAssert.AreEqual(new[] { "Apple", "Cherry" }, Texts(com));

        var items = com.FindAll("li[role=option]");

        Assert.AreEqual("Red or green", items[0].QuerySelector(".bit-lbx-dsc")!.TextContent);
        Assert.IsTrue(items[0].QuerySelector(".bit-lbx-ico")!.ClassList.Contains("bit-icon--Heart"));
        Assert.AreEqual("true", items[1].GetAttribute("aria-selected"));
        Assert.IsTrue(items[1].ClassList.Contains("last-class"));
        Assert.StartsWith("margin:1px", items[1].GetAttribute("style")!);
        Assert.AreEqual("The last one", items[1].GetAttribute("title"));

        // The options render a marker each, outside the list and hidden from everyone.
        var holder = com.Find("[data-bit-lbx-opt]").ParentElement!;

        Assert.IsTrue(holder.HasAttribute("hidden"));
        Assert.AreEqual("true", holder.GetAttribute("aria-hidden"));
        Assert.AreEqual(2, holder.QuerySelectorAll("[data-bit-lbx-opt]").Length);
        Assert.AreEqual(0, com.FindAll("ul [data-bit-lbx-opt]").Count);
    }

    [TestMethod]
    public void BitListBoxShouldSelectTheValueOfAnOption()
    {
        var com = RenderComponent<BitListBoxOptionsTest>();

        com.FindAll("li[role=option]")[0].Click();

        Assert.AreEqual("apple", com.Instance.Value);
        Assert.AreEqual("true", com.FindAll("li[role=option]")[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitListBoxShouldFollowTheParametersOfItsOptions()
    {
        var com = RenderComponent<BitListBoxOptionsTest>();

        com.Render(parameters =>
        {
            parameters.Add(p => p.LastText, "Sour cherry");
            parameters.Add(p => p.LastAriaLabel, "Cherry, sour");
        });

        CollectionAssert.AreEqual(new[] { "Apple", "Sour cherry" }, Texts(com));
        Assert.AreEqual("Cherry, sour", com.FindAll("li[role=option]")[1].GetAttribute("aria-label"));

        com.Render(parameters => parameters.Add(p => p.LastIsDisabled, true));

        Assert.AreEqual("true", com.FindAll("li[role=option]")[1].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitListBoxShouldFollowAnOptionThatChangesWithoutTheListBoxRendering()
    {
        RenderFragment options = builder =>
        {
            builder.OpenComponent<BitListBoxOptionHost>(0);
            builder.CloseComponent();
        };

        var com = RenderComponent<BitListBox<BitListBoxOption<string>, string>>(parameters => parameters.Add(p => p.Options, options));

        CollectionAssert.AreEqual(new[] { "Own option" }, Texts(com));

        // Only the host of the option renders: the option tells the list box, which draws the item.
        var host = com.FindComponent<BitListBoxOptionHost>();
        host.InvokeAsync(() => host.Instance.SetText("Renamed"));

        com.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "Renamed" }, Texts(com)));
    }

    [TestMethod]
    public void BitListBoxShouldKeepTheOrderOfTheMarkupWhenAnOptionIsAddedConditionally()
    {
        // The list box reads the order of the option markers in the document through JS, which the test answers with
        // the markers of the rendered markup, once the conditional option is rendered.
        var handler = Context.JSInterop.Setup<string[]>(ChildrenAttributes, _ => true);

        var com = RenderComponent<BitListBoxOptionsTest>(parameters => parameters.Add(p => p.Value, "cherry"));

        com.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        handler.SetResult(MarkerIds(com));

        com.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "Apple", "Banana", "Cherry" }, Texts(com)));

        // The selection and the tab stop stay with Cherry, now the third item.
        Assert.AreEqual("true", com.FindAll("li[role=option]")[2].GetAttribute("aria-selected"));
        Assert.AreEqual("0", com.FindAll("li[role=option]")[2].GetAttribute("tabindex"));

        com.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        handler.SetResult(MarkerIds(com));

        com.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "Apple", "Cherry" }, Texts(com)));
    }

    [TestMethod]
    public void BitListBoxWithItemsShouldReadNoOrder()
    {
        RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
            parameters.Add(p => p.Items, new List<BitListBoxItem<string>> { new() { Text = "One", Value = "1" } }));

        Assert.AreEqual(0, Context.JSInterop.Invocations[ChildrenAttributes].Count);
    }

    [TestMethod]
    public void BitListBoxWithOptionsShouldIgnoreItems()
    {
        RenderFragment options = builder =>
        {
            builder.OpenComponent<BitListBoxOption<string>>(0);
            builder.AddAttribute(1, nameof(BitListBoxOption<string>.Text), "From the markup");
            builder.AddAttribute(2, nameof(BitListBoxOption<string>.Value), "markup");
            builder.CloseComponent();
        };

        var com = RenderComponent<BitListBox<BitListBoxOption<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitListBoxOption<string>> { new() { Text = "From Items" } });
            parameters.Add(p => p.Options, options);
        });

        CollectionAssert.AreEqual(new[] { "From the markup" }, Texts(com));

        com.Render(parameters => parameters.Add(p => p.Items, new List<BitListBoxOption<string>> { new() { Text = "New Items" } }));

        CollectionAssert.AreEqual(new[] { "From the markup" }, Texts(com));
    }

    [TestMethod]
    public void BitListBoxShouldRefuseOptionsOfAnotherValueType()
    {
        // An option finds its list box through a cascade typed by its own value type, so options of another one would
        // never reach it: the list box says so rather than render an empty list.
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitListBox<BitListBoxOption<int>, string>>());

        StringAssert.Contains(exception.Message, "BitListBoxOption");
    }

    private static string[] MarkerIds<T>(IRenderedComponent<T> com) where T : IComponent =>
        com.FindAll("[data-bit-lbx-opt]").Select(e => e.GetAttribute("data-bit-lbx-opt")!).ToArray();
}
