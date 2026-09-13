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
}
