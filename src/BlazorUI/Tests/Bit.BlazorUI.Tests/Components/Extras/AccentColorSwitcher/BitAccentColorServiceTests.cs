using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

[TestClass]
public class BitAccentColorServiceTests : BunitTestContext
{
    private BitAccentColorService GetService()
    {
        // The same registration an app gets from AddBitBlazorUIExtrasServices; the service is scoped
        // and pulls the core theme services from there.
        Context.Services.AddBitBlazorUIExtrasServices();

        return Context.Services.GetRequiredService<BitAccentColorService>();
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldStillApplyAfterDisposal()
    {
        var service = GetService();

        service.Dispose();

        // A circuit is torn down while a pick is in flight far more often than not, and the
        // transition gate is what the pick is parked on. Disposing it under the pick would turn an
        // orderly teardown into an ObjectDisposedException surfacing from the click handler that
        // started it, where nothing can handle it - so the gate outlives the service.
        await service.ApplyAsync(BitAccentColorPresets.Purple);

        Assert.AreEqual(BitAccentColorPresets.Purple, service.ActiveAccent);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldStillRestoreAfterDisposal()
    {
        // With something to restore, the restore takes the same transition gate a pick does - the
        // empty-store path returns before reaching it and would prove nothing.
        Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true)
               .SetResult(BitAccentColorPresets.Purple.TrimStart('#'));

        var service = GetService();

        service.Dispose();

        await service.InitializeAsync(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.All });

        Assert.AreEqual(BitAccentColorPresets.Purple, service.ActiveAccent);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldMakeEveryInitializeAwaitTheRestoreInFlight()
    {
        // Left unanswered on purpose: this is the window between one switcher starting the restore
        // and the stores answering it.
        var getPersisted = Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true);

        var service = GetService();
        var config = new BitAccentColorConfig { Persistence = BitAccentColorPersistence.All };

        var first = service.InitializeAsync(config);
        // The second switcher of the app chrome, initializing from its own first render while the
        // restore above is still in flight. Returning to it here - with ActiveAccent still the
        // packaged primary - is what made it mark the default swatch active next to the one the
        // first-paint CSS had already marked from the visitor's stores.
        var second = service.InitializeAsync(config);

        Assert.IsFalse(first.IsCompleted);
        Assert.IsFalse(second.IsCompleted);

        getPersisted.SetResult(BitAccentColorPresets.Purple.TrimStart('#'));

        await Task.WhenAll(first, second);

        Assert.AreEqual(BitAccentColorPresets.Purple, service.ActiveAccent);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldRestoreAnAccentOutsideTheOfferedOnes()
    {
        // What ApplyAsync persists for an accent the app applies programmatically without offering
        // it as a swatch - and what the first-paint machinery has already painted by the time the
        // restore runs. Dropped here, the service would report the packaged primary instead, and the
        // switchers would mark its swatch active on a page painted in this accent.
        Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true)
               .SetResult("123456");

        var service = GetService();

        await service.InitializeAsync(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.All });

        Assert.AreEqual("#123456", service.ActiveAccent);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldIgnoreAPersistedValueThatIsNotHex()
    {
        // The stores are visitor-editable, so this is the tampered / stale-format case: nothing to
        // restore, and nothing that could reach BitThemeFactory.
        Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true)
               .SetResult("not-a-color");

        var service = GetService();

        await service.InitializeAsync(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.All });

        Assert.AreEqual(BitAccentColorPresets.Blue, service.ActiveAccent);
    }

    [TestMethod]
    public void BitAccentColorServiceShouldTolerateRepeatedDisposal()
    {
        var service = GetService();

        service.Dispose();
        service.Dispose();
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldIgnoreAnAppliedValueThatIsNotHex()
    {
        var service = GetService();

        await service.ApplyAsync("rebeccapurple");

        Assert.AreEqual(BitAccentColorPresets.Blue, service.ActiveAccent);
        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.AccentColor.apply"),
            "A value that is not hex never reaches BitThemeFactory, nor the stores.");
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldReturnAnOfferedAccentInItsOwnSpelling()
    {
        var service = GetService();

        // A hand-edited store or a bare token resolves to the item, whose own casing keeps ActiveAccent comparable
        // by ordinal equality.
        await service.ApplyAsync(BitAccentColorPresets.Purple.TrimStart('#').ToUpperInvariant());

        Assert.AreEqual(BitAccentColorPresets.Purple, service.ActiveAccent);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldCanonicalizeAnAccentOutsideTheOfferedOnes()
    {
        var service = GetService();

        await service.ApplyAsync("ABCDEF");

        // The #-prefixed lower-case form is what BitThemeFactory requires.
        Assert.AreEqual("#abcdef", service.ActiveAccent);

        var apply = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.AccentColor.apply");
        Assert.AreEqual("abcdef", apply.Arguments[0]);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldRaiseAccentChangedOncePerApply()
    {
        var service = GetService();

        var raised = 0;
        service.AccentChanged += (_, _) => raised++;

        await service.ApplyAsync(BitAccentColorPresets.Purple);
        await service.ApplyAsync(BitAccentColorPresets.Green);

        Assert.AreEqual(2, raised);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldClearRatherThanApplyTheNeutralAccent()
    {
        var service = GetService();

        await service.ApplyAsync(BitAccentColorPresets.Purple);
        await service.ApplyAsync(BitAccentColorPresets.Blue);

        Assert.AreEqual(BitAccentColorPresets.Blue, service.ActiveAccent);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.AccentColor.apply"));
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.AccentColor.clear"),
            "The packaged primary reproduces the packaged palette, so the overrides, the attribute and the stores are cleared instead of ~280 properties being pushed across interop.");
    }

    [DataTestMethod,
        DataRow(BitAccentColorFirstPaintStrategy.None, false, false),
        DataRow(BitAccentColorFirstPaintStrategy.StaticCss, true, false),
        DataRow(BitAccentColorFirstPaintStrategy.StoredCss, true, true)]
    public async Task BitAccentColorServiceShouldApplyPerTheConfiguredFirstPaintStrategy(BitAccentColorFirstPaintStrategy strategy, bool setsAttribute, bool keepsSnapshot)
    {
        var service = GetService();

        await service.InitializeAsync(new BitAccentColorConfig { FirstPaintStrategy = strategy, Persistence = BitAccentColorPersistence.All });
        await service.ApplyAsync(BitAccentColorPresets.Purple);

        var apply = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.AccentColor.apply");

        Assert.AreEqual(BitAccentColorPresets.Purple.TrimStart('#').ToLowerInvariant(), apply.Arguments[0]);
        Assert.AreEqual(setsAttribute, apply.Arguments[3], "Only a CSS strategy keys the first paint on the bit-accent root attribute.");
        Assert.AreEqual(BitAccentColorSsr.Version, apply.Arguments[2]);
        Assert.AreEqual((int)BitAccentColorPersistence.All, apply.Arguments[4]);

        if (keepsSnapshot)
        {
            var css = apply.Arguments[1] as string;
            Assert.IsNotNull(css, "StoredCss keeps a snapshot of the generated palette for the inline head script to inject pre-paint.");
            StringAssert.Contains(css, "--bit-clr-pri:");
        }
        else
        {
            Assert.IsNull(apply.Arguments[1], $"{strategy} keeps no palette snapshot.");
        }
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldFixTheConfigurationWithTheFirstInitialize()
    {
        var service = GetService();

        await service.InitializeAsync(new BitAccentColorConfig { FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss, Persistence = BitAccentColorPersistence.Cookie });
        // A second, differently configured caller must not tear down what the first one set up.
        await service.InitializeAsync(new BitAccentColorConfig { FirstPaintStrategy = BitAccentColorFirstPaintStrategy.None, Persistence = BitAccentColorPersistence.None });

        await service.ApplyAsync(BitAccentColorPresets.Purple);

        var apply = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.AccentColor.apply");
        Assert.AreEqual(true, apply.Arguments[3]);
        Assert.AreEqual((int)BitAccentColorPersistence.Cookie, apply.Arguments[4]);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldReadThePersistedAccentWithTheConfiguredStores()
    {
        var service = GetService();

        await service.InitializeAsync(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.LocalStorage });

        var read = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.AccentColor.getPersisted");
        Assert.AreEqual((int)BitAccentColorPersistence.LocalStorage, read.Arguments[0],
            "Only the stores the persistence flags enable are read, so a cookie left by an earlier configuration cannot restore an accent.");
    }

    [TestMethod]
    public void BitAccentColorServiceShouldSeedTheAccentFromThePrerender()
    {
        var service = GetService();

        var raised = 0;
        service.AccentChanged += (_, _) => raised++;

        service.SeedFromPrerender(BitAccentColorPresets.Purple.TrimStart('#'));

        Assert.AreEqual(BitAccentColorPresets.Purple, service.ActiveAccent);
        Assert.AreEqual(1, raised);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count, "Seeding marks the swatch; painting is the first-paint machinery's.");

        // Seeding the accent already active changes nothing and announces nothing.
        service.SeedFromPrerender(BitAccentColorPresets.Purple);
        Assert.AreEqual(1, raised);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("not-a-color"),
        DataRow("12345")]
    public void BitAccentColorServiceShouldIgnoreASeedThatIsNotHex(string? seed)
    {
        var service = GetService();

        var raised = 0;
        service.AccentChanged += (_, _) => raised++;

        service.SeedFromPrerender(seed);

        Assert.AreEqual(BitAccentColorPresets.Blue, service.ActiveAccent);
        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void BitAccentColorServiceShouldSeedAnAccentOutsideTheOfferedOnes()
    {
        // A layout seeds before any switcher installs its accent list, so the seed is validated as hex only.
        var service = GetService();

        service.SeedFromPrerender("#ABCDEF");

        Assert.AreEqual("#abcdef", service.ActiveAccent);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldIgnoreASeedOnceTheStoresHaveBeenRead()
    {
        var service = GetService();

        await service.InitializeAsync();

        // A (possibly stale) cascaded value arriving after the interactive restore must not undo it.
        service.SeedFromPrerender(BitAccentColorPresets.Purple);

        Assert.AreEqual(BitAccentColorPresets.Blue, service.ActiveAccent);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldRestoreFromTheCustomStoreAheadOfTheWebStores()
    {
        var store = new FakeStore { Accent = BitAccentColorPresets.Green };
        Context.Services.AddSingleton<IBitAccentColorStore>(store);
        Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true).SetResult(BitAccentColorPresets.Purple);

        var service = GetService();

        await service.InitializeAsync(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.All });

        Assert.AreEqual(BitAccentColorPresets.Green, service.ActiveAccent);
        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.AccentColor.getPersisted"),
            "The app-owned copy outranks the web stores, which may lag behind it; they are not even read.");
    }

    [DataTestMethod,
        DataRow(null),
        DataRow("")]
    public async Task BitAccentColorServiceShouldFallBackToTheWebStoresWhenTheCustomStoreHasNothing(string? stored)
    {
        Context.Services.AddSingleton<IBitAccentColorStore>(new FakeStore { Accent = stored });
        Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true).SetResult(BitAccentColorPresets.Purple);

        var service = GetService();

        await service.InitializeAsync(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.All });

        Assert.AreEqual(BitAccentColorPresets.Purple, service.ActiveAccent);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldFallBackToTheWebStoresWhenTheCustomStoreThrows()
    {
        Context.Services.AddSingleton<IBitAccentColorStore>(new FakeStore { Throws = true });
        Context.JSInterop.Setup<string?>("BitBlazorUI.AccentColor.getPersisted", _ => true).SetResult(BitAccentColorPresets.Purple);

        var service = GetService();

        await service.InitializeAsync(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.All });

        Assert.AreEqual(BitAccentColorPresets.Purple, service.ActiveAccent, "Losing the app's copy is never an error the app sees.");
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldMirrorEveryApplyIntoTheCustomStore()
    {
        var store = new FakeStore();
        Context.Services.AddSingleton<IBitAccentColorStore>(store);

        var service = GetService();

        // Written independently of the persistence flags: None here, and the store still takes it.
        await service.InitializeAsync(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.None });

        await service.ApplyAsync(BitAccentColorPresets.Purple);
        Assert.AreEqual(BitAccentColorPresets.Purple, store.Accent);
        Assert.AreEqual(0, store.Removals);

        // The packaged primary is stored as "no preference" rather than as a value.
        await service.ApplyAsync(BitAccentColorPresets.Blue);
        Assert.IsNull(store.Accent);
        Assert.AreEqual(1, store.Removals);
    }

    [TestMethod]
    public async Task BitAccentColorServiceShouldStillApplyWhenTheCustomStoreFailsToWrite()
    {
        Context.Services.AddSingleton<IBitAccentColorStore>(new FakeStore { Throws = true });

        var service = GetService();

        await service.ApplyAsync(BitAccentColorPresets.Purple);

        Assert.AreEqual(BitAccentColorPresets.Purple, service.ActiveAccent);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.AccentColor.apply"));
    }

    private sealed class FakeStore : IBitAccentColorStore
    {
        public string? Accent { get; set; }
        public bool Throws { get; set; }
        public int Removals { get; private set; }

        public Task<string?> GetAccentAsync()
        {
            if (Throws) throw new System.InvalidOperationException("store down");
            return Task.FromResult(Accent);
        }

        public Task SetAccentAsync(string accent)
        {
            if (Throws) throw new System.InvalidOperationException("store down");
            Accent = accent;
            return Task.CompletedTask;
        }

        public Task RemoveAccentAsync()
        {
            if (Throws) throw new System.InvalidOperationException("store down");
            Accent = null;
            Removals++;
            return Task.CompletedTask;
        }
    }
}
