using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// Checklists as task guidance: required and optional tasks, tasks that wait for others, what happens after the user
/// dismisses one, and the mistakes in a definition that are caught before a user meets them.
/// </summary>
[TestClass]
public class ChecklistTaskTests : BunitContext
{
    private const string Id = "setup";

    public ChecklistTaskTests()
    {
        Services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        Services.AddBitBonboardingServices();
        Services.AddSingleton<IBonboardingInterop>(new FakeInterop());
        Services.AddBitBonboardingInMemoryStore();
    }

    private IBonboardingService BonboardingService => Services.GetRequiredService<IBonboardingService>();

    private static BonboardingChecklistDefinition Checklist(params BonboardingChecklistItem[] items) => new() { Id = Id, Title = "Set up", Items = [.. items] };

    private static string[] States(IRenderedComponent<BonboardingChecklist> cut)
        => cut.FindAll(".bit-bonboarding-checklist-item").Select(item => item.GetAttribute("data-bonboarding-state")!).ToArray();

    // ---- Optional items -------------------------------------------------------------

    [TestMethod]
    public async Task Optional_Items_Are_Tagged_And_Left_Out_Of_The_Progress()
    {
        var completed = 0;
        var cut = Render<BonboardingChecklist>(parameters => parameters
            .Add(list => list.OnCompleted, () => completed++)
            .Add(list => list.Checklist, Checklist(
                new BonboardingChecklistItem { Id = "project", Title = "Create a project" },
                new BonboardingChecklistItem { Id = "theme", Title = "Pick a theme", Optional = true })));

        cut.WaitForAssertion(() => Assert.AreEqual("0 of 1 done", cut.Find(".bit-bonboarding-checklist-count").TextContent));
        Assert.AreEqual("1", cut.Find("[role='progressbar']").GetAttribute("aria-valuemax"));

        var optional = cut.Find("[data-bonboarding-item='theme']");
        Assert.AreEqual("true", optional.GetAttribute("data-bonboarding-optional"));
        Assert.AreEqual("Optional", optional.QuerySelector(".bit-bonboarding-checklist-tag")!.TextContent, "Said in words, not only by a style.");

        await BonboardingService.SetChecklistItemAsync(Id, "project");

        cut.WaitForAssertion(() => Assert.AreEqual("1 of 1 done", cut.Find(".bit-bonboarding-checklist-count").TextContent));
        Assert.AreEqual("true", cut.Find(".bit-bonboarding-checklist").GetAttribute("data-bonboarding-complete"), "Complete with the optional item still to do.");
        Assert.IsTrue(cut.Instance.IsComplete);
        Assert.AreEqual(1, completed);
    }

    [TestMethod]
    public void A_Checklist_Of_Optional_Items_Only_Counts_Them_All()
    {
        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "a", Title = "A", Optional = true, IsComplete = _ => ValueTask.FromResult(true) },
            new BonboardingChecklistItem { Id = "b", Title = "B", Optional = true })));

        cut.WaitForAssertion(() => Assert.AreEqual("1 of 2 done", cut.Find(".bit-bonboarding-checklist-count").TextContent));
        Assert.IsFalse(cut.Instance.IsComplete);
    }

    // ---- Items that wait for others ---------------------------------------------------

    [TestMethod]
    public async Task An_Item_Waits_For_The_Items_It_Depends_On()
    {
        var invited = 0;
        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "project", Title = "Create a project" },
            new BonboardingChecklistItem { Id = "invite", Title = "Invite your team", DependsOn = ["project"], OnClick = _ => { invited++; return ValueTask.CompletedTask; } })));

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "todo", "locked" }, States(cut)));

        var locked = cut.Find("[data-bonboarding-item='invite']");
        Assert.IsNull(locked.QuerySelector("button"), "A locked item cannot be pressed.");
        Assert.AreEqual("Do first: Create a project", locked.QuerySelector(".bit-bonboarding-checklist-item-locked")!.TextContent, "It says what to do first.");

        var state = cut.Instance.Items!.Single(item => item.Item.Id == "invite");
        Assert.IsTrue(state.IsLocked);
        Assert.IsFalse(state.IsActionable);
        Assert.AreEqual("project", state.BlockedBy.Single().Id);

        // A custom template calling it anyway does nothing either.
        await cut.InvokeAsync(state.ActivateAsync);
        Assert.AreEqual(0, invited);

        await BonboardingService.SetChecklistItemAsync(Id, "project");

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "done", "todo" }, States(cut)));
        await cut.Find("[data-bonboarding-item='invite'] button").ClickAsync(new());
        Assert.AreEqual(1, invited);
    }

    [TestMethod]
    public async Task A_Locked_Item_Done_Another_Way_Counts_As_Done()
    {
        await BonboardingService.SetChecklistItemAsync(Id, "invite");

        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "project", Title = "Create a project" },
            new BonboardingChecklistItem { Id = "invite", Title = "Invite your team", DependsOn = ["project"] })));

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "todo", "done" }, States(cut)));
        Assert.IsFalse(cut.Instance.Items![1].IsLocked);
    }

    [TestMethod]
    public void An_Item_Hidden_By_Its_Condition_Holds_Nothing_Up()
    {
        var cut = Render<BonboardingChecklist>(parameters => parameters.Add(list => list.Checklist, Checklist(
            new BonboardingChecklistItem { Id = "billing", Title = "Add billing", When = _ => ValueTask.FromResult(false) },
            new BonboardingChecklistItem { Id = "invite", Title = "Invite your team", DependsOn = ["billing"] })));

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "todo" }, States(cut)));
    }

    // ---- Dismissal ----------------------------------------------------------------------

    [TestMethod]
    public async Task OnDismissed_Runs_Once_The_Checklist_Has_Gone()
    {
        var checklistOnScreen = (bool?)null;
        IRenderedComponent<BonboardingChecklist>? cut = null;

        cut = Render<BonboardingChecklist>(parameters => parameters
            .Add(list => list.Checklist, Checklist(new BonboardingChecklistItem { Id = "a", Title = "A" }))
            .Add(list => list.Dismissible, true)
            .Add(list => list.OnDismissed, () => checklistOnScreen = cut!.FindAll(".bit-bonboarding-checklist").Count > 0));

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-bonboarding-action='dismiss-checklist']")));
        await cut.Find("[data-bonboarding-action='dismiss-checklist']").ClickAsync(new());

        Assert.IsFalse(checklistOnScreen, "Focus can be moved to something that stays: the checklist is no longer rendered.");
    }

    // ---- Validation -----------------------------------------------------------------------

    [TestMethod]
    public void Validate_Finds_Duplicate_Ids_Unknown_Prerequisites_And_Circles()
    {
        var checklist = Checklist(
            new BonboardingChecklistItem { Id = "a", Title = "A", DependsOn = ["b"] },
            new BonboardingChecklistItem { Id = "b", Title = "B", DependsOn = ["c"] },
            new BonboardingChecklistItem { Id = "c", Title = "C", DependsOn = ["a"] },
            new BonboardingChecklistItem { Id = "d", Title = "D", DependsOn = ["d", "missing"] },
            new BonboardingChecklistItem { Id = "d", Title = "D again" });

        var issues = checklist.Validate();
        var codes = issues.Select(issue => issue.Code).ToArray();

        CollectionAssert.AreEquivalent(new[] { "duplicate-item-id", "item-depends-on-itself", "unknown-dependency", "dependency-cycle" }, codes);
        Assert.IsTrue(issues.All(issue => issue.Severity == BonboardingValidationSeverity.Error));
        StringAssert.Contains(issues.Single(issue => issue.Code == "dependency-cycle").Message, "a -> b -> c -> a");
    }

    [TestMethod]
    public void Validate_Passes_A_Sound_Checklist_And_Warns_About_One_Of_Optional_Items_Only()
    {
        Assert.IsEmpty(Checklist(
            new BonboardingChecklistItem { Id = "a", Title = "A" },
            new BonboardingChecklistItem { Id = "b", Title = "B", DependsOn = ["a"] },
            new BonboardingChecklistItem { Id = "c", Title = "C", DependsOn = ["a", "b"], Optional = true }).Validate());

        var issue = Checklist(new BonboardingChecklistItem { Id = "a", Title = "A", Optional = true }).Validate().Single();
        Assert.AreEqual("only-optional-items", issue.Code);
        Assert.AreEqual(BonboardingValidationSeverity.Warning, issue.Severity);
    }
}
