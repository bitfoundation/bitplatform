using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

/// <summary>
/// Covers the shape of the BitParams cascade the loading family shares: the one name every loader is cascaded under,
/// and that the params object carries every parameter of <see cref="BitLoadingBase"/> a loader does not write per
/// instance. What each cascaded value does to a rendered loader is covered, for every loader of the family, by the
/// cascade section of <see cref="BitLoadingTestsBase{TLoading}"/>.
/// </summary>
[TestClass]
public class BitLoadingParamsTests
{
    // A render fragment belongs to the markup it is written in, so the label template has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitLoadingBase.CascadingParameters),
        nameof(BitLoadingBase.LabelTemplate),
    ];

    [TestMethod]
    public void BitLoadingParamsShouldHaveCorrectParamName()
    {
        // The family shares one cascade, named after the base rather than after any one loader.
        Assert.AreEqual($"{nameof(BitParams)}.BitLoading", BitLoadingParams.ParamName);
    }

    [TestMethod]
    public void BitLoadingParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitLoadingParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitLoadingParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitLoadingParamsShouldCarryEveryParameterThatIsNotPerLoader()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitLoadingBase).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                               .Select(p => p.Name)
                                               .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitLoadingParams).GetProperty(name), $"BitLoadingParams has no {name}.");
        }
    }

    [TestMethod]
    public void EveryLoaderShouldReadTheOneSharedCascade()
    {
        // Every loader derives from the base, which is what makes one BitLoadingParams reach all of them: a loader
        // with a cascade of its own would be one the shared params object silently misses.
        var loaders = typeof(BitLoadingBase).Assembly.GetTypes()
                                                      .Where(t => t.IsAbstract is false && typeof(BitLoadingBase).IsAssignableFrom(t))
                                                      .ToArray();

        Assert.HasCount(18, loaders);

        foreach (var loader in loaders)
        {
            var cascade = loader.GetProperty(nameof(BitLoadingBase.CascadingParameters))!;
            var attribute = cascade.GetCustomAttribute<CascadingParameterAttribute>();

            Assert.IsNotNull(attribute, $"{loader.Name} has no cascading parameter.");
            Assert.AreEqual(BitLoadingParams.ParamName, attribute.Name, loader.Name);
            Assert.AreEqual(typeof(BitLoadingBase), cascade.DeclaringType, loader.Name);
        }
    }
}
