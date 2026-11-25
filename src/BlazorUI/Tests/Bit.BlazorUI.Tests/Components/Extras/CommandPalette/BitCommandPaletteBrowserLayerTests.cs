using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// What the palette hands the browser layer: the values <c>setup</c> receives, and the global shortcuts it is kept in
/// sync with afterwards. The browser layer claims a matched key before .NET sees it, so a shortcut synced there that
/// .NET would not run swallows the key for nothing.
/// </summary>
[TestClass]
public class BitCommandPaletteBrowserLayerTests : BunitTestContext
{
    private IBitCommandPaletteService _service = default!;

    [TestInitialize]
    public void Initialize()
    {
        Services.AddBitCommandPaletteServices();
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        _service = Services.GetRequiredService<IBitCommandPaletteService>();
    }

    private BunitJSModuleInterop SetupBrowserLayer(InvocationMatcher? matcher = null) => Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, matcher ?? (_ => true));

    private IReadOnlyList<object?> SetupArguments() => Context.JSInterop.Invocations.Single(i => i.Identifier == BitCommandPaletteTestJs.Setup).Arguments;

    private static IEnumerable<string> LastSyncedShortcuts(BunitJSModuleInterop handle)
        => (IEnumerable<string>)handle.Invocations.Last(i => i.Identifier == "setCommandShortcuts").Arguments[0]!;

    [TestMethod]
    public void Setup_receives_the_browser_options_as_plain_values()
    {
        var options = Services.GetRequiredService<BitCommandPaletteOptions>();
        options.SuppressShortcutsInEditableElements = false;
        options.SequenceTimeoutMs = 750;
        _service.Register(BitCommandPaletteCmd.New("Rename").Shortcut("F2"));
        SetupBrowserLayer();

        Context.Render<BitCommandPalette>(p => p.Add(x => x.VimBindings, true));

        // Positional, and nothing whose shape the serializer would have to discover through reflection.
        var args = SetupArguments();
        Assert.HasCount(8, args);
        Assert.Contains("mod+k", (IEnumerable<string>)args[2]!);
        AssertSequence.AreEqual(["f2"], (IEnumerable<string>)args[3]!);
        Assert.AreEqual(false, args[4]);
        Assert.AreEqual(750, args[5]);
        Assert.AreEqual(true, args[6]);
        Assert.AreEqual(false, args[7]); // not inline
    }

    [TestMethod]
    public void A_command_registered_while_setup_is_on_its_way_reaches_the_browser_layer()
    {
        var registered = false;
        var handle = SetupBrowserLayer(_ =>
        {
            // The matcher runs while setup is on its way: its shortcuts were already taken and its handle is not back.
            if (!registered)
            {
                registered = true;
                _service.Register(BitCommandPaletteCmd.New("Rename").Shortcut("F2"));
            }

            return true;
        });

        Context.Render<BitCommandPalette>();

        Assert.IsEmpty((IEnumerable<string>)SetupArguments()[3]!);
        AssertSequence.AreEqual(["f2"], LastSyncedShortcuts(handle));
    }

    [TestMethod]
    public async Task A_shortcut_of_a_command_shadowed_by_one_with_the_same_id_is_not_claimed()
    {
        var saved = false;
        _service.Register(BitCommandPaletteCmd.Action("Save", () => saved = true).Id("save"));
        SetupBrowserLayer();

        var cut = Context.Render<BitCommandPalette>(p => p.Add(x => x.Commands, new[] { BitCommandPaletteCmd.New("Save").Id("save").Shortcut("Mod+S").Build() }));

        Assert.IsEmpty((IEnumerable<string>)SetupArguments()[3]!);
        await cut.InvokeAsync(() => cut.Instance._OnGlobalShortcut("mod+s"));
        Assert.IsFalse(saved);
    }

    [TestMethod]
    public async Task Disabled_and_hidden_commands_leave_their_shortcuts_to_the_page()
    {
        var visible = false;
        _service.Register(
            BitCommandPaletteCmd.New("Disabled").Shortcut("F2").Disabled(),
            BitCommandPaletteCmd.New("Hidden").Shortcut("F3").VisibleWhen(() => visible),
            BitCommandPaletteCmd.New("Shown").Shortcut("F4"));
        var handle = SetupBrowserLayer();

        var cut = Context.Render<BitCommandPalette>();
        AssertSequence.AreEqual(["f4"], (IEnumerable<string>)SetupArguments()[3]!);

        visible = true;
        await cut.InvokeAsync(() => _service.OpenAsync());

        AssertSequence.AreEqual(["f3", "f4"], LastSyncedShortcuts(handle));
    }

    [TestMethod]
    public async Task Typing_neither_rereads_the_registry_nor_reevaluates_the_shortcuts()
    {
        var evaluations = 0;
        _service.Register(
            BitCommandPaletteCmd.New("Merge").Shortcut("Mod+M").VisibleWhen(() => { evaluations++; return true; }),
            BitCommandPaletteCmd.New("Alpha"), BitCommandPaletteCmd.New("Beta"));
        var handle = SetupBrowserLayer();
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => _service.OpenAsync());
        var syncs = handle.Invocations.Count(i => i.Identifier == "setCommandShortcuts");
        var before = evaluations;

        foreach (var query in new[] { "a", "al", "alp", "alph", "alpha" })
        {
            await cut.InvokeAsync(() => cut.Instance.SetQueryAsync(query));
        }

        // Each keystroke evaluates the predicate once, to list the command or not - not again for its shortcut.
        Assert.AreEqual(before + 5, evaluations);
        Assert.AreEqual(syncs, handle.Invocations.Count(i => i.Identifier == "setCommandShortcuts"));
    }

    [TestMethod]
    public async Task A_command_that_changes_what_is_visible_updates_the_shortcuts_when_it_has_run()
    {
        var signedIn = false;
        _service.Register(
            BitCommandPaletteCmd.Action("Sign in", () => signedIn = true),
            BitCommandPaletteCmd.New("Sign out").Shortcut("Mod+Shift+Q").VisibleWhen(() => signedIn));
        var handle = SetupBrowserLayer();
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => _service.OpenAsync("sign in"));

        await cut.InvokeAsync(() => cut.Instance._OnKeyDown("Enter", false, false, false, false));

        AssertSequence.AreEqual(["mod+shift+q"], LastSyncedShortcuts(handle));
    }

    [TestMethod]
    public async Task A_command_registered_later_reaches_the_snapshot_and_the_shortcuts()
    {
        var handle = SetupBrowserLayer();
        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => _service.OpenAsync("late"));
        Assert.IsEmpty(cut.FindAll("[data-bit-cpl=item]"));

        await cut.InvokeAsync(() => _service.Register(BitCommandPaletteCmd.New("Late arrival").Shortcut("F7")));

        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-bit-cpl=item]")));
        AssertSequence.AreEqual(["f7"], LastSyncedShortcuts(handle));
    }

    [TestMethod]
    public async Task A_failed_setup_leaves_a_working_palette_without_hotkeys()
    {
        _service.Register(BitCommandPaletteCmd.New("Alpha"));
        // Thrown while the call is made, as System.Text.Json does for an argument it has no metadata for: not a JSException.
        SetupBrowserLayer(_ => throw new NotSupportedException("No metadata."));

        var cut = Context.Render<BitCommandPalette>();
        await cut.InvokeAsync(() => _service.OpenAsync());

        cut.WaitForAssertion(() => Assert.ContainsSingle(cut.FindAll("[data-bit-cpl=item]")));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public void A_sequence_timeout_must_be_positive()
    {
        var options = new BitCommandPaletteOptions();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { options.SequenceTimeoutMs = 0; });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { options.SequenceTimeoutMs = -1; });
        Assert.AreEqual(1000, options.SequenceTimeoutMs);
    }
}
