using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

[TestClass]
public class TourSessionTests
{
    private static BonboardingTourDefinition Tour(params BonboardingStepDefinition[] steps)
        => new() { Id = "test-tour", Steps = [.. steps] };

    private static BonboardingStepDefinition Step(string id, string? selector = null) => new()
    {
        Id = id,
        Target = selector is null ? BonboardingStepTarget.None : BonboardingStepTarget.Css(selector),
        Title = id,
    };

    // ---- Basic navigation ---------------------------------------------------

    [TestMethod]
    public async Task Starts_On_The_First_Step()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a"), Step("b", "#b")));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual("a", session.CurrentStep!.Id);
        Assert.AreEqual(1, session.DisplayPosition);
        Assert.AreEqual(2, session.DisplayCount);
        Assert.IsTrue(session.IsFirstStep);
        Assert.IsFalse(session.IsLastStep);
    }

    [TestMethod]
    public async Task Moves_Forward_And_Back()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a"), Step("b", "#b"), Step("c", "#c")));

        await session.NextAsync();
        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.IsFalse(session.IsFirstStep);

        await session.NextAsync();
        Assert.AreEqual("c", session.CurrentStep!.Id);
        Assert.IsTrue(session.IsLastStep);

        await session.PreviousAsync();
        Assert.AreEqual("b", session.CurrentStep!.Id);

        await session.PreviousAsync();
        Assert.AreEqual("a", session.CurrentStep!.Id);
        Assert.IsTrue(session.IsFirstStep);
    }

    [TestMethod]
    public async Task Next_On_The_Last_Step_Completes_The_Tour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a")));
        await session.NextAsync();

        Assert.AreEqual(BonboardingTourStatus.Completed, session.Status);
        Assert.AreEqual(BonboardingTourEndReason.Completed, session.EndReason);
        Assert.IsNull(session.CurrentStep);
        Assert.IsFalse(session.IsActive);
    }

    [TestMethod]
    public async Task Previous_On_The_First_Step_Does_Nothing()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a"), Step("b", "#b")));
        await session.PreviousAsync();

        Assert.AreEqual("a", session.CurrentStep!.Id);
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    [TestMethod]
    public async Task Jumps_To_A_Step_By_Id()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a"), Step("b", "#b"), Step("c", "#c")));
        await session.GoToAsync("c");

        Assert.AreEqual("c", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task Jumping_To_An_Unknown_Step_Is_Reported_To_The_Caller()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a")));

        // A typo in the calling code is that code's problem, not a reason to end the user's tour.
        var thrown = Assert.ThrowsExactly<BonboardingException>(() => session.GoToAsync("nope"));
        Assert.AreEqual("nope", thrown.StepId);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.GoToAsync(5));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.GoToAsync(-1));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual("a", session.CurrentStep!.Id);
        Assert.IsNull(session.Error);
    }

    [TestMethod]
    public async Task A_Branch_To_An_Unknown_Step_Still_Fails_The_Tour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        // Inside the tour's own definition, a dangling id is a broken tour.
        var a = Step("a", "#a");
        a.ResolveNext = _ => ValueTask.FromResult<string?>("nope");

        var session = await harness.Service.StartAsync(Tour(a, Step("b")));
        await session.NextAsync();

        Assert.AreEqual(BonboardingTourStatus.Failed, session.Status);
        Assert.IsInstanceOfType<BonboardingException>(session.Error);
    }

    [TestMethod]
    public async Task Skip_And_Dismiss_Record_Their_Own_Reasons()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var skipped = await harness.Service.StartAsync(Tour(Step("a", "#a")));
        await skipped.SkipAsync();
        Assert.AreEqual(BonboardingTourStatus.Skipped, skipped.Status);

        var dismissed = await harness.Service.StartAsync(
            new BonboardingTourDefinition { Id = "other", Steps = [Step("a", "#a")] });
        await dismissed.DismissAsync();
        Assert.AreEqual(BonboardingTourStatus.Dismissed, dismissed.Status);
    }

    // ---- Conditional steps --------------------------------------------------

    [TestMethod]
    public async Task Skips_Steps_Whose_Condition_Is_False()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var hidden = Step("b", "#b");
        hidden.When = _ => ValueTask.FromResult(false);

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a"), hidden, Step("c", "#c")));

        Assert.AreEqual(2, session.DisplayCount);

        await session.NextAsync();
        Assert.AreEqual("c", session.CurrentStep!.Id);

        await session.PreviousAsync();
        Assert.AreEqual("a", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task Conditions_Are_Re_Evaluated_On_Every_Transition()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var conditional = Step("b", "#b");
        conditional.When = context => ValueTask.FromResult(context.State.ContainsKey("unlocked"));

        var first = Step("a", "#a");
        first.OnBeforeLeave = context =>
        {
            context.State["unlocked"] = true;
            return ValueTask.CompletedTask;
        };

        var session = await harness.Service.StartAsync(Tour(first, conditional, Step("c", "#c")));

        // The gate is closed while the first step is showing.
        Assert.AreEqual(2, session.DisplayCount);

        await session.NextAsync();

        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.AreEqual(3, session.DisplayCount);
    }

    [TestMethod]
    public async Task A_Condition_That_Excludes_Everything_Completes_The_Tour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var step = Step("a", "#a");
        step.When = _ => ValueTask.FromResult(false);

        var session = await harness.Service.StartAsync(Tour(step));

        Assert.AreEqual(BonboardingTourStatus.Completed, session.Status);
    }

    // ---- Branching ----------------------------------------------------------

    [TestMethod]
    public async Task Branches_Through_ResolveNext()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var fork = Step("a", "#a");
        fork.ResolveNext = _ => ValueTask.FromResult<string?>("c");

        var session = await harness.Service.StartAsync(Tour(fork, Step("b", "#b"), Step("c", "#c")));
        await session.NextAsync();

        Assert.AreEqual("c", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task Back_Follows_The_Visit_History_Not_The_Declaration_Order()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var fork = Step("a", "#a");
        fork.ResolveNext = _ => ValueTask.FromResult<string?>("c");

        var session = await harness.Service.StartAsync(Tour(fork, Step("b", "#b"), Step("c", "#c")));
        await session.NextAsync();
        await session.PreviousAsync();

        // Back from c must land on a, the step actually visited, not on b.
        Assert.AreEqual("a", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task ResolveNext_Can_End_The_Tour_Early()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var fork = Step("a", "#a");
        fork.ResolveNext = _ => ValueTask.FromResult<string?>(BonboardingTourDefinition.EndStepId);

        var session = await harness.Service.StartAsync(Tour(fork, Step("b", "#b")));
        await session.NextAsync();

        Assert.AreEqual(BonboardingTourStatus.Completed, session.Status);
    }

    [TestMethod]
    public async Task A_Branching_Step_Is_Never_Reported_As_Last()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var fork = Step("a", "#a");
        fork.ResolveNext = _ => ValueTask.FromResult<string?>(null);

        var session = await harness.Service.StartAsync(Tour(fork));

        Assert.IsFalse(session.IsLastStep);
    }

    // ---- Guards -------------------------------------------------------------

    [TestMethod]
    public async Task CanAdvance_Blocks_Forward_Navigation()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var allow = false;
        var gated = Step("a", "#a");
        gated.CanAdvance = _ => ValueTask.FromResult(allow);

        var session = await harness.Service.StartAsync(Tour(gated, Step("b", "#b")));

        await session.NextAsync();
        Assert.AreEqual("a", session.CurrentStep!.Id);

        allow = true;
        await session.NextAsync();
        Assert.AreEqual("b", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task A_Throwing_Hook_Fails_The_Tour_Rather_Than_Escaping()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var step = Step("a", "#a");
        step.OnBeforeLeave = _ => throw new InvalidOperationException("validation failed");

        var session = await harness.Service.StartAsync(Tour(step, Step("b", "#b")));
        await session.NextAsync();

        Assert.AreEqual(BonboardingTourStatus.Failed, session.Status);
        Assert.AreEqual("validation failed", session.Error!.Message);
    }

    // ---- Missing targets ----------------------------------------------------

    [TestMethod]
    public async Task Waits_For_A_Missing_Target_Then_Shows_It_When_It_Appears()
    {
        await using var harness = new BonboardingHarness();

        var step = Step("a", "#late");
        step.MissingTarget = BonboardingMissingTargetBehavior.Wait;

        var session = await harness.Service.StartAsync(Tour(step));

        Assert.AreEqual(BonboardingTourStatus.Waiting, session.Status);
        Assert.IsTrue(session.IsWaiting);
        Assert.IsTrue(harness.Interop.LastActivation!.WaitForTarget);

        await harness.Interop.AppearAsync(session.Id, "#late");

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.IsNotNull(session.TargetRect);
    }

    [TestMethod]
    public async Task Skips_A_Step_Whose_Target_Is_Missing()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#b");

        var missing = Step("a", "#gone");
        missing.MissingTarget = BonboardingMissingTargetBehavior.Skip;

        var session = await harness.Service.StartAsync(Tour(missing, Step("b", "#b")));

        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.Contains(BonboardingEventKind.StepSkipped, harness.Events.Kinds);
    }

    [TestMethod]
    public async Task Centres_A_Step_Whose_Target_Is_Missing()
    {
        await using var harness = new BonboardingHarness();

        var missing = Step("a", "#gone");
        missing.MissingTarget = BonboardingMissingTargetBehavior.Center;

        var session = await harness.Service.StartAsync(Tour(missing));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual("a", session.CurrentStep!.Id);
        Assert.IsNull(session.TargetRect);

        // The activation measurement is the only frame the browser is guaranteed to send.
        Assert.IsNotNull(((BonboardingTourSession)session).Placement);
    }

    [TestMethod]
    public async Task Fails_A_Step_Whose_Target_Is_Missing_When_Asked_To()
    {
        await using var harness = new BonboardingHarness();

        var missing = Step("a", "#gone");
        missing.MissingTarget = BonboardingMissingTargetBehavior.Fail;

        var session = await harness.Service.StartAsync(Tour(missing));

        Assert.AreEqual(BonboardingTourStatus.Failed, session.Status);
        Assert.IsInstanceOfType<BonboardingException>(session.Error);
    }

    [TestMethod]
    public async Task A_Wait_That_Times_Out_Applies_The_Timeout_Behaviour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#b");

        var missing = Step("a", "#never");
        missing.MissingTarget = BonboardingMissingTargetBehavior.Wait;
        missing.WaitTimeoutBehavior = BonboardingMissingTargetBehavior.Skip;

        var session = await harness.Service.StartAsync(Tour(missing, Step("b", "#b")));
        Assert.AreEqual(BonboardingTourStatus.Waiting, session.Status);

        await harness.Interop.TimeOutWaitAsync(session.Id);

        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    [TestMethod]
    public async Task A_Wait_Timeout_Never_Loops_Back_Into_Waiting()
    {
        await using var harness = new BonboardingHarness();

        var missing = Step("a", "#never");
        missing.MissingTarget = BonboardingMissingTargetBehavior.Wait;
        missing.WaitTimeoutBehavior = BonboardingMissingTargetBehavior.Wait;

        var session = await harness.Service.StartAsync(Tour(missing));
        await harness.Interop.TimeOutWaitAsync(session.Id);

        // Waiting again would hang forever, so the timeout degrades to skipping.
        Assert.AreNotEqual(BonboardingTourStatus.Waiting, session.Status);
    }

    [TestMethod]
    public async Task Losing_A_Target_Mid_Step_Returns_To_Waiting()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var step = Step("a", "#a");
        step.MissingTarget = BonboardingMissingTargetBehavior.Wait;

        var session = await harness.Service.StartAsync(Tour(step));
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);

        await harness.Interop.LoseTargetAsync(session.Id);

        Assert.AreEqual(BonboardingTourStatus.Waiting, session.Status);
        Assert.IsNull(session.TargetRect);
    }

    [TestMethod]
    public async Task A_Wait_That_Times_Out_Into_A_Centred_Card_Places_And_Shows_It()
    {
        await using var harness = new BonboardingHarness();

        var shown = 0;
        var missing = Step("a", "#never");
        missing.MissingTarget = BonboardingMissingTargetBehavior.Wait;
        missing.WaitTimeoutBehavior = BonboardingMissingTargetBehavior.Center;
        missing.OnShown = _ => { shown++; return ValueTask.CompletedTask; };

        var session = (BonboardingTourSession)await harness.Service.StartAsync(Tour(missing));
        Assert.AreEqual(BonboardingTourStatus.Waiting, session.Status);
        var activations = harness.Interop.Activations.Count;

        await harness.Interop.TimeOutWaitAsync(session.Id);

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.IsNotNull(session.Placement);
        Assert.AreEqual(1, shown);
        Assert.Contains(BonboardingEventKind.StepShown, harness.Events.Kinds);

        // The browser is handed the step again, without a target, so it keeps the card placed.
        Assert.AreEqual(activations + 1, harness.Interop.Activations.Count);
        Assert.AreEqual(BonboardingTargetKind.None, harness.Interop.LastActivation!.Target.Kind);
    }

    [TestMethod]
    public async Task A_Lost_Target_Is_Watched_For_Again_And_Restored_Without_Re_Showing_The_Step()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var shown = 0;
        var step = Step("a", "#a");
        step.MissingTarget = BonboardingMissingTargetBehavior.Wait;
        step.OnShown = _ => { shown++; return ValueTask.CompletedTask; };

        var session = await harness.Service.StartAsync(Tour(step));
        var activations = harness.Interop.Activations.Count;

        await harness.Interop.LoseTargetAsync(session.Id);

        // The browser stopped watching when it reported the loss, so it has to be asked to wait.
        Assert.AreEqual(activations + 1, harness.Interop.Activations.Count);
        Assert.IsTrue(harness.Interop.LastActivation!.WaitForTarget);

        await harness.Interop.AppearAsync(session.Id, "#a");

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.IsNotNull(session.TargetRect);
        Assert.AreEqual(1, shown);
    }

    [TestMethod]
    public async Task A_Lost_Target_Set_To_Skip_Ignores_CanAdvance()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var step = Step("a", "#a");
        step.MissingTarget = BonboardingMissingTargetBehavior.Skip;
        step.CanAdvance = _ => ValueTask.FromResult(false);

        var session = await harness.Service.StartAsync(Tour(step, Step("b")));

        await harness.Interop.LoseTargetAsync(session.Id);

        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.Contains(BonboardingEventKind.StepSkipped, harness.Events.Kinds);
    }

    [TestMethod]
    public async Task A_Lost_Target_Set_To_Center_Is_Placed_As_A_Card()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var step = Step("a", "#a");
        step.MissingTarget = BonboardingMissingTargetBehavior.Center;

        var session = (BonboardingTourSession)await harness.Service.StartAsync(Tour(step));

        await harness.Interop.LoseTargetAsync(session.Id);

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.IsNull(session.TargetRect);
        Assert.IsNotNull(session.Placement);
    }

    // ---- Interaction --------------------------------------------------------

    [TestMethod]
    public async Task An_AdvanceOn_Step_Lets_Pointer_Events_Reach_The_Target()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var interactive = Step("a", "#a");
        interactive.AdvanceOn = BonboardingAdvanceTrigger.TargetClick;

        var session = await harness.Service.StartAsync(Tour(interactive, Step("b", "#b")));

        Assert.AreEqual(BonboardingInteractionMode.TargetOnly, session.CurrentOptions!.Interaction);
        Assert.AreEqual("target", harness.Interop.LastActivation!.Interaction);

        await harness.Interop.TriggerAdvanceAsync(session.Id);

        Assert.AreEqual("b", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task An_Explicit_Interaction_Mode_Beats_The_AdvanceOn_Default()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var step = Step("a", "#a");
        step.AdvanceOn = BonboardingAdvanceTrigger.TargetClick;
        step.Interaction = BonboardingInteractionMode.Free;

        var session = await harness.Service.StartAsync(Tour(step));

        Assert.AreEqual(BonboardingInteractionMode.Free, session.CurrentOptions!.Interaction);
    }

    [TestMethod]
    public async Task Escape_Dismisses_The_Tour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a")));
        await harness.Interop.PressKeyAsync(session.Id, "Escape");

        Assert.AreEqual(BonboardingTourStatus.Dismissed, session.Status);
    }

    [TestMethod]
    public async Task Escape_Is_Ignored_When_Disabled()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var step = Step("a", "#a");
        step.CloseOnEscape = false;

        var session = await harness.Service.StartAsync(Tour(step));
        await harness.Interop.PressKeyAsync(session.Id, "Escape");

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    [TestMethod]
    public async Task Arrow_Keys_Navigate_And_Reverse_In_RightToLeft()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var tour = Tour(Step("a", "#a"), Step("b", "#b"));
        var session = await harness.Service.StartAsync(tour);

        await harness.Interop.PressKeyAsync(session.Id, "ArrowRight");
        Assert.AreEqual("b", session.CurrentStep!.Id);

        await harness.Interop.PressKeyAsync(session.Id, "ArrowLeft");
        Assert.AreEqual("a", session.CurrentStep!.Id);

        await session.DismissAsync();

        var rtlTour = new BonboardingTourDefinition
        {
            Id = "rtl",
            Direction = BonboardingDirection.RightToLeft,
            Steps = [Step("a", "#a"), Step("b", "#b")],
        };

        var rtl = await harness.Service.StartAsync(rtlTour);
        await harness.Interop.PressKeyAsync(rtl.Id, "ArrowLeft");

        Assert.AreEqual("b", rtl.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task Overlay_Clicks_Only_Close_When_Enabled()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a")));
        await harness.Interop.ClickOverlayAsync(session.Id);
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);

        await session.DismissAsync();

        var closable = new BonboardingTourDefinition
        {
            Id = "closable",
            CloseOnOverlayClick = true,
            Steps = [Step("a", "#a")],
        };

        var second = await harness.Service.StartAsync(closable);
        await harness.Interop.ClickOverlayAsync(second.Id);

        Assert.AreEqual(BonboardingTourStatus.Dismissed, second.Status);
    }

    // ---- Custom actions -----------------------------------------------------

    [TestMethod]
    public async Task A_Custom_Action_Runs_Its_Handler_Then_Applies_Its_Effect()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var clicked = false;
        var action = new BonboardingStepAction
        {
            Label = "Designer",
            Effect = BonboardingStepActionEffect.GoTo,
            TargetStepId = "c",
            OnClick = _ => { clicked = true; return ValueTask.CompletedTask; },
        };

        var step = Step("a", "#a");
        step.Actions = [action];

        var session = await harness.Service.StartAsync(Tour(step, Step("b", "#b"), Step("c", "#c")));
        await session.InvokeActionAsync(action);

        Assert.IsTrue(clicked);
        Assert.AreEqual("c", session.CurrentStep!.Id);
        Assert.Contains(BonboardingEventKind.ActionInvoked, harness.Events.Kinds);
    }

    [TestMethod]
    public async Task An_Action_Handler_That_Ends_The_Tour_Suppresses_Its_Effect()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        IBonboardingTourSession? captured = null;

        var action = new BonboardingStepAction
        {
            Label = "Stop",
            Effect = BonboardingStepActionEffect.Next,
            OnClick = context =>
            {
                captured = context.Session;
                return new ValueTask(context.Session.DismissAsync());
            },
        };

        var step = Step("a", "#a");
        step.Actions = [action];

        var session = await harness.Service.StartAsync(Tour(step, Step("b", "#b")));
        await session.InvokeActionAsync(action);

        Assert.AreSame(session, captured);
        Assert.AreEqual(BonboardingTourStatus.Dismissed, session.Status);
    }

    // ---- Pause and resume ---------------------------------------------------

    [TestMethod]
    public async Task Pause_Hides_The_Tour_And_Resume_Restores_The_Same_Step()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a"), Step("b", "#b")));
        await session.NextAsync();

        await session.PauseAsync();
        Assert.AreEqual(BonboardingTourStatus.Paused, session.Status);
        Assert.IsFalse(session.IsActive);

        await session.ResumeAsync();
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual("b", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task Stopping_Every_Tour_Also_Ends_A_Paused_One()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a")));
        await session.PauseAsync();

        await harness.Service.StopAllAsync();

        Assert.AreEqual(BonboardingTourStatus.Cancelled, session.Status);
    }

    // ---- Cleanup and races ---------------------------------------------------

    [TestMethod]
    public async Task Ending_A_Tour_Detaches_Everything_It_Owns()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a")));
        Assert.ContainsSingle(harness.Interop.AttachedSessions);

        await session.CompleteAsync();

        Assert.IsEmpty(harness.Interop.AttachedSessions);
        Assert.IsTrue(harness.Interop.DeactivateCount > 0);
        Assert.AreEqual(1, harness.Interop.DetachCount);
    }

    [TestMethod]
    public async Task Rapid_Navigation_Never_Interleaves_Transitions()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c", "#d");

        var session = await harness.Service.StartAsync(
            Tour(Step("a", "#a"), Step("b", "#b"), Step("c", "#c"), Step("d", "#d")));

        // Three simultaneous clicks on Next, as an impatient user produces.
        await Task.WhenAll(session.NextAsync(), session.NextAsync(), session.NextAsync());

        Assert.AreEqual("d", session.CurrentStep!.Id);
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    [TestMethod]
    public async Task Commands_After_The_Tour_Ends_Are_Ignored()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a"), Step("b", "#b")));
        await session.DismissAsync();

        await session.NextAsync();
        await session.PreviousAsync();
        await session.RefreshAsync();

        Assert.AreEqual(BonboardingTourStatus.Dismissed, session.Status);
        Assert.IsNull(session.CurrentStep);
    }

    [TestMethod]
    public async Task Starting_A_Second_Tour_Ends_The_First_By_Default()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var first = await harness.Service.StartAsync(Tour(Step("a", "#a")));
        var second = await harness.Service.StartAsync(
            new BonboardingTourDefinition { Id = "second", Steps = [Step("a", "#a")] });

        Assert.IsFalse(first.IsActive);
        Assert.IsTrue(second.IsActive);
        Assert.AreSame(second, harness.Service.Active);
    }

    [TestMethod]
    public async Task FindSession_Returns_The_Live_Session_After_A_Restart()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var tour = Tour(Step("a", "#a"));

        var first = await harness.Service.StartAsync(tour);
        await first.CompleteAsync();

        // Ended sessions are discarded rather than kept for the lifetime of the circuit.
        Assert.IsNull(harness.Service.FindSession(tour.Id));
        Assert.IsEmpty(harness.Service.Sessions);

        var second = await harness.Service.RestartAsync(tour);

        Assert.AreSame(second, harness.Service.FindSession(tour.Id));
        Assert.ContainsSingle(harness.Service.Sessions);
    }

    [TestMethod]
    public async Task Independent_Sessions_Can_Run_Side_By_Side()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var first = await harness.Service.StartAsync(Tour(Step("a", "#a")));
        var second = await harness.Service.StartAsync(
            new BonboardingTourDefinition { Id = "second", Steps = [Step("a", "#a")] },
            new BonboardingStartOptions { StopOthers = false });

        Assert.IsTrue(first.IsActive);
        Assert.IsTrue(second.IsActive);
        Assert.AreEqual(2, harness.Interop.AttachedSessions.Count);
    }

    // ---- Focus --------------------------------------------------------------

    [TestMethod]
    public async Task Focus_Is_Captured_On_The_First_Step_And_Restored_At_The_End()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = (BonboardingTourSession)await harness.Service.StartAsync(Tour(Step("a", "#a"), Step("b", "#b")));

        await session.FocusPopoverAsync();
        await session.NextAsync();
        await session.FocusPopoverAsync();

        Assert.AreEqual(1, harness.Interop.CaptureFocusCount);
        Assert.AreEqual(2, harness.Interop.FocusPopoverCount);

        await session.CompleteAsync();

        Assert.AreEqual(1, harness.Interop.RestoreFocusCount);
    }

    [TestMethod]
    public async Task Focus_Is_Not_Restored_When_The_Tour_Opts_Out()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var tour = new BonboardingTourDefinition { Id = "no-restore", RestoreFocus = false, Steps = [Step("a", "#a")] };
        var session = (BonboardingTourSession)await harness.Service.StartAsync(tour);

        await session.FocusPopoverAsync();
        await session.CompleteAsync();

        Assert.AreEqual(0, harness.Interop.RestoreFocusCount);
    }

    // ---- Multiple targets ---------------------------------------------------

    [TestMethod]
    public async Task Additional_Targets_Are_Sent_To_The_Browser_Layer()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#extra");

        var step = Step("a", "#a");
        step.AdditionalTargets = [BonboardingStepTarget.Css("#extra")];

        var session = await harness.Service.StartAsync(Tour(step));

        var activation = harness.Interop.LastActivation!;
        Assert.IsNotNull(activation.AdditionalTargets);
        Assert.AreEqual("#extra", Assert.ContainsSingle(activation.AdditionalTargets!).Selector);
        Assert.IsTrue(session.HasTarget);
    }

    // ---- Dynamic targets -----------------------------------------------------

    [TestMethod]
    public async Task A_Dynamic_Target_Is_Resolved_When_The_Step_Activates()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#chosen");

        var step = Step("a");
        step.Target = BonboardingStepTarget.Dynamic(_ => BonboardingStepTarget.Css("#chosen"));

        var session = await harness.Service.StartAsync(Tour(step));

        Assert.AreEqual("#chosen", harness.Interop.LastActivation!.Target.Selector);
        Assert.IsTrue(session.HasTarget);
    }

    [TestMethod]
    public async Task A_Dynamic_Target_Returning_Null_Is_Treated_As_No_Target()
    {
        await using var harness = new BonboardingHarness();

        var step = Step("a");
        step.Target = BonboardingStepTarget.Dynamic(_ => (BonboardingStepTarget?)null);

        var session = await harness.Service.StartAsync(Tour(step));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual(BonboardingTargetKind.None, harness.Interop.LastActivation!.Target.Kind);
    }

    // ---- Anchors -------------------------------------------------------------

    [TestMethod]
    public async Task An_Anchor_Target_Becomes_A_Data_Attribute_Selector()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("[data-bonboarding-anchor=\"create\"]");

        var step = Step("a");
        step.Target = BonboardingStepTarget.Anchor("create");

        var session = await harness.Service.StartAsync(Tour(step));

        Assert.AreEqual("[data-bonboarding-anchor=\"create\"]", harness.Interop.LastActivation!.Target.Selector);
        Assert.IsTrue(session.HasTarget);
    }

    // ---- Events ---------------------------------------------------------------

    [TestMethod]
    public async Task Publishes_A_Usable_Analytics_Stream()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a"), Step("b", "#b")));
        await session.NextAsync();
        await session.CompleteAsync();

        var kinds = harness.Events.Kinds;

        Assert.AreEqual(BonboardingEventKind.TourStarted, kinds[0]);
        Assert.Contains(BonboardingEventKind.StepShown, kinds);
        Assert.Contains(BonboardingEventKind.StepLeft, kinds);
        Assert.AreEqual(BonboardingEventKind.TourCompleted, kinds[^1]);

        var completed = harness.Events.Events[^1];
        Assert.AreEqual(BonboardingTourEndReason.Completed, completed.Reason);
        Assert.AreEqual("test-tour", completed.Tour.Id);

        // Step durations are what make funnel analysis possible.
        Assert.IsTrue(harness.Events.Events.Any(e => e.Kind == BonboardingEventKind.StepLeft && e.Duration is not null));
    }

    [TestMethod]
    public async Task A_Throwing_Observer_Cannot_Break_A_Tour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        harness.Service.EventRaised += (_, _) => throw new InvalidOperationException("analytics is down");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a")));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    // ---- Prerendering ----------------------------------------------------------

    [TestMethod]
    public async Task Runs_Without_A_Browser()
    {
        await using var harness = new BonboardingHarness();
        harness.Interop.IsAvailable = false;

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a"), Step("b", "#b")));

        // No DOM means no measurement, but the engine still advances so a prerendered pass is
        // harmless rather than fatal.
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.IsNull(session.Placement);
        Assert.IsEmpty(harness.Interop.Activations);

        await session.NextAsync();
        Assert.AreEqual("b", session.CurrentStep!.Id);
    }

    // ---- Skipped steps, Back, and hooks calling back in ---------------------------

    [TestMethod]
    public async Task A_Skipped_Step_Is_Not_Visited_Left_Or_Returned_To()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#c");

        var changes = new List<BonboardingStepChangedEventArgs>();
        var b = Step("b", "#gone");
        b.MissingTarget = BonboardingMissingTargetBehavior.Skip;

        var bPrepared = 0;
        var bLeft = 0;
        b.OnBeforeShow = _ => { bPrepared++; return ValueTask.CompletedTask; };
        b.OnBeforeLeave = _ => { bLeft++; return ValueTask.CompletedTask; };

        var tour = Tour(Step("a", "#a"), b, Step("c", "#c"));
        tour.OnStepChanged = args => { changes.Add(args); return ValueTask.CompletedTask; };

        var session = await harness.Service.StartAsync(tour);
        await session.NextAsync();

        Assert.AreEqual("c", session.CurrentStep!.Id);
        CollectionAssert.AreEqual(new[] { "a", "c" }, session.History.ToArray());

        // The skipped step never reached the screen: it reports no time on it, and the step after
        // it says the user came from the one they actually saw.
        Assert.IsFalse(harness.Events.Events.Any(e => e.Kind == BonboardingEventKind.StepLeft && e.Step?.Id == "b"));
        Assert.AreEqual("a", changes[^1].PreviousStep?.Id);
        Assert.AreEqual(BonboardingStepChangeReason.Next, changes[^1].Reason);

        // Its show hook did run, so its leave hook runs too, to undo whatever the show hook opened.
        Assert.AreEqual(1, bPrepared);
        Assert.AreEqual(1, bLeft);

        // Back goes to the step the user saw, without trying the skipped one again.
        await session.PreviousAsync();
        Assert.AreEqual("a", session.CurrentStep!.Id);
        Assert.AreEqual(1, bPrepared);
    }

    [TestMethod]
    public async Task A_Skipped_First_Step_Hands_On_The_Start_Reason()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#b");

        var changes = new List<BonboardingStepChangedEventArgs>();
        var a = Step("a", "#gone");
        a.MissingTarget = BonboardingMissingTargetBehavior.Skip;

        var tour = Tour(a, Step("b", "#b"));
        tour.OnStepChanged = args => { changes.Add(args); return ValueTask.CompletedTask; };

        var session = await harness.Service.StartAsync(tour);

        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.HasCount(1, changes);
        Assert.IsNull(changes[0].PreviousStep);
        Assert.AreEqual(BonboardingStepChangeReason.Start, changes[0].Reason);
        CollectionAssert.AreEqual(new[] { "b" }, session.History.ToArray());
    }

    [TestMethod]
    public async Task Skipping_Back_Past_The_First_Step_Carries_On_Forwards()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var a = Step("a", "#a");
        a.MissingTarget = BonboardingMissingTargetBehavior.Skip;

        var session = await harness.Service.StartAsync(Tour(a, Step("b", "#b")));
        await session.NextAsync();

        // The first step's target goes away while the user is on the second one.
        harness.Interop.ExistingSelectors.Remove("#a");
        await session.PreviousAsync();

        // Nothing to go back to: the user lands on a step that can be shown, not on one that cannot.
        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.IsNull(session.Error);
    }

    [TestMethod]
    public async Task Back_From_The_First_Step_Runs_No_Leave_Hooks()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var left = 0;
        var a = Step("a", "#a");
        a.OnBeforeLeave = _ => { left++; return ValueTask.CompletedTask; };

        var session = await harness.Service.StartAsync(Tour(a, Step("b")));
        await session.PreviousAsync();

        Assert.AreEqual("a", session.CurrentStep!.Id);
        Assert.AreEqual(0, left);
        Assert.DoesNotContain(BonboardingEventKind.StepLeft, harness.Events.Kinds);
        CollectionAssert.AreEqual(new[] { "a" }, session.History.ToArray());
    }

    [TestMethod]
    public async Task A_Leave_Hook_That_Removes_The_Way_Back_Shows_The_Step_Again()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var a = Step("a", "#a");
        a.When = context => ValueTask.FromResult(!context.State.ContainsKey("hide-a"));

        var shown = 0;
        var b = Step("b", "#b");
        b.OnBeforeShow = _ => { shown++; return ValueTask.CompletedTask; };
        b.OnBeforeLeave = context => { context.State["hide-a"] = true; return ValueTask.CompletedTask; };

        var session = await harness.Service.StartAsync(Tour(a, b));
        await session.NextAsync();
        await session.PreviousAsync();

        // The leave hook took away the only step behind. Its hooks have run, so rather than leave
        // the step half torn down it is shown again, with its history intact.
        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual(2, shown);
        CollectionAssert.AreEqual(new[] { "a", "b" }, session.History.ToArray());
    }

    [TestMethod]
    public async Task A_Hook_Can_Await_A_Command_Of_Its_Own()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var a = Step("a", "#a");
        a.CanAdvance = async context =>
        {
            await context.Session.DismissAsync();
            return true;
        };

        var session = await harness.Service.StartAsync(Tour(a, Step("b", "#b")));
        await session.NextAsync().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual(BonboardingTourStatus.Dismissed, session.Status);
    }

    [TestMethod]
    public async Task A_Hook_Can_Await_A_Command_It_Runs_On_Another_Thread()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var a = Step("a", "#a");
        a.OnShown = context => new ValueTask(Task.Run(() => context.Session.NextAsync()));

        var session = await harness.Service.StartAsync(Tour(a, Step("b", "#b"))).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual("b", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task Work_A_Hook_Leaves_Running_Waits_For_The_Transition()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var go = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? background = null;
        string? seenWhileShowing = null;

        var a = Step("a", "#a");

        // Fire and forget, as a hook might when it does not want to hold the tour up.
        a.OnBeforeShow = context =>
        {
            background = Task.Run(async () =>
            {
                await go.Task;
                await context.Session.NextAsync();
            });

            return ValueTask.CompletedTask;
        };

        a.OnShown = async context =>
        {
            go.SetResult();
            await Task.Delay(200);
            seenWhileShowing = context.Session.CurrentStep?.Id;
        };

        var session = await harness.Service.StartAsync(Tour(a, Step("b", "#b")));
        await background!.WaitAsync(TimeSpan.FromSeconds(10));

        // The background Next queued behind the transition that was still showing step a, instead
        // of running inside it.
        Assert.AreEqual("a", seenWhileShowing);
        Assert.AreEqual("b", session.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task The_Step_After_A_Skipped_One_Is_Persisted_With_A_Live_Token()
    {
        await using var harness = new BonboardingHarness(
            configureServices: services => services.AddBonboardingStore<TokenRecordingStore>());
        harness.WithElements("#b");

        var a = Step("a", "#gone");
        a.MissingTarget = BonboardingMissingTargetBehavior.Skip;

        var session = await harness.Service.StartAsync(Tour(a, Step("b", "#b")));

        Assert.AreEqual("b", session.CurrentStep!.Id);

        // Skipping must not hand the skipped step's own token, cancelled the moment the next
        // transition starts, to the step that replaces it.
        var store = (TokenRecordingStore)harness.Store;
        Assert.IsNotEmpty(store.Writes);
        Assert.IsFalse(store.Writes.Any(write => write.Cancelled));
        Assert.AreEqual("b", store.Writes[^1].Record.CurrentStepId);
    }

    [TestMethod]
    public async Task The_Arrow_Keeps_Clear_Of_The_Measured_Popover_Corners()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        // A target at the left edge, so the arrow is pushed against the popover's corner.
        harness.Interop.TargetRect = new BonboardingRect(0, 300, 10, 10);

        var step = Step("a", "#a");
        step.Placement = BonboardingPlacement.Bottom;

        var session = await harness.Service.StartAsync(Tour(step));
        var assumed = session.Placement!.Value.ArrowOffset;

        harness.Interop.PopoverRadius = 30;
        await session.RefreshAsync();
        var measured = session.Placement!.Value.ArrowOffset;

        // The inset is the corner radius plus half the arrow; unmeasured, the default stylesheet's.
        Assert.AreEqual(BonboardingDefaults.PopoverRadius + (BonboardingDefaults.ArrowSize / 2), assumed);
        Assert.AreEqual(30 + (BonboardingDefaults.ArrowSize / 2), measured);
    }

    // ---- Registration ---------------------------------------------------------

    [TestMethod]
    public async Task Starting_A_Tour_Does_Not_Register_It()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var session = await harness.Service.StartAsync(Tour(Step("a", "#a")));

        Assert.IsTrue(session.IsActive);
        Assert.IsNull(harness.Service.GetTour("test-tour"));
        Assert.IsEmpty(harness.Service.RegisteredTours);
    }

    [TestMethod]
    public async Task A_Running_Tour_Still_Decides_Which_Saved_Records_Are_Current()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        await harness.Store.SetAsync(new BonboardingRecord { TourId = "test-tour", Version = 1, Completed = true });

        var tour = Tour(Step("a", "#a"));
        tour.Version = 2;
        tour.Persist = false;

        await harness.Service.StartAsync(tour, new BonboardingStartOptions { Force = true });

        Assert.IsNull(await harness.Service.GetRecordAsync("test-tour"));
    }
}
