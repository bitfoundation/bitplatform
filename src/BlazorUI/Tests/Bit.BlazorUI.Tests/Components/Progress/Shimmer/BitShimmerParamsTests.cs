using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Shimmer;

/// <summary>
/// Covers the shape of the BitParams cascade of the Shimmer: the name it is cascaded under, that the params object
/// carries every parameter a shimmer does not write per instance, and that an empty one changes nothing. What each
/// cascaded value does to the rendered shimmer is covered by the cascade section of <see cref="BitShimmerTests"/>.
/// </summary>
[TestClass]
public class BitShimmerParamsTests : BunitTestContext
{
    // What a shimmer stands in for, what it says and whether it is still waiting are written per shimmer, so they
    // have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitShimmer.CascadingParameters),
        nameof(BitShimmer.ChildContent),
        nameof(BitShimmer.Content),
        nameof(BitShimmer.Label),
        nameof(BitShimmer.Loaded),
        nameof(BitShimmer.LoadedLabel),
        nameof(BitShimmer.Template),
    ];

    [TestMethod]
    public void BitShimmerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitShimmer)}", BitShimmerParams.ParamName);
        Assert.AreEqual("BitParams.BitShimmer", BitShimmerParams.ParamName);
    }

    [TestMethod]
    public void BitShimmerParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitShimmerParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitShimmerParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitShimmerParamsShouldCarryEveryParameterThatIsNotPerShimmer()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitShimmer).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                           .Select(p => p.Name)
                                           .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitShimmerParams).GetProperty(name), $"BitShimmerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitShimmerShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitShimmerParams() });
            parameters.AddChildContent<BitShimmer>(shimmer => shimmer.Add(p => p.Id, "test-shimmer"));
        });

        component.MarkupMatches(@"
<div id=""test-shimmer"" class=""bit-smr bit-smr-lin"" aria-busy=""true"">
    <div class=""bit-smr-wrp"" aria-hidden=""true"">
        <div style="""" class=""bit-smr-anm bit-smr-wav ""></div>
    </div>
</div>");
    }

    [TestMethod]
    public void BitShimmerShouldApplyCascadedBaseParameters()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitShimmerParams { Dir = BitDir.Rtl, Disabled = true, Class = "cascaded", AriaLabel = "Profile" } });
            parameters.AddChildContent<BitShimmer>();
        });

        var root = component.Find(".bit-smr");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        // A name reaches the role the same way whether the shimmer or the cascade gave it.
        Assert.AreEqual("Profile", root.GetAttribute("aria-label"));
        Assert.AreEqual("progressbar", root.GetAttribute("role"));
    }
}
