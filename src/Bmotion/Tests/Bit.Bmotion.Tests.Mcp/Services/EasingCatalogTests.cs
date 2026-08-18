namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The easing catalog turns preset names into numbers by sampling the library's own easing
/// implementation. The one fact it exists to publish is <c>Overshoots</c> - whether the curve
/// leaves the 0-1 range and so carries the element past its target - because that is what decides
/// whether a preset can be used on a drawer, a modal or anything else with a hard edge. These tests
/// hold that flag to the curve it is derived from, and pin the families whose answer is known.
/// </summary>
[TestClass]
public class EasingCatalogTests
{
    [TestMethod]
    public async Task Get_ListsEveryBmEasePreset_Once()
    {
        var catalog = await BmotionEasingCatalog.GetAsync();

        CollectionAssert.AreEquivalent(Enum.GetNames<BmEase>(), catalog.Select(easing => easing.Name).ToArray());
        Assert.AreEqual(catalog.Length, catalog.Select(easing => easing.Name).Distinct().Count());
    }

    [TestMethod]
    public async Task Get_EveryEntry_IsFullyPopulated()
    {
        foreach (var easing in await BmotionEasingCatalog.GetAsync())
        {
            Assert.AreEqual(11, easing.Curve.Length, $"BmEase.{easing.Name} was sampled at {easing.Curve.Length} points.");
            Assert.AreEqual(easing.Curve.Length, easing.Sparkline.Length,
                            $"BmEase.{easing.Name}'s sparkline does not match its curve.");
            Assert.AreNotEqual(string.Empty, easing.Feel.Trim(), $"BmEase.{easing.Name} has no guidance.");
            Assert.AreNotEqual(string.Empty, easing.Family.Trim());
            Assert.AreNotEqual(string.Empty, easing.Direction.Trim());

            foreach (var value in easing.Curve)
            {
                Assert.IsTrue(double.IsFinite(value), $"BmEase.{easing.Name} sampled a non-finite value.");
            }
        }
    }

    [TestMethod]
    public async Task Get_EveryCurve_RunsFromZeroToOne()
    {
        foreach (var easing in await BmotionEasingCatalog.GetAsync())
        {
            Assert.AreEqual(0, easing.Curve[0], 0.02, $"BmEase.{easing.Name} does not start at rest.");
            Assert.AreEqual(1, easing.Curve[^1], 0.02, $"BmEase.{easing.Name} does not arrive at the target.");
        }
    }

    /// <summary>
    /// The published flag has to be the curve's own property, not an opinion about the name - a
    /// preset renamed or re-tuned in the library must move the flag with it.
    /// </summary>
    [TestMethod]
    public async Task Get_TheOvershootFlag_IsDerivedFromTheCurveItPublishes()
    {
        foreach (var easing in await BmotionEasingCatalog.GetAsync())
        {
            var leavesRange = easing.Curve.Any(value => value < -0.001 || value > 1.001);

            Assert.AreEqual(leavesRange, easing.Overshoots,
                            $"BmEase.{easing.Name}: Overshoots={easing.Overshoots} but the curve " +
                            $"[{string.Join(", ", easing.Curve)}] says {leavesRange}.");
        }
    }

    /// <summary>
    /// Anticipate is deliberately not in this list: its wind-up is a shallow bezier dip of about
    /// one percent, which eleven samples do not reliably land on - so the catalog reports it as not
    /// overshooting, and that is the honest answer at this resolution.
    /// </summary>
    [TestMethod]
    [DataRow("BackOut")]
    [DataRow("BackIn")]
    [DataRow("BackInOut")]
    [DataRow("ElasticOut")]
    [DataRow("ElasticIn")]
    public async Task Get_ThePresetsWithAHardEdgeProblem_AreFlaggedAsOvershooting(string name)
    {
        var easing = (await BmotionEasingCatalog.GetAsync()).Single(entry => entry.Name == name);

        Assert.IsTrue(easing.Overshoots,
                      $"BmEase.{name} is not flagged, so it reads as safe on a drawer. " +
                      $"Curve: [{string.Join(", ", easing.Curve)}]");
    }

    [TestMethod]
    [DataRow("Linear")]
    [DataRow("In")]
    [DataRow("Out")]
    [DataRow("InOut")]
    [DataRow("QuadOut")]
    [DataRow("ExpoOut")]
    [DataRow("SineInOut")]
    public async Task Get_ThePresetsThatStayInsideTheirBounds_AreNotFlagged(string name)
    {
        var easing = (await BmotionEasingCatalog.GetAsync()).Single(entry => entry.Name == name);

        Assert.IsFalse(easing.Overshoots, $"BmEase.{name} was flagged as overshooting, which rules it out needlessly.");
    }

    [TestMethod]
    public async Task Get_Linear_IsSampledAsTheStraightLineItIs()
    {
        var linear = (await BmotionEasingCatalog.GetAsync()).Single(entry => entry.Name == "Linear");

        Assert.AreEqual("Linear", linear.Direction);
        Assert.AreEqual("Linear", linear.Family);

        for (int i = 0; i < linear.Curve.Length; i++)
        {
            Assert.AreEqual(i / 10.0, linear.Curve[i], 0.02);
        }
    }

    [TestMethod]
    [DataRow("QuadIn", "Quad", "In")]
    [DataRow("QuadOut", "Quad", "Out")]
    [DataRow("QuadInOut", "Quad", "InOut")]
    [DataRow("BackOut", "Back", "Out")]
    [DataRow("ExpoInOut", "Expo", "InOut")]
    [DataRow("Anticipate", "Anticipate", "Custom")]
    public async Task Get_TheFamilyAndDirection_AreReadOffThePresetName(string name, string family, string direction)
    {
        var easing = (await BmotionEasingCatalog.GetAsync()).Single(entry => entry.Name == name);

        Assert.AreEqual(family, easing.Family);
        Assert.AreEqual(direction, easing.Direction);
    }

    /// <summary>The bare In/Out/InOut members are the library's default cubic curves, not a family of their own.</summary>
    [TestMethod]
    [DataRow("In")]
    [DataRow("Out")]
    [DataRow("InOut")]
    public async Task Get_TheBareDirections_AreReportedAsTheCubicFamily(string name)
    {
        var easing = (await BmotionEasingCatalog.GetAsync()).Single(entry => entry.Name == name);

        Assert.AreEqual("Cubic", easing.Family);
    }

    [TestMethod]
    public async Task Get_AnEaseOut_CoversMoreGroundEarlyThanAnEaseIn()
    {
        var catalog = await BmotionEasingCatalog.GetAsync();

        var quadOut = catalog.Single(entry => entry.Name == "QuadOut").Curve;
        var quadIn = catalog.Single(entry => entry.Name == "QuadIn").Curve;

        // At a quarter of the way through: decelerating is ahead, accelerating is behind.
        Assert.IsTrue(quadOut[3] > 0.3, $"QuadOut at t=0.3 is {quadOut[3]}.");
        Assert.IsTrue(quadIn[3] < 0.3, $"QuadIn at t=0.3 is {quadIn[3]}.");
    }

    [TestMethod]
    public async Task Get_IsCachedRatherThanReSampledPerCall()
    {
        Assert.AreSame(await BmotionEasingCatalog.GetAsync(), await BmotionEasingCatalog.GetAsync());
    }
}
