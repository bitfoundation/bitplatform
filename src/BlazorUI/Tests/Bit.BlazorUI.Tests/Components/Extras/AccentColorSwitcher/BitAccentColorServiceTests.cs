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
    public void BitAccentColorServiceShouldTolerateRepeatedDisposal()
    {
        var service = GetService();

        service.Dispose();
        service.Dispose();
    }
}
