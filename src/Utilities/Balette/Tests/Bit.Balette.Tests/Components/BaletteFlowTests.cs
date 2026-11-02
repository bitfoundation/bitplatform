using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

/// <summary>
/// Flows that start outside the open palette - a global shortcut, <see cref="IBaletteService.ExecuteAsync"/>,
/// <see cref="IBaletteService.OpenAsync"/> with a query or a page - and commands that live only in the palette's
/// markup or its <see cref="Balette.Commands"/> parameter rather than in the service's registry.
/// </summary>
[TestClass]
public class BaletteFlowTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BunitJSModuleInterop _jsHandle;

    public BaletteFlowTests()
    {
        Services.AddBitBaletteServices();
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        var handle = _jsHandle = module.SetupModule("init", _ => true);
        handle.Setup<bool>("isMac").SetResult(false);
        handle.SetupVoid("open", _ => true).SetVoidResult();
        handle.SetupVoid("close").SetVoidResult();
        handle.SetupVoid("focusInput", _ => true).SetVoidResult();
        handle.SetupVoid("scrollIntoView", _ => true).SetVoidResult();
        handle.SetupVoid("setCommandShortcuts", _ => true).SetVoidResult();
        handle.SetupVoid("dispose").SetVoidResult();

        _service = Services.GetRequiredService<IBaletteService>();
    }

    // A toast's close that never comes by itself: a flow waiting on it ends only when something calls it off, and a
    // test that fails to call it off times out instead of passing because the close came first.
    private static readonly TimeSpan NoToastClose = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CalledOff = TimeSpan.FromSeconds(10);

    private bool? LastOpenSelectsAll()
        => _jsHandle.Invocations.LastOrDefault(i => i.Identifier == "open").Arguments is { Count: 2 } args ? (bool?)args[1] : null;

    [TestMethod]
    public async Task Opening_with_a_query_keeps_it_for_typing_after()
    {
        _service.Register(BaletteCmd.New("Anything"));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.OpenAsync(">"));
        cut.WaitForAssertion(() => Assert.IsFalse(LastOpenSelectsAll()));
        Assert.AreEqual(">", cut.Find("[data-balette=input]").GetAttribute("value"));

        await cut.InvokeAsync(() => _service.CloseAsync());
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.IsTrue(LastOpenSelectsAll()));
    }

    [TestMethod]
    public async Task A_shortcut_whose_handler_prompts_opens_the_palette_on_the_prompt()
    {
        _service.Register(BaletteCmd.New("Rename…").Shortcut("F2")
            .Handler(_ => Task.FromResult(BaletteCommandResult.Prompt("New name", _ => Task.FromResult(BaletteCommandResult.Close)))));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("f2"));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
            Assert.Contains("New name", cut.Find("[data-balette=prompt]").TextContent);
        });
        Assert.IsTrue(_service.IsOpen);
    }

    [TestMethod]
    public async Task Executing_a_page_command_from_code_opens_the_palette_on_its_page()
    {
        _service.Register(BaletteCmd.Page("Theme", BaletteCmd.New("Light"), BaletteCmd.New("Dark")));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("theme"));

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
            Assert.Contains("Theme", cut.Find("[data-balette=breadcrumbs]").TextContent);
            Assert.HasCount(2, cut.FindAll("[data-balette=item]"));
        });
    }

    [TestMethod]
    public async Task A_confirmation_started_from_a_shortcut_confirms_that_command_and_no_other()
    {
        var ran = new List<string>();
        _service.Register(
            BaletteCmd.Action("Harmless", () => ran.Add("harmless")),
            BaletteCmd.New("Delete project").Shortcut("Mod+Shift+D").Destructive("This cannot be undone").Handler(() => ran.Add("delete")));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("mod+shift+d"));
        cut.WaitForAssertion(() =>
        {
            var confirming = Assert.ContainsSingle(cut.FindAll("[data-confirming=true]"));
            Assert.AreEqual("true", confirming.GetAttribute("aria-selected"));
            Assert.Contains("Delete project", confirming.TextContent);
        });

        // The second Enter runs the active row, which has to be the command being confirmed.
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        AssertSequence.AreEqual(["delete"], ran);
    }

    [TestMethod]
    public async Task Commands_passed_to_the_palette_can_be_executed_and_opened_by_id()
    {
        var ran = false;
        var cut = Render<Balette>(p => p.Add(x => x.Commands, new[]
        {
            BaletteCmd.Action("Instance action", () => ran = true).Id("instance-action").Build(),
            BaletteCmd.Page("Instance page", BaletteCmd.New("Child one"), BaletteCmd.New("Child two")).Id("instance-page").Build(),
        }));

        await cut.InvokeAsync(() => _service.ExecuteAsync("instance-action"));
        Assert.IsTrue(ran);

        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "instance-page"));
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Instance page", cut.Find("[data-balette=breadcrumbs]").TextContent);
            Assert.HasCount(2, cut.FindAll("[data-balette=item]"));
        });
    }

    [TestMethod]
    public async Task Declarative_pages_can_be_opened_by_id()
    {
        var cut = Render<Balette>(p => p.AddChildContent<BaletteItem>(parent => parent
            .Add(x => x.Title, "New…")
            .Add(x => x.Id, "new")
            .AddChildContent<BaletteItem>(child => child.Add(x => x.Title, "Project"))));

        await cut.InvokeAsync(() => _service.OpenAsync(pageCommandId: "new"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("New…", cut.Find("[data-balette=breadcrumbs]").TextContent);
            Assert.Contains("Project", Assert.ContainsSingle(cut.FindAll("[data-balette=item]")).TextContent);
        });
    }

    [TestMethod]
    public async Task Executing_an_unknown_id_throws()
    {
        var cut = Render<Balette>();

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => cut.InvokeAsync(() => _service.ExecuteAsync("missing")));
    }

    [TestMethod]
    public async Task The_title_and_its_marks_render_as_one_inline_box()
    {
        _service.Register(BaletteCmd.New("Go to Settings"));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync("gs"));

        cut.WaitForAssertion(() =>
        {
            var label = cut.Find("[data-balette=item] [data-balette=label]");
            Assert.AreEqual("Go to Settings", label.TextContent);
            Assert.HasCount(2, label.QuerySelectorAll("mark"));
        });
    }

    [TestMethod]
    public async Task A_toast_before_closing_is_rendered_while_the_palette_is_still_open()
    {
        _service.Register(BaletteCmd.Action("Copy link", _ => Task.FromResult(BaletteCommandResult.Toast("Link copied"))));
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        // From the keyboard nothing renders until the whole flow is over, the close included, so the toast has to
        // be rendered on its own before the palette waits to close.
        var run = cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));

        cut.WaitForAssertion(() => Assert.Contains("Link copied", cut.Find("[data-balette=toast]").TextContent));
        await run;
        cut.WaitForAssertion(() => Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open")));
    }

    [TestMethod]
    public async Task A_toast_does_not_close_the_palette_reopened_before_its_close()
    {
        _service.Register(BaletteCmd.Action("Copy link", _ => Task.FromResult(BaletteCommandResult.Toast("Link copied"))));
        var cut = Render<Balette>();
        cut.Instance.ToastCloseDelay = NoToastClose;
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        var run = cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => Assert.Contains("Link copied", cut.Find("[data-balette=toast]").TextContent));

        // Esc and Ctrl+K inside the toast's close delay: the close it scheduled belongs to the palette that was dismissed.
        await cut.InvokeAsync(() => _service.CloseAsync());
        await cut.InvokeAsync(() => _service.OpenAsync());
        await run.WaitAsync(CalledOff);

        Assert.IsTrue(_service.IsOpen);
        Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
    }

    [TestMethod]
    public async Task Esc_while_a_toast_waits_to_close_closes_rather_than_going_back_a_page()
    {
        _service.Register(BaletteCmd.New("Rename…").Handler(_ => Task.FromResult(BaletteCommandResult.Prompt(
            "New name", _ => Task.FromResult(BaletteCommandResult.Toast("Renamed")), initialValue: "Apollo"))));
        var cut = Render<Balette>();
        cut.Instance.ToastCloseDelay = NoToastClose;
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => Assert.Contains("New name", cut.Find("[data-balette=prompt]").TextContent));

        var run = cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => Assert.Contains("Renamed", cut.Find("[data-balette=toast]").TextContent));

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Escape", false, false, false, false));
        await run.WaitAsync(CalledOff);

        Assert.IsFalse(_service.IsOpen);
        Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open"));
    }

    [TestMethod]
    public async Task Esc_while_a_toast_waits_to_close_goes_back_a_page_in_an_inline_palette_that_keeps_its_place()
    {
        Services.GetRequiredService<BaletteOptions>().ResetOnClose = false;
        _service.Register(BaletteCmd.New("Rename…").Handler(_ => Task.FromResult(BaletteCommandResult.Prompt(
            "New name", _ => Task.FromResult(BaletteCommandResult.Toast("Renamed")), initialValue: "Apollo"))));
        var cut = Render<Balette>(p => p.Add(x => x.Inline, true));
        cut.Instance.ToastCloseDelay = NoToastClose;
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => Assert.Contains("New name", cut.Find("[data-balette=prompt]").TextContent));

        var run = cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => Assert.Contains("Renamed", cut.Find("[data-balette=toast]").TextContent));

        // Its close would change nothing, so Esc does what it always does there: back to the page the prompt came from.
        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Escape", false, false, false, false));
        await run.WaitAsync(CalledOff);

        Assert.IsEmpty(cut.FindAll("[data-balette=prompt]"));
        Assert.ContainsSingle(cut.FindAll("[data-balette=item]"));
    }

    [TestMethod]
    public async Task A_toast_does_not_close_the_palette_while_the_user_types()
    {
        _service.Register(BaletteCmd.Action("Copy link", _ => Task.FromResult(BaletteCommandResult.Toast("Link copied"))));
        var cut = Render<Balette>();
        cut.Instance.ToastCloseDelay = NoToastClose;
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        var run = cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter", false, false, false, false));
        cut.WaitForAssertion(() => Assert.Contains("Link copied", cut.Find("[data-balette=toast]").TextContent));

        await cut.InvokeAsync(() => cut.Find("[data-balette=input]").Input("cop"));
        await run.WaitAsync(CalledOff);

        Assert.IsTrue(_service.IsOpen);
        Assert.AreEqual("cop", cut.Find("[data-balette=input]").GetAttribute("value"));
    }

    [TestMethod]
    public async Task A_toast_does_not_close_the_palette_on_a_confirmation_armed_by_a_click()
    {
        _service.Register(
            BaletteCmd.Action("Copy link", _ => Task.FromResult(BaletteCommandResult.Toast("Link copied"))).Id("copy"),
            BaletteCmd.Action("Reset counter", () => { }).Destructive("The count goes back to zero"));
        var cut = Render<Balette>();
        cut.Instance.ToastCloseDelay = NoToastClose;
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.HasCount(2, cut.FindAll("[data-balette=item]")));

        var run = cut.InvokeAsync(() => _service.ExecuteAsync("copy"));
        cut.WaitForAssertion(() => Assert.Contains("Link copied", cut.Find("[data-balette=toast]").TextContent));

        // A click runs no key handler: the command itself is what calls the close off.
        cut.FindAll("[data-balette=item]").Single(i => i.TextContent.Contains("Reset counter")).Click();
        await run.WaitAsync(CalledOff);

        Assert.IsTrue(_service.IsOpen);
        Assert.ContainsSingle(cut.FindAll("[data-confirming=true]"));
        Assert.Contains("The count goes back to zero", cut.Find("[data-balette=toast]").TextContent);
    }

    [TestMethod]
    public async Task A_toast_does_not_close_the_palette_after_a_key_that_reached_the_renderer_as_its_delay_ran_out()
    {
        _service.Register(BaletteCmd.Action("Copy link", _ => Task.FromResult(BaletteCommandResult.Toast("Link copied"))));
        var cut = Render<Balette>();
        cut.Instance.ToastCloseDelay = TimeSpan.FromMilliseconds(1);
        await cut.InvokeAsync(() => _service.OpenAsync());
        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));

        Task run = Task.CompletedTask;
        await cut.InvokeAsync(() =>
        {
            run = cut.Instance.OnKeyDown("Enter", false, false, false, false);

            // The renderer is held until the delay is over, so the close waits behind the key pressed next - as on
            // Blazor Server, where a key's event can reach the renderer before the end of the delay does.
            Thread.Sleep(200);
            return cut.Instance.OnKeyDown("ArrowDown", false, false, false, false);
        });
        await run.WaitAsync(CalledOff);

        Assert.IsTrue(_service.IsOpen);
        Assert.AreEqual("true", cut.Find("dialog").GetAttribute("data-open"));
    }

    [TestMethod]
    public async Task A_command_that_keeps_the_palette_open_asks_for_no_focus_while_it_is_closed()
    {
        var toggled = 0;
        _service.Register(BaletteCmd.New("Word wrap").Id("toggle:wordwrap").Shortcut("Alt+Z")
            .Handler(_ => { toggled++; return Task.FromResult(BaletteCommandResult.KeepOpen); }));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("alt+z"));
        await cut.InvokeAsync(() => _service.ExecuteAsync("toggle:wordwrap"));

        Assert.AreEqual(2, toggled);
        Assert.AreEqual("false", cut.Find("dialog").GetAttribute("data-open"));
        Assert.IsFalse(_jsHandle.Invocations.Any(i => i.Identifier == "focusInput"), "The input only exists while the palette is open.");
    }

    [TestMethod]
    public void The_service_reports_the_detected_platform()
    {
        var raised = 0;
        _service.PlatformDetected += () => raised++;

        var cut = Render<Balette>();

        cut.WaitForAssertion(() => Assert.AreEqual(1, raised));
        Assert.IsFalse(_service.IsMac);
    }
}
