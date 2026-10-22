using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

/// <summary>
/// What the palette hands the browser layer: the values <c>init</c> receives, and the global shortcuts it is kept in
/// sync with afterwards. The browser layer claims a matched key before .NET sees it, so a shortcut synced there that
/// .NET would not run swallows the key for nothing.
/// </summary>
[TestClass]
public class BaletteBrowserLayerTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BunitJSModuleInterop _module;

    public BaletteBrowserLayerTests()
    {
        Services.AddBitBaletteServices();
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        _service = Services.GetRequiredService<IBaletteService>();
    }

    private BunitJSModuleInterop SetupInit(InvocationMatcher? matcher = null) => _module.SetupModule("init", matcher ?? (_ => true));

    private IReadOnlyList<object?> InitArguments() => _module.Invocations.Single(i => i.Identifier == "init").Arguments;

    private static IEnumerable<string> LastSyncedShortcuts(BunitJSModuleInterop handle)
        => (IEnumerable<string>)handle.Invocations.Last(i => i.Identifier == "setCommandShortcuts").Arguments[0]!;

    [TestMethod]
    public void Init_receives_the_browser_options_as_plain_values()
    {
        var options = Services.GetRequiredService<BaletteOptions>();
        options.SuppressShortcutsInEditableElements = false;
        options.SequenceTimeoutMs = 750;
        _service.Register(BaletteCmd.New("Rename").Shortcut("F2"));
        SetupInit();

        Render<Balette>(p => p.Add(x => x.VimBindings, true));

        // Positional, and nothing whose shape the serializer would have to discover through reflection.
        var args = InitArguments();
        Assert.HasCount(7, args);
        Assert.Contains("mod+k", (IEnumerable<string>)args[2]!);
        AssertSequence.AreEqual(["f2"], (IEnumerable<string>)args[3]!);
        Assert.AreEqual(false, args[4]);
        Assert.AreEqual(750, args[5]);
        Assert.AreEqual(true, args[6]);
    }

    [TestMethod]
    public void A_command_registered_while_init_is_on_its_way_reaches_the_browser_layer()
    {
        var registered = false;
        var handle = SetupInit(_ =>
        {
            // The matcher runs while init is on its way: its shortcuts were already taken and its handle is not back.
            if (!registered)
            {
                registered = true;
                _service.Register(BaletteCmd.New("Rename").Shortcut("F2"));
            }

            return true;
        });

        Render<Balette>();

        Assert.IsEmpty((IEnumerable<string>)InitArguments()[3]!);
        AssertSequence.AreEqual(["f2"], LastSyncedShortcuts(handle));
    }

    [TestMethod]
    public async Task A_shortcut_of_a_command_shadowed_by_one_with_the_same_id_is_not_claimed()
    {
        var saved = false;
        _service.Register(BaletteCmd.Action("Save", () => saved = true).Id("save"));
        SetupInit();

        var cut = Render<Balette>(p => p.Add(x => x.Commands, new[] { BaletteCmd.New("Save").Id("save").Shortcut("Mod+S").Build() }));

        Assert.IsEmpty((IEnumerable<string>)InitArguments()[3]!);
        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("mod+s"));
        Assert.IsFalse(saved);
    }

    [TestMethod]
    public async Task Disabled_and_hidden_commands_leave_their_shortcuts_to_the_page()
    {
        var visible = false;
        _service.Register(
            BaletteCmd.New("Disabled").Shortcut("F2").Disabled(),
            BaletteCmd.New("Hidden").Shortcut("F3").VisibleWhen(() => visible),
            BaletteCmd.New("Shown").Shortcut("F4"));
        var handle = SetupInit();

        var cut = Render<Balette>();
        AssertSequence.AreEqual(["f4"], (IEnumerable<string>)InitArguments()[3]!);

        visible = true;
        await cut.InvokeAsync(() => _service.OpenAsync());

        AssertSequence.AreEqual(["f3", "f4"], LastSyncedShortcuts(handle));
    }

    [TestMethod]
    public async Task A_failed_init_leaves_a_working_palette_without_hotkeys()
    {
        _service.Register(BaletteCmd.New("Alpha"));
        // Thrown while the call is made, as System.Text.Json does for an argument it has no metadata for: not a JSException.
        SetupInit(_ => throw new NotSupportedException("No metadata."));

        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync());

        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-balette=item]")));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public void A_sequence_timeout_must_be_positive()
    {
        var options = new BaletteOptions();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { options.SequenceTimeoutMs = 0; });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { options.SequenceTimeoutMs = -1; });
        Assert.AreEqual(1000, options.SequenceTimeoutMs);
    }
}
