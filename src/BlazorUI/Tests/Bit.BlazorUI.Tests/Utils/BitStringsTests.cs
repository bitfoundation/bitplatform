using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// The words the components fall back on (BitStrings): one translated object reaches every component, a parameter
/// still wins over it, and AddBitBlazorUIServices sets it. Current is process-wide, so every test puts it back.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class BitStringsTests : BunitTestContext
{
    private sealed class GermanStrings : BitStrings
    {
        public override string IncreaseValue => "Erhöhen";
        public override string DecreaseValue => "Verringern";
        public override string ClearValue => "Wert löschen";
        public override string Loading => "Wird geladen";
        public override string InvalidInput => "Ungültige Eingabe";
        public override string ClearText => "Text löschen";
        public override string Breadcrumb => "Brotkrümelpfad";
        public override string Pagination => "Seitennavigation";
        public override string PageFormat => "Seite {0}";
        public override string NoItemsFound => "Keine Einträge";
        public override string NoResultsFound => "Keine Treffer";
    }

    [TestCleanup]
    public void ResetStrings() => BitStrings.Current = null;

    [TestMethod]
    public void CurrentShouldDefaultToTheEnglishStringsAndGoBackToThemOnNull()
    {
        Assert.AreSame(BitStrings.Default, BitStrings.Current);
        Assert.AreEqual("Increase value", BitStrings.Current.IncreaseValue);

        BitStrings.Current = new GermanStrings();
        Assert.AreEqual("Erhöhen", BitStrings.Current.IncreaseValue);
        // A member the subclass leaves alone keeps its English.
        Assert.AreEqual("Clear selection", BitStrings.Current.ClearSelection);

        BitStrings.Current = null;
        Assert.AreSame(BitStrings.Default, BitStrings.Current);
    }

    [TestMethod]
    public void AddBitBlazorUIServicesShouldSetTheStringsAndKeepThemWhenLeftUnset()
    {
        var german = new GermanStrings();

        new ServiceCollection().AddBitBlazorUIServices(o => o.Strings = german);
        Assert.AreSame(german, BitStrings.Current);

        new ServiceCollection().AddBitBlazorUIServices(o => o.TrySingleton = false);
        Assert.AreSame(german, BitStrings.Current);
    }

    [TestMethod]
    public void TranslatedStringsShouldReachTheNumberField()
    {
        BitStrings.Current = new GermanStrings();

        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, 7);
            parameters.Add(p => p.Loading, true);
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder => builder.AddContent(0, "!")));
        });

        Assert.AreEqual("Erhöhen", component.Find(".bit-nfl-aup").GetAttribute("aria-label"));
        Assert.AreEqual("Verringern", component.Find(".bit-nfl-adn").GetAttribute("aria-label"));
        Assert.AreEqual("Wert löschen", component.Find(".bit-nfl-cbt").GetAttribute("aria-label"));
        Assert.AreEqual("Ungültige Eingabe, Wird geladen", component.Find("span[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void AParameterShouldStillWinOverTheStrings()
    {
        BitStrings.Current = new GermanStrings();

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Mode, BitSpinButtonMode.Compact);
            parameters.Add(p => p.IncrementAriaLabel, "Plus");
        });

        Assert.AreEqual("Plus", component.Find(".bit-nfl-aup").GetAttribute("aria-label"));
        Assert.AreEqual("Verringern", component.Find(".bit-nfl-adn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void TranslatedStringsShouldReachOtherComponents()
    {
        BitStrings.Current = new GermanStrings();

        var textField = RenderComponent<BitTextField>(parameters => parameters.Add(p => p.ShowClearButton, true));
        textField.Find(".bit-tfl-inp").Change("hallo");
        Assert.AreEqual("Text löschen", textField.Find(".bit-tfl-cbt").GetAttribute("aria-label"));

        var breadcrumb = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
            parameters.Add(p => p.Items, [new BitBreadcrumbItem { Text = "Start" }]));
        Assert.AreEqual("Brotkrümelpfad", breadcrumb.Find("nav").GetAttribute("aria-label"));

        var pagination = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.DefaultSelectedPage, 3);
        });
        Assert.AreEqual("Seitennavigation", pagination.Find("nav").GetAttribute("aria-label"));
        Assert.AreEqual("Seite 3", pagination.Find(".bit-pgn-sel").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void TranslatedStringsShouldBeWhatEveryEmptyListSays()
    {
        BitStrings.Current = new GermanStrings();

        var basicList = RenderComponent<BitBasicList<int>>(parameters => parameters.Add(p => p.Items, []));
        Assert.AreEqual("Keine Einträge", basicList.Find(".bit-bsl-emp").TextContent.Trim());

        var virtualize = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, []);
            parameters.Add(p => p.ItemTemplate, (RenderFragment<int>)(item => builder => builder.AddContent(0, item)));
        });
        Assert.AreEqual("Keine Einträge", virtualize.Find(".bit-vir-emp").TextContent.Trim());

        var accordionList = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters => parameters.Add(p => p.Items, []));
        Assert.AreEqual("Keine Einträge", accordionList.Find(".bit-acl-emp").TextContent.Trim());

        // The texts object of the grid reads the strings each time it is asked, not once when it is made, so an
        // instance made before the strings were set still says them; a text set on it wins over them.
        var gridStrings = new BitDataGridStrings();
        Assert.AreEqual("Keine Einträge", gridStrings.EmptyText);
        Assert.AreEqual("Keine Treffer", gridStrings.NoMatchesText);
        BitStrings.Current = null;
        Assert.AreEqual("No items found", gridStrings.EmptyText);
        gridStrings.EmptyText = "Leer";
        Assert.AreEqual("Leer", gridStrings.EmptyText);
    }
}