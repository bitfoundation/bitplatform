using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.SwipeTrap;

[TestClass]
public class BitSwipeTrapParamsTests : BunitTestContext
{
    [TestMethod]
    public void BitSwipeTrapShouldRespectCascadingParams()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");

        var component = RenderComponent<BitSwipeTrapCascadingParamsTest>();

        var traps = component.FindAll(".bit-stp");

        Assert.AreEqual(2, traps.Count);

        // The first trap takes everything from the cascading parameters.
        var first = traps[0];
        Assert.IsTrue(first.ClassList.Contains("bit-stp-hrz"));
        Assert.IsTrue(first.ClassList.Contains("cascaded"));
        Assert.AreEqual("0", first.GetAttribute("tabindex"));
        Assert.AreEqual("ArrowLeft ArrowRight", first.GetAttribute("aria-keyshortcuts"));

        // The second one sets its own lock, trigger, filtering and class, which the cascade must not overwrite.
        var second = traps[1];
        Assert.IsTrue(second.ClassList.Contains("bit-stp-vrt"));
        Assert.IsFalse(second.ClassList.Contains("bit-stp-hrz"));
        Assert.IsTrue(second.ClassList.Contains("second"));
        Assert.IsFalse(second.ClassList.Contains("cascaded"));
        Assert.IsFalse(second.HasAttribute("tabindex"));

        var setups = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.SwipeTrap.setup").ToList();
        Assert.AreEqual(2, setups.Count);

        // trigger, triggerVelocity, threshold, throttle, orientationLock, touchOnly, skipSelector, keyboardTrigger
        var cascaded = setups[0].Arguments;
        Assert.AreEqual(60m, cascaded[2]);
        Assert.AreEqual(0.5m, cascaded[3]);
        Assert.AreEqual(10m, cascaded[4]);
        Assert.AreEqual(20, cascaded[5]);
        Assert.AreEqual(BitSwipeOrientation.Horizontal, cascaded[6]);
        Assert.AreEqual(true, cascaded[7]);
        Assert.AreEqual(".no-swipe", cascaded[8]);
        Assert.AreEqual(true, cascaded[9]);

        var own = setups[1].Arguments;
        Assert.AreEqual(0.5m, own[2]);
        Assert.AreEqual(0.5m, own[3]);
        Assert.AreEqual(10m, own[4]);
        Assert.AreEqual(BitSwipeOrientation.Vertical, own[6]);
        Assert.AreEqual(false, own[7]);
        Assert.AreEqual(false, own[9]);
    }
}
