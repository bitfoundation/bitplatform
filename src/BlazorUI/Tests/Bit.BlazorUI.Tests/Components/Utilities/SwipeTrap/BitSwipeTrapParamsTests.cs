using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.SwipeTrap;

/// <summary>
/// Covers the BitParams cascade of the SwipeTrap: the name it is cascaded under, that the params object carries
/// every parameter a trap does not write per instance, what a cascaded value fills in - and hands the script - and
/// what the trap keeps because it wrote it for itself.
/// </summary>
[TestClass]
public class BitSwipeTrapParamsTests : BunitTestContext
{
    // What a trap wraps and what it reports back are written per trap, so they have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitSwipeTrap.CascadingParameters),
        nameof(BitSwipeTrap.ChildContent),
        nameof(BitSwipeTrap.OnStart),
        nameof(BitSwipeTrap.OnMove),
        nameof(BitSwipeTrap.OnEnd),
        nameof(BitSwipeTrap.OnTrigger),
    ];

    [TestMethod]
    public void BitSwipeTrapParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitSwipeTrap)}", BitSwipeTrapParams.ParamName);
        Assert.AreEqual("BitParams.BitSwipeTrap", BitSwipeTrapParams.ParamName);
    }

    [TestMethod]
    public void BitSwipeTrapParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitSwipeTrapParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitSwipeTrapParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitSwipeTrapParamsShouldCarryEveryParameterThatIsNotPerTrap()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitSwipeTrap).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Select(p => p.Name)
                                             .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitSwipeTrapParams).GetProperty(name), $"BitSwipeTrapParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitSwipeTrapShouldNotBeAffectedByAnEmptyParamsObject()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitSwipeTrapParams() });
            parameters.AddChildContent<BitSwipeTrap>();
        });

        component.MarkupMatches(@"<div class=""bit-stp"" id:ignore></div>");

        // The script is handed the defaults, exactly as it is without a cascade.
        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.SwipeTrap.setup");

        Assert.AreEqual(0.25m, setup.Arguments[2]);
        Assert.AreEqual(0m, setup.Arguments[3]);
        Assert.AreEqual(0m, setup.Arguments[4]);
        Assert.AreEqual(0, setup.Arguments[5]);
        Assert.AreEqual(BitSwipeOrientation.None, setup.Arguments[6]);
        Assert.AreEqual(false, setup.Arguments[7]);
        Assert.IsNull(setup.Arguments[8]);
        Assert.AreEqual(false, setup.Arguments[9]);
    }

    [TestMethod]
    public void BitSwipeTrapShouldApplyCascadedBaseParameters()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.SwipeTrap.setup");

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitSwipeTrapParams { Dir = BitDir.Rtl, Disabled = true, AriaLabel = "Swipe area", KeyboardTrigger = true } });
            parameters.AddChildContent<BitSwipeTrap>();
        });

        var root = component.Find(".bit-stp");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual("Swipe area", root.GetAttribute("aria-label"));
        Assert.AreEqual("group", root.GetAttribute("role"));

        // A cascaded Disabled keeps a cascaded KeyboardTrigger out of the tab order, the way the trap's own pair does.
        Assert.IsFalse(root.HasAttribute("tabindex"));
    }

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
