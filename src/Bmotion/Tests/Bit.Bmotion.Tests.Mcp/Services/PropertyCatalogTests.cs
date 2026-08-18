using System.Reflection;

namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The animatable-property catalog: the list is reflected off <see cref="BmProps"/> and the
/// expensive claim on each entry - whether the browser compositor can own it, and therefore whether
/// it animates or jumps on Blazor Server - is measured by running the engine.
/// <para>
/// Two things can go wrong and neither shows up as an exception. A property added to the library
/// can arrive with no description, so the tool answers about it in placeholders. And the measured
/// verdict can disagree with <see cref="BmotionPropertyCatalog.IsCompositorProperty"/>, which is
/// what the same tool uses to *explain* the verdict - leaving the server measuring one thing and
/// saying another. Both are asserted here.
/// </para>
/// </summary>
[TestClass]
public class PropertyCatalogTests
{
    private static readonly string[] KnownCategories =
        ["Transform", "Visual", "Layout", "Typography", "Motion path", "SVG", "Custom"];

    [TestMethod]
    public async Task Get_CoversEveryWritableBmPropsArgument()
    {
        var catalog = await BmotionPropertyCatalog.GetAsync();

        var expected = typeof(BmProps)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite && property.Name != nameof(BmProps.Transition))
            .Select(property => char.ToLowerInvariant(property.Name[0]) + property.Name[1..])
            .ToArray();

        CollectionAssert.AreEquivalent(expected, catalog.Select(entry => entry.Name).ToArray(),
                                       "The catalog and Bm.To(...) have drifted apart.");
    }

    /// <summary>
    /// A property with no entry in the description table falls back to placeholders ("Other", the
    /// name as its own CSS, "name: ..."), which is a silent, low-quality answer rather than a
    /// failure. Adding a property to BmProps without describing it should be caught here.
    /// </summary>
    [TestMethod]
    public async Task Get_EveryProperty_IsDescribed_NotFilledInWithPlaceholders()
    {
        var catalog = await BmotionPropertyCatalog.GetAsync();

        var undescribed = catalog.Where(entry => entry.Category == "Other").Select(entry => entry.Name).ToArray();

        Assert.AreEqual(0, undescribed.Length,
                        $"Not described in BmotionPropertyCatalog: {string.Join(", ", undescribed)}.");

        foreach (var entry in catalog)
        {
            CollectionAssert.Contains(KnownCategories, entry.Category, $"'{entry.Name}' has category '{entry.Category}'.");
            Assert.AreNotEqual(string.Empty, entry.Css.Trim(), $"'{entry.Name}' names no CSS.");
            StringAssert.Contains(entry.Example, entry.Name, $"'{entry.Name}' has example '{entry.Example}'.");
            Assert.AreNotEqual(string.Empty, entry.ValueType.Trim());
        }
    }

    /// <summary>
    /// The measured verdict and the rule the tools quote when explaining it have to agree. If they
    /// ever diverge, AnalyzeBmotionAnimation reports one playback path and gives the reasons for
    /// the other.
    /// </summary>
    [TestMethod]
    public async Task Get_TheMeasuredVerdict_AgreesWithTheRuleTheToolsExplainItWith()
    {
        var catalog = await BmotionPropertyCatalog.GetAsync();

        var disagreements = catalog
            .Where(entry => entry.CompositorEligible != BmotionPropertyCatalog.IsCompositorProperty(entry.Name))
            .Select(entry => $"{entry.Name}: engine says {entry.CompositorEligible}, " +
                             $"IsCompositorProperty says {BmotionPropertyCatalog.IsCompositorProperty(entry.Name)}")
            .ToArray();

        Assert.AreEqual(0, disagreements.Length, string.Join("; ", disagreements));
    }

    [TestMethod]
    [DataRow("x")]
    [DataRow("y")]
    [DataRow("scale")]
    [DataRow("rotate")]
    [DataRow("opacity")]
    public async Task Get_TheCheapProperties_AreMeasuredAsCompositorEligible(string name)
    {
        var entry = (await BmotionPropertyCatalog.GetAsync()).Single(property => property.Name == name);

        Assert.IsTrue(entry.CompositorEligible, $"'{name}' is documented as compositor-cheap but did not offload.");
        Assert.AreEqual("Animates", entry.OnBlazorServer);
    }

    [TestMethod]
    [DataRow("width")]
    [DataRow("height")]
    [DataRow("backgroundColor")]
    [DataRow("top")]
    [DataRow("filter")]
    [DataRow("boxShadow")]
    public async Task Get_TheExpensiveProperties_AreMeasuredAsNeedingTheFrameLoop(string name)
    {
        var entry = (await BmotionPropertyCatalog.GetAsync()).Single(property => property.Name == name);

        Assert.IsFalse(entry.CompositorEligible, $"'{name}' was reported as compositor-eligible.");
        Assert.AreEqual("Jumps to the target", entry.OnBlazorServer);
    }

    [TestMethod]
    public async Task Get_OnBlazorServer_NeverContradictsTheVerdict()
    {
        foreach (var entry in await BmotionPropertyCatalog.GetAsync())
        {
            Assert.AreEqual(entry.CompositorEligible ? "Animates" : "Jumps to the target", entry.OnBlazorServer,
                            $"'{entry.Name}' says one thing in two fields.");
        }
    }

    [TestMethod]
    public async Task Get_IsOrderedByCategory_SoTheCheapPropertiesComeFirst()
    {
        var catalog = await BmotionPropertyCatalog.GetAsync();

        Assert.AreEqual("Transform", catalog[0].Category);

        // Every Transform entry precedes every non-Transform one, which is the ordering an agent
        // scanning the list depends on.
        var lastTransform = Array.FindLastIndex(catalog, entry => entry.Category == "Transform");
        var firstOther = Array.FindIndex(catalog, entry => entry.Category != "Transform");

        Assert.IsTrue(lastTransform < firstOther, "The categories are interleaved.");
    }

    /// <summary>The probe runs once per process; it must not be paid for on every call.</summary>
    [TestMethod]
    public async Task Get_IsCachedRatherThanReProbedPerCall()
    {
        Assert.AreSame(await BmotionPropertyCatalog.GetAsync(), await BmotionPropertyCatalog.GetAsync());
    }

    [TestMethod]
    public void BuildTarget_SetsTheNamedProperties_AndReportsTheRest()
    {
        var props = BmotionPropertyCatalog.BuildTarget(["x", "opacity", "wobbliness"], out var unknown);

        Assert.IsNotNull(props.X);
        Assert.IsNotNull(props.Opacity);
        CollectionAssert.AreEqual(new[] { "wobbliness" }, unknown);
    }

    [TestMethod]
    public void BuildTarget_ToleratesTheWayAnAgentQuotesAndSpacesNames()
    {
        BmotionPropertyCatalog.BuildTarget([" x ", "\"opacity\"", "BACKGROUNDCOLOR"], out var unknown);

        Assert.AreEqual(0, unknown.Length, $"Rejected: {string.Join(", ", unknown)}.");
    }

    /// <summary>
    /// The dictionary-valued escape hatches have no single representative value, so they cannot be
    /// probed - and are reported as unknown rather than silently becoming a no-op target.
    /// </summary>
    [TestMethod]
    public void BuildTarget_TheDictionaryValuedArguments_AreReportedAsUnprobeable()
    {
        BmotionPropertyCatalog.BuildTarget(["css", "cssVars"], out var unknown);

        CollectionAssert.AreEquivalent(new[] { "css", "cssVars" }, unknown);
    }

    [TestMethod]
    public void BuildTarget_NoNames_IsAnEmptyTargetRatherThanAThrow()
    {
        BmotionPropertyCatalog.BuildTarget([], out var unknown);

        Assert.AreEqual(0, unknown.Length);
    }

    [TestMethod]
    public void ExplainPlayback_WhenOffloaded_ExplainsAndOffersNothingToFix()
    {
        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x", "opacity"], new BmTween(), offloaded: true);

        StringAssert.Contains(reason, "Blazor Server");
        Assert.AreEqual(0, remedies.Length);
    }

    [TestMethod]
    [DataRow("width", "scale")]
    [DataRow("height", "scale")]
    [DataRow("top", "x/y")]
    [DataRow("left", "x/y")]
    [DataRow("backgroundColor", "opacity")]
    public void ExplainPlayback_NamesTheCompositorFriendlyReplacement(string property, string replacement)
    {
        var (_, remedies) = BmotionPropertyCatalog.ExplainPlayback([property], new BmTween(), offloaded: false);

        Assert.IsTrue(remedies.Any(remedy => remedy.Contains(replacement, StringComparison.OrdinalIgnoreCase)),
                      $"'{property}' was not pointed at {replacement}. Remedies: {string.Join(" | ", remedies)}");
    }

    [TestMethod]
    public void ExplainPlayback_ATweenWithNoDuration_IsNamedAsTheBlocker()
    {
        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], new BmTween { Duration = 0 }, offloaded: false);

        StringAssert.Contains(reason, "no duration");
        Assert.IsTrue(remedies.Any(remedy => remedy.Contains("duration", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ExplainPlayback_AReverseRepeat_IsNamed_WithTheMirrorEquivalent()
    {
        var transition = new BmTween { Repeat = BmRepeat.Reverse() };

        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], transition, offloaded: false);

        StringAssert.Contains(reason, "Reverse");
        Assert.IsTrue(remedies.Any(remedy => remedy.Contains("Mirror", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ExplainPlayback_ARepeatDelay_IsNamedAsTheBlocker()
    {
        var transition = new BmTween { Repeat = BmRepeat.Loop(3, delay: 0.5) };

        var (reason, _) = BmotionPropertyCatalog.ExplainPlayback(["x"], transition, offloaded: false);

        StringAssert.Contains(reason, "delay between iterations");
    }

    [TestMethod]
    public void ExplainPlayback_AnOnUpdateCallback_IsNamedAsTheBlocker()
    {
        var transition = new BmTween { OnUpdate = _ => { } };

        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], transition, offloaded: false);

        StringAssert.Contains(reason, "OnUpdate");
        Assert.AreNotEqual(0, remedies.Length);
    }

    /// <summary>
    /// A verdict the tool cannot explain is the most misleading answer it can give, so it always
    /// falls back to the general rule rather than to an empty list.
    /// </summary>
    [TestMethod]
    public void ExplainPlayback_WithNoKnownBlocker_StillSaysWhatToDo()
    {
        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], new BmTween { Duration = 0.3 }, offloaded: false);

        StringAssert.Contains(reason, "worth reporting");
        Assert.AreNotEqual(0, remedies.Length);
    }

    [TestMethod]
    [DataRow("x", true)]
    [DataRow("X", true)]
    [DataRow("scaleX", true)]
    [DataRow("perspective", true)]
    [DataRow("opacity", true)]
    [DataRow("width", false)]
    [DataRow("originX", false)]
    [DataRow("backgroundColor", false)]
    public void IsCompositorProperty_IsCaseInsensitiveAndCoversTheTransformComponents(string name, bool expected)
    {
        Assert.AreEqual(expected, BmotionPropertyCatalog.IsCompositorProperty(name));
    }
}
