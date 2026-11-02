using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// What protects a tour's author and its user from each other's mistakes: a definition that cannot work is refused
/// before anyone sees it, a user held on a step is told why, and application code can move a tour on without
/// holding a reference to it.
/// </summary>
[TestClass]
public class AuthoringTests
{
    private static BonboardingStepDefinition Step(string id, string? selector = null) => new()
    {
        Id = id,
        Title = id,
        Target = selector is null ? BonboardingStepTarget.None : BonboardingStepTarget.Css(selector),
    };

    private static BonboardingTourDefinition Tour(params BonboardingStepDefinition[] steps)
        => new() { Id = "authored", Steps = [.. steps] };

    private static string[] Codes(BonboardingTourDefinition tour, BonboardingValidationSeverity severity)
        => tour.Validate().Where(issue => issue.Severity == severity).Select(issue => issue.Code).ToArray();

    // ---- Validation -----------------------------------------------------------

    [TestMethod]
    public void A_Well_Formed_Tour_Has_No_Issues()
    {
        var branch = Step("a");
        branch.Actions = [new BonboardingStepAction { Label = "Skip ahead", Effect = BonboardingStepActionEffect.GoTo, TargetStepId = "c" }];

        Assert.IsEmpty(Tour(branch, Step("b"), Step("c")).Validate());
    }

    [TestMethod]
    public void Duplicate_Blank_And_Reserved_Step_Ids_Are_Errors()
    {
        var blank = Step("x");
        blank.Id = " ";

        var codes = Codes(Tour(Step("a"), Step("a"), blank, Step(BonboardingTourDefinition.EndStepId)), BonboardingValidationSeverity.Error);

        CollectionAssert.AreEquivalent(new[] { "duplicate-step-id", "blank-step-id", "reserved-step-id" }, codes);
    }

    [TestMethod]
    public void A_Jump_To_Nowhere_Is_An_Error()
    {
        var step = Step("a");
        step.Actions =
        [
            new BonboardingStepAction { Label = "Missing", Effect = BonboardingStepActionEffect.GoTo, TargetStepId = "nope" },
            new BonboardingStepAction { Label = "Unset", Effect = BonboardingStepActionEffect.GoTo },
        ];

        var issues = Tour(step).Validate();

        CollectionAssert.AreEquivalent(new[] { "goto-unknown-step", "goto-without-target" }, issues.Select(issue => issue.Code).ToArray());
        Assert.IsTrue(issues.All(issue => issue.StepId == "a"));
        Assert.Contains("'nope'", issues.First(issue => issue.Code == "goto-unknown-step").Message);
    }

    [TestMethod]
    public void Steps_That_Cannot_Work_As_Written_Are_Warnings()
    {
        var stuck = Step("stuck", "#a");
        stuck.ShowNext = false;

        var blocked = Step("blocked", "#a");
        blocked.AdvanceOn = BonboardingAdvanceTrigger.TargetClick;
        blocked.Interaction = BonboardingInteractionMode.Blocked;

        var untargeted = Step("untargeted");
        untargeted.AdvanceOn = BonboardingAdvanceTrigger.TargetClick;

        var absolute = Step("absolute");
        absolute.Route = "https://example.com/settings";

        var dim = Step("dim");
        dim.OverlayOpacity = 55;

        var codes = Codes(Tour(stuck, blocked, untargeted, absolute, dim), BonboardingValidationSeverity.Warning);

        CollectionAssert.AreEquivalent(
            new[] { "no-way-forward", "advance-on-blocked", "advance-on-without-target", "absolute-route", "overlay-opacity-out-of-range" },
            codes);
    }

    [TestMethod]
    public void A_Step_Without_A_Next_Button_Is_Fine_When_Something_Else_Moves_It_On()
    {
        var byClick = Step("click", "#a");
        byClick.ShowNext = false;
        byClick.AdvanceOn = BonboardingAdvanceTrigger.TargetClick;

        var bySignal = Step("signal");
        bySignal.ShowNext = false;
        bySignal.AdvanceOnSignal = "project-created";

        var byAction = Step("action");
        byAction.ShowNext = false;
        byAction.Actions = [new BonboardingStepAction { Label = "Finish", Effect = BonboardingStepActionEffect.Complete }];

        // A custom body can carry its own buttons.
        var byBody = Step("body");
        byBody.ShowNext = false;
        byBody.Body = ctx => builder => builder.AddContent(0, "Press the button in here.");

        Assert.IsEmpty(Tour(byClick, bySignal, byAction, byBody).Validate());
    }

    [TestMethod]
    public async Task An_Invalid_Tour_Is_Refused_With_Every_Error_Listed()
    {
        await using var harness = new BonboardingHarness();

        var step = Step("a");
        step.Actions = [new BonboardingStepAction { Label = "Go", Effect = BonboardingStepActionEffect.GoTo, TargetStepId = "nope" }];

        var error = await Assert.ThrowsExactlyAsync<BonboardingException>(() => harness.Service.StartAsync(Tour(step, Step("a"))));

        Assert.Contains("'nope'", error.Message);
        Assert.Contains("more than one step with the id 'a'", error.Message);
        Assert.AreEqual("authored", error.TourId);
        Assert.IsEmpty(harness.Service.Sessions, "Nothing was started.");
        Assert.IsEmpty(harness.Interop.Activations);
    }

    [TestMethod]
    public async Task A_Tour_With_Only_Warnings_Still_Starts()
    {
        await using var harness = new BonboardingHarness();

        var dim = Step("a");
        dim.OverlayOpacity = 2;

        var session = await harness.Service.StartAsync(Tour(dim));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    // ---- Telling the user why ----------------------------------------------------

    [TestMethod]
    public async Task A_Held_Advance_Says_Why_And_The_Reason_Goes_With_The_Step()
    {
        await using var harness = new BonboardingHarness();

        var named = false;
        var step = Step("name");
        step.ValidationMessage = "Fill in the form first.";
        step.CanAdvance = ctx =>
        {
            if (!named) ctx.ValidationMessage = "Give the project a name first.";
            return ValueTask.FromResult(named);
        };

        var session = await harness.Service.StartAsync(Tour(step, Step("done")));

        await session.NextAsync();

        Assert.AreEqual("name", session.CurrentStep!.Id);
        Assert.AreEqual("Give the project a name first.", session.ValidationMessage, "The hook's own reason wins over the step's.");
        Assert.Contains(BonboardingEventKind.AdvanceBlocked, harness.Events.Kinds);

        named = true;
        await session.NextAsync();

        Assert.AreEqual("done", session.CurrentStep!.Id);
        Assert.IsNull(session.ValidationMessage);
    }

    [TestMethod]
    public async Task The_Step_Message_Is_Used_When_The_Hook_Gives_None()
    {
        await using var harness = new BonboardingHarness();

        var step = Step("a");
        step.ValidationMessage = "Pick a plan to continue.";
        step.CanAdvance = _ => ValueTask.FromResult(false);

        var session = await harness.Service.StartAsync(Tour(step));
        await session.NextAsync();

        Assert.AreEqual("Pick a plan to continue.", session.ValidationMessage);
    }

    [TestMethod]
    public async Task Going_Back_Clears_The_Reason()
    {
        await using var harness = new BonboardingHarness();

        var step = Step("b");
        step.ValidationMessage = "Not yet.";
        step.CanAdvance = _ => ValueTask.FromResult(false);

        var session = await harness.Service.StartAsync(Tour(Step("a"), step));
        await session.NextAsync();
        await session.NextAsync();
        Assert.AreEqual("Not yet.", session.ValidationMessage);

        await session.PreviousAsync();

        Assert.AreEqual("a", session.CurrentStep!.Id);
        Assert.IsNull(session.ValidationMessage);
    }

    // ---- Signals --------------------------------------------------------------------

    [TestMethod]
    public async Task A_Signal_Moves_On_The_Step_Waiting_For_It()
    {
        await using var harness = new BonboardingHarness();

        var create = Step("create");
        create.ShowNext = false;
        create.AdvanceOnSignal = "project-created";

        var session = await harness.Service.StartAsync(Tour(create, Step("next")));

        Assert.IsFalse(await harness.Service.SignalAsync("something-else"));
        Assert.IsFalse(await harness.Service.SignalAsync("Project-Created"), "Signals are compared ordinally.");
        Assert.AreEqual("create", session.CurrentStep!.Id);

        Assert.IsTrue(await harness.Service.SignalAsync("project-created"));
        Assert.AreEqual("next", session.CurrentStep!.Id);

        // Not remembered: raised again, nothing waits for it.
        Assert.IsFalse(await harness.Service.SignalAsync("project-created"));
        Assert.AreEqual("next", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task A_Signal_Still_Asks_CanAdvance()
    {
        await using var harness = new BonboardingHarness();

        var create = Step("create");
        create.AdvanceOnSignal = "saved";
        create.ValidationMessage = "Save with a name.";
        create.CanAdvance = _ => ValueTask.FromResult(false);

        var session = await harness.Service.StartAsync(Tour(create, Step("next")));

        Assert.IsFalse(await harness.Service.SignalAsync("saved"));
        Assert.AreEqual("create", session.CurrentStep!.Id);
        Assert.AreEqual("Save with a name.", session.ValidationMessage);
    }

    [TestMethod]
    public async Task A_Signal_On_The_Last_Step_Completes_The_Tour()
    {
        await using var harness = new BonboardingHarness();

        var last = Step("last");
        last.AdvanceOnSignal = "done";

        var session = await harness.Service.StartAsync(Tour(Step("first"), last), new BonboardingStartOptions { StartAtStepId = "last" });

        Assert.IsTrue(await harness.Service.SignalAsync("done"));
        Assert.AreEqual(BonboardingTourStatus.Completed, session.Status);
    }
}
