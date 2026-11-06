using System.Linq;
using Bit.BlazorUI.Demo.Client.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// The ranking behind the demo site's search boxes - the header's and the components gallery's, which
/// <c>ComponentCatalog.Search</c> orders by <see cref="ComponentCatalogItem.Relevance"/>: 0-2 for the name, 3-5 for an
/// alias (whole, prefix, inside), 6 for a keyword, 7 for anything else the item says. The items here are built the way
/// the catalog builds them, with the names, aliases and keywords of their nav entries.
/// </summary>
[TestClass]
public class ComponentCatalogItemTests
{
    private static readonly ComponentCatalogItem[] Items =
    [
        Item("Header", "Layouts", aliases: "AppBar, TopBar"),
        Item("NavBar", "Navs", aliases: "NavMenu, TabPanel, TabBar, BottomNavigation"),
        Item("SearchBox", "Inputs", aliases: "AutoComplete, Typeahead"),
        Item("DatePicker", "Pickers", aliases: "MonthPicker, YearPicker, DateTimePicker"),
        Item("Button", "Buttons", aliases: "CompoundButton, IconButton, LoadingButton", keywords: "FAB, Floating Action Button, Submit"),
        Item("AiChat", "AI", aliases: "Chatbot, Assistant, Copilot", keywords: "Chat UI, Conversation, LLM, GPT, Claude, OpenAI, Streaming, Messages, Composer"),
    ];

    /// <summary>
    /// A name or an alias is one word, which a reader types as the words it is made of as often as not. Those are the
    /// same name, in the same tier as the word typed whole: compared as typed, "search box" and "app bar" found nothing.
    /// </summary>
    [TestMethod]
    [DataRow("searchbox", "SearchBox", 0)]
    [DataRow("search box", "SearchBox", 0)]
    [DataRow("date picker", "DatePicker", 0)]
    [DataRow("date-picker", "DatePicker", 0)]
    [DataRow("ai chat", "AiChat", 0)]
    [DataRow("date", "DatePicker", 1)]
    [DataRow("appbar", "Header", 3)]
    [DataRow("app bar", "Header", 3)]
    [DataRow("top bar", "Header", 3)]
    [DataRow("tab bar", "NavBar", 3)]
    [DataRow("loading button", "Button", 3)]
    [DataRow("date time", "DatePicker", 4)]
    public void A_name_typed_as_its_words_matches_as_that_name(string term, string name, int expected)
    {
        Assert.AreEqual(expected, Find(name).Relevance(term), $"'{term}' against {name}.");
    }

    /// <summary>
    /// "bar" alone is inside NavBar's own name, which outranks its being inside BitHeader's AppBar; "app bar" is the
    /// whole of AppBar, which NavBar's name does not hold, so it is BitHeader's.
    /// </summary>
    [TestMethod]
    public void Words_spelling_an_alias_rank_it_above_a_name_holding_one_of_them()
    {
        var header = Find("Header");
        var navBar = Find("NavBar");

        Assert.IsLessThan(header.Relevance("bar"), navBar.Relevance("bar"), "'bar' should rank NavBar first.");
        Assert.IsLessThan(navBar.Relevance("app bar"), header.Relevance("app bar"), "'app bar' should rank Header first.");
    }

    /// <summary>
    /// The keywords and the rest of an item hold phrases, and are compared with the term as typed - a phrase still
    /// finds them - while a term with nothing but separators in it, empty once they are left out, matches no name.
    /// </summary>
    [TestMethod]
    public void The_tiers_holding_phrases_compare_the_term_as_typed()
    {
        Assert.AreEqual(6, Find("Button").Relevance("floating action button"), "A keyword phrase is no longer found.");

        foreach (var item in Items)
        {
            Assert.AreEqual(ComponentCatalogItem.NoMatch, item.Relevance("-"), $"A term of separators alone matches {item.Name}.");
        }
    }

    private static ComponentCatalogItem Find(string name) => Items.Single(item => item.Name == name);

    private static ComponentCatalogItem Item(string name, string category, string? aliases = null, string? keywords = null) => new()
    {
        Name = name,
        Url = $"/components/{name.ToLowerInvariant()}",
        Category = category,
        Aliases = aliases,
        Keywords = keywords,
        IconName = "Puzzle",
        Package = "Bit.BlazorUI",
        // As ComponentCatalog builds it, less the one-line summary.
        SearchText = $"{name} {category} {aliases} {keywords}".ToLowerInvariant()
    };
}
