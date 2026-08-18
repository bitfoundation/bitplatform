using System.Globalization;

namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The parser that turns a string an agent typed into the transition the engine runs.
/// <para>
/// It is the entry point of every measuring tool, so a spelling it silently mis-reads does not
/// produce an error - it produces a confident measurement of the wrong motion. These tests pin the
/// family of spellings it promises to accept, and pin the two failure modes that have to stay
/// visible: an unreadable spec comes back as an error rather than as a default, and an argument
/// that was not applied comes back as a warning rather than being dropped.
/// </para>
/// </summary>
[TestClass]
public class TransitionSpecTests
{
    [TestMethod]
    [DataRow("spring(stiffness: 260, damping: 12)")]
    [DataRow("Bm.Spring(stiffness: 260, damping: 12)")]
    [DataRow("Motion.Spring(stiffness: 260, damping: 12)")]
    [DataRow("Spring(stiffness=260, damping=12)")]
    [DataRow("spring stiffness=260 damping=12")]
    [DataRow("  SPRING( STIFFNESS : 260 , DAMPING : 12 )  ")]
    [DataRow("spring(260, 12)")]
    public void Parse_EverySpellingOfTheSameSpring_ProducesTheSameTransition(string spec)
    {
        var result = BmotionTransitionSpec.Parse(spec);

        Assert.IsNull(result.Error, $"'{spec}' was rejected: {result.Error}");

        var spring = result.Transition as BmSpring;

        Assert.IsNotNull(spring, $"'{spec}' did not parse as a spring but as {result.Transition?.GetType().Name}.");
        Assert.AreEqual(260, spring.Stiffness);
        Assert.AreEqual(12, spring.Damping);
    }

    [TestMethod]
    public void Parse_PositionalArguments_FollowTheBmFactorySignatures()
    {
        var spring = (BmSpring)BmotionTransitionSpec.Parse("spring(260, 12, 1.5)").Transition!;

        Assert.AreEqual(260, spring.Stiffness);
        Assert.AreEqual(12, spring.Damping);
        Assert.AreEqual(1.5, spring.Mass);

        var tween = (BmTween)BmotionTransitionSpec.Parse("tween(0.4, InOut)").Transition!;

        Assert.AreEqual(0.4, tween.Duration);
        Assert.AreEqual(BmEase.InOut, tween.Ease);

        var inertia = (BmInertia)BmotionTransitionSpec.Parse("inertia(500, 700, 0.8)").Transition!;

        Assert.AreEqual(500, inertia.Velocity);
        Assert.AreEqual(700, inertia.TimeConstant);
        Assert.AreEqual(0.8, inertia.Power);
    }

    [TestMethod]
    public void Parse_EmptySpec_IsTheLibrarysOwnDefaultTween()
    {
        foreach (var spec in new string?[] { null, "", "   " })
        {
            var result = BmotionTransitionSpec.Parse(spec);

            Assert.IsNull(result.Error);
            Assert.IsInstanceOfType<BmTween>(result.Transition);
        }
    }

    [TestMethod]
    public void Parse_BareNumber_ReadsAsATweenDurationAndSaysSo()
    {
        var result = BmotionTransitionSpec.Parse("0.4, BackOut");

        var tween = result.Transition as BmTween;

        Assert.IsNotNull(tween);
        Assert.AreEqual(0.4, tween.Duration);
        Assert.AreEqual(BmEase.BackOut, tween.Ease);
        // The assumption is the kind of thing that produces a confidently wrong answer if unstated.
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("read as a tween", StringComparison.OrdinalIgnoreCase)),
                      $"No warning named the assumption. Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    [DataRow("bounce(0.4)")]
    [DataRow("easeOut")]
    [DataRow("cubic-bezier(0.4, 0, 0.2, 1)")]
    public void Parse_AnUnknownKind_IsAnErrorThatNamesTheKindsThatExist(string spec)
    {
        var result = BmotionTransitionSpec.Parse(spec);

        Assert.IsNull(result.Transition, $"'{spec}' should not have produced a transition.");
        Assert.IsNotNull(result.Error);

        foreach (var kind in BmotionTransitionSpec.Kinds)
        {
            StringAssert.Contains(result.Error, kind, $"The error does not name '{kind}': {result.Error}");
        }
    }

    [TestMethod]
    public void Parse_AMisspelledArgument_Warns_RatherThanSilentlyBecomingADefault()
    {
        var result = BmotionTransitionSpec.Parse("spring(stifness: 260, damping: 12)");

        Assert.IsNotNull(result.Transition);
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("stifness", StringComparison.Ordinal)),
                      $"The misspelling was dropped silently. Warnings: {string.Join(" | ", result.Warnings)}");

        // The rest of the spec still applied: a typo costs one argument, not the whole call.
        Assert.AreEqual(12, ((BmSpring)result.Transition).Damping);
    }

    [TestMethod]
    public void Parse_ANonNumericValue_Warns_AndLeavesTheArgumentUnset()
    {
        var result = BmotionTransitionSpec.Parse("spring(stiffness: lots, damping: 12)");

        var spring = (BmSpring)result.Transition!;

        Assert.AreNotEqual(0, result.Warnings.Length);
        Assert.AreEqual(12, spring.Damping);
    }

    [TestMethod]
    public void Parse_BounceWithStiffness_WarnsThatOneOfThemIsUnused()
    {
        var result = BmotionTransitionSpec.Parse("spring(bounce: 0.4, duration: 0.6, stiffness: 260, damping: 12)");

        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("Bounce wins", StringComparison.OrdinalIgnoreCase)),
                      $"Nothing said the physics arguments are ignored. Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    public void Parse_BounceWithoutDuration_WarnsThatTheDurationDefaultApplies()
    {
        var result = BmotionTransitionSpec.Parse("spring(bounce: 0.4)");

        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("duration", StringComparison.OrdinalIgnoreCase)),
                      $"Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    public void Parse_SecondsWrittenWithAUnit_IsStillANumber()
    {
        var tween = (BmTween)BmotionTransitionSpec.Parse("tween(duration: 0.4s)").Transition!;

        Assert.AreEqual(0.4, tween.Duration);
    }

    [TestMethod]
    public void Parse_ABezier_KeepsItsOwnCommasTogether()
    {
        var tween = (BmTween)BmotionTransitionSpec.Parse("tween(duration: 0.5, bezier: [0.42, 0, 0.58, 1])").Transition!;

        Assert.AreEqual(0.5, tween.Duration);
        CollectionAssert.AreEqual(new[] { 0.42, 0, 0.58, 1.0 }, tween.Bezier);
    }

    [TestMethod]
    public void Parse_ABezierWithTooFewNumbers_IsRefusedRatherThanTruncated()
    {
        var result = BmotionTransitionSpec.Parse("tween(duration: 0.5, bezier: [0.42, 0])");

        Assert.IsNull(((BmTween)result.Transition!).Bezier);
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("four numbers", StringComparison.OrdinalIgnoreCase)),
                      $"Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    public void Parse_APositionalArgumentPastTheSignature_IsReportedRatherThanDropped()
    {
        var result = BmotionTransitionSpec.Parse("tween(0.4, InOut, 99)");

        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("99", StringComparison.Ordinal)),
                      $"Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    [DataRow("InOut", BmEase.InOut)]
    [DataRow("inout", BmEase.InOut)]
    [DataRow("BmEase.BackOut", BmEase.BackOut)]
    [DataRow("ease.BackOut", BmEase.BackOut)]
    [DataRow("easeBackOut", BmEase.BackOut)]
    [DataRow("Linear", BmEase.Linear)]
    public void TryEase_AcceptsEverySpellingAnAgentIsLikelyToWrite(string text, BmEase expected)
    {
        Assert.IsTrue(BmotionTransitionSpec.TryEase(text, out var ease), $"'{text}' was not recognised.");
        Assert.AreEqual(expected, ease);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("swoosh")]
    public void TryEase_RejectsWhatIsNotAnEasing(string text)
    {
        Assert.IsFalse(BmotionTransitionSpec.TryEase(text, out _));
    }

    [TestMethod]
    public void Parse_AnUnknownEasing_KeepsTheDefaultAndPointsAtTheToolThatLists()
    {
        var result = BmotionTransitionSpec.Parse("tween(0.4, swoosh)");

        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("GetBmotionEasings", StringComparison.Ordinal)),
                      $"Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    public void Parse_TheCanonicalCall_IsTheCodeToWrite_AndParsesBackToTheSameTransition()
    {
        foreach (var spec in new[]
        {
            "spring(stiffness: 260, damping: 12, mass: 1.2)",
            "spring(bounce: 0.4, duration: 0.6)",
            "tween(0.35, BackOut)",
            "inertia(velocity: 500, power: 0.8)",
        })
        {
            var first = BmotionTransitionSpec.Parse(spec);

            StringAssert.StartsWith(first.Canonical, "Bm.", $"'{spec}' produced '{first.Canonical}', which is not a C# call.");

            // Round-tripping is the property that makes the canonical form safe to paste into code:
            // what it says was understood has to mean the same thing when read back.
            var second = BmotionTransitionSpec.Parse(first.Canonical);

            Assert.AreEqual(first.Canonical, second.Canonical,
                            $"'{spec}' did not round-trip: '{first.Canonical}' -> '{second.Canonical}'.");
        }
    }

    /// <summary>
    /// The server runs with InvariantGlobalization, but a spec is parsed on whatever thread culture
    /// the host hands it. A comma-decimal culture must not read "0.4" as 4.
    /// </summary>
    [TestMethod]
    public void Parse_ANumber_IsReadTheSameWayUnderACommaDecimalCulture()
    {
        var original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var tween = (BmTween)BmotionTransitionSpec.Parse("tween(duration: 0.4)").Transition!;

            Assert.AreEqual(0.4, tween.Duration);

            // And the canonical call it writes back has to be C#, not localized.
            StringAssert.Contains(BmotionTransitionSpec.Parse("spring(mass: 1.5)").Canonical, "1.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    public void Parse_IsTotal_NoInputThrows()
    {
        foreach (var spec in new[]
        {
            "spring(", ")", "((((", "spring(:::)", "spring(,,,)", "tween(bezier: [)",
            "spring(stiffness:)", "=", ":", "spring(stiffness: 1e400)", new string('x', 5_000),
        })
        {
            var result = BmotionTransitionSpec.Parse(spec);

            // Either it read something or it explained why not - never both empty, never an exception.
            Assert.IsTrue(result.Transition is not null || result.Error is not null,
                          $"'{spec[..Math.Min(40, spec.Length)]}' produced neither a transition nor an error.");
        }
    }
}
