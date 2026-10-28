using System.Reflection;

namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The animatable-property catalog: the list is reflected off <see cref="BmProps"/> and the
/// expensive claim on each entry - whether the browser plays it, as a timeline or a recording, and
/// therefore whether it animates or jumps on Blazor Server - is measured by running the engine.
/// <para>
/// Two things can go wrong and neither shows up as an exception. A property added to the library
/// can arrive with no description, so the tool answers about it in placeholders. And the measured
/// verdict can disagree with <see cref="BmotionPropertyCatalog.IsFrameLoopOnlyProperty"/>, which is
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
            .Where(entry => entry.PlayedByBrowser == BmotionPropertyCatalog.IsFrameLoopOnlyProperty(entry.Name))
            .Select(entry => $"{entry.Name}: engine says played by the browser = {entry.PlayedByBrowser}, " +
                             $"IsFrameLoopOnlyProperty says {BmotionPropertyCatalog.IsFrameLoopOnlyProperty(entry.Name)}")
            .ToArray();

        Assert.AreEqual(0, disagreements.Length, string.Join("; ", disagreements));
    }

    [TestMethod]
    [DataRow("x")]
    [DataRow("y")]
    [DataRow("scale")]
    [DataRow("rotate")]
    [DataRow("opacity")]
    public async Task Get_TheCheapProperties_AreMeasuredAsCompositorTimelines(string name)
    {
        var entry = (await BmotionPropertyCatalog.GetAsync()).Single(property => property.Name == name);

        Assert.IsTrue(entry.PlayedByBrowser, $"'{name}' was not handed to the browser.");
        Assert.AreEqual("Timeline", entry.BrowserPlayback);
        Assert.IsTrue(entry.CompositorEligible, $"'{name}' is documented as compositor-cheap but was not reported so.");
        Assert.AreEqual("Animates", entry.OnBlazorServer);
    }

    /// <summary>
    /// The properties the catalog used to measure as needing the frame loop - and so as jumping on
    /// Blazor Server. Measured as Blazor Server now, the engine hands every one of them to the
    /// browser as a timeline. They are still not compositor properties: they cost layout or paint.
    /// </summary>
    [TestMethod]
    [DataRow("width")]
    [DataRow("height")]
    [DataRow("backgroundColor")]
    [DataRow("top")]
    [DataRow("filter")]
    [DataRow("boxShadow")]
    public async Task Get_ThePropertiesThatUsedToNeedTheFrameLoop_AreMeasuredAsPlayedByTheBrowserOnServer(string name)
    {
        var entry = (await BmotionPropertyCatalog.GetAsync()).Single(property => property.Name == name);

        Assert.IsTrue(entry.PlayedByBrowser, $"'{name}' was measured as needing the frame loop.");
        Assert.AreEqual("Timeline", entry.BrowserPlayback);
        Assert.AreEqual("Animates", entry.OnBlazorServer);
        Assert.IsFalse(entry.CompositorEligible, $"'{name}' was reported as running on the compositor.");
    }

    /// <summary>The one property no Web Animation can play: an SVG path morph is an attribute.</summary>
    [TestMethod]
    public async Task Get_AnSvgPathMorph_IsTheOnePropertyThatStillNeedsTheFrameLoop()
    {
        var catalog = await BmotionPropertyCatalog.GetAsync();

        var frameLoopOnly = catalog.Where(entry => entry.PlayedByBrowser is false).Select(entry => entry.Name).ToArray();

        CollectionAssert.AreEqual(new[] { "d" }, frameLoopOnly);

        var d = catalog.Single(entry => entry.Name == "d");

        Assert.IsNull(d.BrowserPlayback);
        Assert.AreEqual("Jumps to the target", d.OnBlazorServer);
    }

    /// <summary>
    /// A value the browser does not interpolate the way the engine does is recorded rather than
    /// described by a curve - and the dictionary-valued escape hatches are measured too, rather than
    /// written off as unprobeable.
    /// </summary>
    [TestMethod]
    public async Task Get_ACustomProperty_IsRecorded_AndAnArbitraryCssPropertyIsMeasuredLikeAnyOther()
    {
        var catalog = await BmotionPropertyCatalog.GetAsync();

        var cssVars = catalog.Single(entry => entry.Name == "cssVars");
        var css = catalog.Single(entry => entry.Name == "css");

        Assert.AreEqual("Recorded", cssVars.BrowserPlayback, "A custom property interpolates discretely; only a recording reproduces it.");
        Assert.AreEqual("Animates", cssVars.OnBlazorServer);

        // Its probe is a backdrop-filter, which the browser interpolates function by function.
        Assert.AreEqual("Timeline", css.BrowserPlayback);
    }

    [TestMethod]
    public async Task Get_OnBlazorServer_NeverContradictsTheVerdict()
    {
        foreach (var entry in await BmotionPropertyCatalog.GetAsync())
        {
            Assert.AreEqual(entry.PlayedByBrowser ? "Animates" : "Jumps to the target", entry.OnBlazorServer,
                            $"'{entry.Name}' says one thing in two fields.");

            // A shape exactly when the browser plays it, and only the two shapes there are.
            Assert.AreEqual(entry.PlayedByBrowser, entry.BrowserPlayback is not null, $"'{entry.Name}' has a shape it was not played in.");
            if (entry.BrowserPlayback is { } shape) CollectionAssert.Contains(new[] { "Timeline", "Recorded" }, shape);

            // The compositor is a part of the browser: nothing runs there that the browser was not handed.
            Assert.IsFalse(entry.CompositorEligible && entry.PlayedByBrowser is false, $"'{entry.Name}' is on a compositor it never reached.");
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
    public void ExplainPlayback_WhenPlayedAsATimeline_ExplainsAndOffersNothingToFix()
    {
        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x", "opacity"], new BmTween(), BmotionPlaybackPath.Timeline);

        StringAssert.Contains(reason, "Blazor Server");
        StringAssert.Contains(reason, "two keyframes");
        Assert.AreEqual(0, remedies.Length);
    }

    /// <summary>
    /// The advice that outlived the old rule: a length or a colour plays in the browser now, but off
    /// the compositor, so the explanation still names the compositor equivalent and what it saves.
    /// </summary>
    [TestMethod]
    [DataRow("width", "scale")]
    [DataRow("height", "scale")]
    [DataRow("top", "x/y")]
    [DataRow("left", "x/y")]
    [DataRow("backgroundColor", "opacity")]
    public void ExplainPlayback_APropertyOffTheCompositor_NamesTheCompositorEquivalent(string property, string replacement)
    {
        var (reason, _) = BmotionPropertyCatalog.ExplainPlayback([property], new BmTween(), BmotionPlaybackPath.Timeline);

        StringAssert.Contains(reason, replacement, $"'{property}' was not pointed at {replacement}.");
        StringAssert.Contains(reason, "compositor");
    }

    [TestMethod]
    public void ExplainPlayback_ATweenWithNoDuration_IsExplainedAsInstant()
    {
        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], new BmTween { Duration = 0 }, BmotionPlaybackPath.Instant);

        StringAssert.Contains(reason, "no duration");
        StringAssert.Contains(reason, "every runtime");
        Assert.IsTrue(remedies.Any(remedy => remedy.Contains("duration", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ExplainPlayback_AnEndlessReverseRepeat_IsNamedAsTheFrameLoopBlocker_WithTheMirrorEquivalent()
    {
        var transition = new BmTween { Repeat = BmRepeat.Reverse() };

        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], transition, BmotionPlaybackPath.FrameLoop);

        StringAssert.Contains(reason, "Reverse");
        Assert.IsTrue(remedies.Any(remedy => remedy.Contains("Mirror", StringComparison.Ordinal)));
    }

    /// <summary>A Reverse with a count is recorded, not left to the frame loop - and Loop is what makes it a timeline.</summary>
    [TestMethod]
    public void ExplainPlayback_ACountedReverseRepeat_IsNamedAsWhatMakesItARecording()
    {
        var transition = new BmTween { Repeat = BmRepeat.Reverse(2) };

        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], transition, BmotionPlaybackPath.Recorded);

        StringAssert.Contains(reason, "Reverse");
        Assert.IsTrue(remedies.Any(remedy => remedy.Contains("Loop", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ExplainPlayback_ARepeatDelay_IsNamedAsWhatMakesItARecording()
    {
        var transition = new BmTween { Repeat = BmRepeat.Loop(3, delay: 0.5) };

        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], transition, BmotionPlaybackPath.Recorded);

        StringAssert.Contains(reason, "waits between iterations");
        Assert.IsTrue(remedies.Any(remedy => remedy.Contains("delay", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ExplainPlayback_ARecording_StatesWhatItCostsAgainstATimeline()
    {
        var (reason, _) = BmotionPropertyCatalog.ExplainPlayback(["x"], new BmInertia(), BmotionPlaybackPath.Recorded);

        StringAssert.Contains(reason, "recording");
        StringAssert.Contains(reason, "more keyframes");
        StringAssert.Contains(reason, "Blazor Server");
    }

    /// <summary>
    /// A property the catalog measured as recorded under a plain tween is recorded for its value, not
    /// for its transition - and the explanation says so rather than inventing a transition to blame.
    /// </summary>
    [TestMethod]
    public void ExplainPlayback_AValueTheBrowserCannotInterpolate_IsBlamedRatherThanTheTransition()
    {
        var (reason, _) = BmotionPropertyCatalog.ExplainPlayback(["offsetPath"], new BmTween(), BmotionPlaybackPath.Recorded, ["offsetPath"]);

        StringAssert.Contains(reason, "offsetPath is a value the browser does not interpolate");
    }

    [TestMethod]
    public void ExplainPlayback_AnOnUpdateCallback_IsNamedAsTheBlocker()
    {
        var transition = new BmTween { OnUpdate = _ => { } };

        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], transition, BmotionPlaybackPath.FrameLoop);

        StringAssert.Contains(reason, "OnUpdate");
        Assert.AreNotEqual(0, remedies.Length);
    }

    [TestMethod]
    public void ExplainPlayback_AnSvgPathMorph_IsNamedAsTheBlocker()
    {
        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["d"], new BmTween(), BmotionPlaybackPath.FrameLoop);

        StringAssert.Contains(reason, "SVG geometry attribute");
        Assert.AreNotEqual(0, remedies.Length);
    }

    [TestMethod]
    public void ExplainPlayback_ARepeatThatUnrollsPastTenSeconds_NamesTheRecordingLimit()
    {
        var transition = new BmTween { Duration = 4, Repeat = BmRepeat.Mirror(2) };

        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], transition, BmotionPlaybackPath.FrameLoop);

        StringAssert.Contains(reason, "12 seconds");
        StringAssert.Contains(reason, "10 seconds");
        Assert.IsTrue(remedies.Any(remedy => remedy.Contains("forever", StringComparison.Ordinal)));
    }

    /// <summary>
    /// A verdict the tool cannot explain is the most misleading answer it can give, so it always
    /// falls back to the general rule rather than to an empty list.
    /// </summary>
    [TestMethod]
    public void ExplainPlayback_WithNoKnownBlocker_StillSaysWhatToDo()
    {
        var (reason, remedies) = BmotionPropertyCatalog.ExplainPlayback(["x"], new BmTween { Duration = 0.3 }, BmotionPlaybackPath.FrameLoop);

        StringAssert.Contains(reason, "worth reporting");
        Assert.AreNotEqual(0, remedies.Length);
    }

    [TestMethod]
    [DataRow("d", true)]
    [DataRow("D", true)]
    [DataRow(" \"points\" ", true)]
    [DataRow("x", false)] // a transform component here, not the SVG attribute of the same name
    [DataRow("width", false)]
    [DataRow("backgroundColor", false)]
    [DataRow("pathLength", false)]
    public void IsFrameLoopOnlyProperty_IsTheSvgGeometryAttributes(string name, bool expected)
    {
        Assert.AreEqual(expected, BmotionPropertyCatalog.IsFrameLoopOnlyProperty(name));
    }

    /// <summary>
    /// The starting value each probe reads off the page has the target's own shape, or the engine
    /// would jump from it - and the catalog would measure a jump no page with a stylesheet makes.
    /// </summary>
    [TestMethod]
    public void StartingStyles_HaveTheShapeOfTheTarget_UnderTheEngineKey()
    {
        var styles = BmotionPropertyCatalog.StartingStyles(["width", "filter", "backgroundColor", "originX", "opacity", "x"]);

        Assert.AreEqual("0px", styles["width"]);
        Assert.AreEqual("blur(0px)", styles["filter"]);
        Assert.AreEqual("rgba(0, 0, 0, 0)", styles["backgroundColor"]);
        Assert.AreEqual("0% 0%", styles["transformOrigin"], "The origin fractions animate as one transformOrigin.");
        Assert.AreEqual("1", styles["opacity"]);
        Assert.IsFalse(styles.ContainsKey("x"), "The engine owns the transform; it never reads a component off the page.");
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
