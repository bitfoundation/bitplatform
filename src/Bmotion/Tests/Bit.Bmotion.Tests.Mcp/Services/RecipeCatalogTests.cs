using System.Text.RegularExpressions;
using Bit.Bmotion.Demo.Client.Shared;

namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The recipes: the fastest route from a request to correct code, and therefore the code an agent
/// is most likely to paste unread.
/// <para>
/// That makes one test here worth more than the rest put together - every recipe is run through
/// the server's own code review. A recipe that trips the review teaches the mistake the review
/// exists to catch, and does it with the server's full authority.
/// </para>
/// </summary>
[TestClass]
public class RecipeCatalogTests
{
    [TestMethod]
    public void All_EveryRecipe_IsCompleteAndIdentifiable()
    {
        Assert.IsTrue(BmotionRecipeCatalog.All.Length >= 10, $"Only {BmotionRecipeCatalog.All.Length} recipes.");

        foreach (var recipe in BmotionRecipeCatalog.All)
        {
            Assert.IsTrue(Regex.IsMatch(recipe.Id, "^[a-z0-9]+(-[a-z0-9]+)*$"),
                          $"'{recipe.Id}' is not a kebab-case id an agent can be told to type.");
            Assert.AreNotEqual(string.Empty, recipe.Title.Trim());
            Assert.AreNotEqual(string.Empty, recipe.Intent.Trim());
            Assert.AreNotEqual(string.Empty, recipe.Keywords.Trim());

            Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Code), $"'{recipe.Id}' has no code.");
            // The caveat is the half of a recipe the code cannot show, and the reason a recipe beats
            // the API reference for this question at all.
            Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Notes), $"'{recipe.Id}' carries no caveat.");
        }
    }

    [TestMethod]
    public void All_TheIdsAreUnique()
    {
        var ids = BmotionRecipeCatalog.All.Select(recipe => recipe.Id).ToArray();

        CollectionAssert.AreEquivalent(ids.Distinct().ToArray(), ids);
    }

    /// <summary>
    /// The recipes are markup an agent will paste. Running them through the server's own review is
    /// the strongest statement the two halves of this server agree with each other.
    /// </summary>
    [TestMethod]
    public void All_EveryRecipe_SurvivesTheServersOwnCodeReview()
    {
        var failures = new List<string>();

        foreach (var recipe in BmotionRecipeCatalog.All)
        {
            var review = BmotionCodeReview.Review(recipe.Code);

            foreach (var finding in review.Findings.Where(finding => finding.Severity != "Suggestion"))
            {
                failures.Add($"{recipe.Id} line {finding.Line}: [{finding.Severity}] {finding.Rule}");
            }
        }

        Assert.AreEqual(0, failures.Count,
                        $"Recipes the server would itself reject:\n{string.Join("\n", failures)}");
    }

    /// <summary>
    /// Every transition written into a recipe has to be one the measuring tools can read back - the
    /// prompts tell an agent to simulate the transition it is about to use, and it will use this one.
    /// </summary>
    [TestMethod]
    public async Task All_TheTransitionsInTheRecipes_AreReadableByTheMeasuringTools()
    {
        var calls = BmotionRecipeCatalog.All
            .SelectMany(recipe => Regex.Matches(recipe.Code ?? string.Empty, @"Bm\.(Spring|Tween|Inertia)\([^)]*\)")
                                       .Select(match => (recipe.Id, Spec: match.Value)))
            .ToArray();

        Assert.AreNotEqual(0, calls.Length, "No recipe writes a transition, which cannot be right.");

        foreach (var (id, spec) in calls)
        {
            var result = await BmotionMotionLab.SimulateAsync(spec);

            Assert.IsNull(result.Error, $"'{id}' uses '{spec}', which the simulator cannot read: {result.Error}");
        }
    }

    /// <summary>
    /// SeeAlso is handed to an agent as the demo page that shows the recipe running. A route that
    /// does not exist is a dead end it cannot tell from a working one.
    /// </summary>
    [TestMethod]
    public void All_TheSeeAlsoLinks_PointAtRealDemoPages()
    {
        var routes = NavItem.All.Select(page => $"/{page.Href}").ToArray();

        foreach (var recipe in BmotionRecipeCatalog.All.Where(recipe => recipe.SeeAlso is not null))
        {
            CollectionAssert.Contains(routes, recipe.SeeAlso,
                                      $"'{recipe.Id}' points at '{recipe.SeeAlso}', which is not a page of this demo.");
        }
    }

    /// <summary>
    /// The listing is fetched to choose an id from; carrying every recipe's full markup in it would
    /// spend a client's context before it has chosen anything.
    /// </summary>
    [TestMethod]
    public void Summaries_LeaveOutTheBodyAndKeepTheIdentity()
    {
        var summaries = BmotionRecipeCatalog.Summaries;

        Assert.AreEqual(BmotionRecipeCatalog.All.Length, summaries.Length);

        foreach (var summary in summaries)
        {
            Assert.IsNull(summary.Code, $"'{summary.Id}' carries its code into the listing.");
            Assert.IsNull(summary.Notes, $"'{summary.Id}' carries its notes into the listing.");
            Assert.AreNotEqual(string.Empty, summary.Id);
            Assert.AreNotEqual(string.Empty, summary.Intent);
        }
    }

    [TestMethod]
    public void Summaries_AreASnapshot_NotTheCatalogItself()
    {
        // The listing is projected per call, so a caller mutating what it got cannot reach the
        // catalog every other caller reads.
        var first = BmotionRecipeCatalog.Summaries;
        var second = BmotionRecipeCatalog.Summaries;

        Assert.AreNotSame(first, second);
        Assert.AreNotSame(first[0], second[0]);

        // And stripping the code for the listing did not strip it from the catalog itself.
        Assert.IsNotNull(BmotionRecipeCatalog.All[0].Code);
    }

    [TestMethod]
    public void Find_MatchesTheIdExactly_First()
    {
        foreach (var recipe in BmotionRecipeCatalog.All)
        {
            Assert.AreEqual(recipe.Id, BmotionRecipeCatalog.Find(recipe.Id)?.Id);
            Assert.AreEqual(recipe.Id, BmotionRecipeCatalog.Find(recipe.Id.ToUpperInvariant())?.Id);
            Assert.AreEqual(recipe.Id, BmotionRecipeCatalog.Find($"  {recipe.Id}  ")?.Id);
        }
    }

    [TestMethod]
    [DataRow("modal", "modal-dialog")]
    [DataRow("stagger", "staggered-list")]
    [DataRow("exit", "exit-animation")]
    public void Find_MatchesLoosely_SoAHalfRememberedNameStillLands(string typed, string expected)
    {
        Assert.AreEqual(expected, BmotionRecipeCatalog.Find(typed)?.Id);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    [DataRow("a recipe for disaster")]
    public void Find_WhatIsNotARecipe_IsNull(string? id)
    {
        Assert.IsNull(BmotionRecipeCatalog.Find(id));
    }

    /// <summary>
    /// A recipe whose code the agent must not simply lift - one that only works on WebAssembly -
    /// has to say so in the notes rather than leaving it to be discovered in production.
    /// </summary>
    [TestMethod]
    [DataRow("layout-animation")]
    [DataRow("drag-with-constraints")]
    public void All_TheRecipesThatNeedTheFrameLoop_SayWhereTheyStopWorking(string id)
    {
        var recipe = BmotionRecipeCatalog.Find(id);

        Assert.IsNotNull(recipe, $"'{id}' is no longer a recipe.");
        Assert.IsTrue(recipe.Notes!.Contains("Server", StringComparison.OrdinalIgnoreCase) ||
                      recipe.Notes.Contains("WebAssembly", StringComparison.OrdinalIgnoreCase),
                      $"'{id}' does not say which render modes it survives: {recipe.Notes}");
    }

    /// <summary>An exit animation without its presence component is the mistake the catalog exists to prevent.</summary>
    [TestMethod]
    public void All_TheExitRecipes_ShowTheirPresenceComponent()
    {
        foreach (var recipe in BmotionRecipeCatalog.All.Where(recipe => recipe.Code!.Contains("Exit=", StringComparison.Ordinal)))
        {
            Assert.IsTrue(recipe.Code!.Contains("BmotionAnimatePresence", StringComparison.Ordinal) ||
                          recipe.Code.Contains("BmotionPresenceGroup", StringComparison.Ordinal) ||
                          recipe.Code.Contains("BmotionPresenceSwitch", StringComparison.Ordinal),
                          $"'{recipe.Id}' animates an exit with nothing to hold the element in the DOM.");
        }
    }
}
