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

    /// <summary>
    /// One named argument, written the way anyone writes it.
    /// <para>
    /// This is the shape that used to be read as something else entirely. An argument list with no
    /// comma in it was split on whitespace, so "damping: 30" became the two tokens "damping:" and
    /// "30" - the first a name with no value, the second a bare number that fell into the first
    /// positional slot. "spring(damping: 30)" therefore came back measured as a spring of stiffness
    /// 30, and "spring(bounce: 0.4)" as one of stiffness 0.4, which does not settle at all. The
    /// answer was a confident measurement of a transition nobody asked about, which is the one
    /// failure mode a measuring tool must not have.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow("spring(damping: 30)", 30)]
    [DataRow("spring(damping:30)", 30)]
    [DataRow("spring(damping = 30)", 30)]
    [DataRow("spring damping: 30", 30)]
    public void Parse_ASingleNamedArgument_LandsInTheArgumentItNames(string spec, double damping)
    {
        var spring = BmotionTransitionSpec.Parse(spec).Transition as BmSpring;

        Assert.IsNotNull(spring, $"'{spec}' did not parse as a spring.");
        Assert.AreEqual(damping, spring.Damping, $"'{spec}' did not set damping.");

        // And nothing leaked into the slot a positional argument would have taken.
        Assert.AreEqual(new BmSpring().Stiffness, spring.Stiffness, $"'{spec}' also wrote to stiffness.");
    }

    [TestMethod]
    [DataRow("spring(bounce: 0.4)")]
    [DataRow("spring(mass: 2)")]
    [DataRow("spring(duration: 1.2)")]
    public void Parse_ASingleNamedArgument_IsNotReportedAsUnreadable(string spec)
    {
        var result = BmotionTransitionSpec.Parse(spec);

        Assert.IsNull(result.Error);
        Assert.DoesNotContain(warning => warning.Contains("is not a number", StringComparison.Ordinal), result.Warnings,
                       $"'{spec}' lost its value. Warnings: {string.Join(" | ", result.Warnings)}");
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
        Assert.Contains(warning => warning.Contains("read as a tween", StringComparison.OrdinalIgnoreCase), result.Warnings,
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
            Assert.Contains(kind, result.Error, $"The error does not name '{kind}': {result.Error}");
        }
    }

    [TestMethod]
    public void Parse_AMisspelledArgument_Warns_RatherThanSilentlyBecomingADefault()
    {
        var result = BmotionTransitionSpec.Parse("spring(stifness: 260, damping: 12)");

        Assert.IsNotNull(result.Transition);
        Assert.Contains(warning => warning.Contains("stifness", StringComparison.Ordinal), result.Warnings,
                      $"The misspelling was dropped silently. Warnings: {string.Join(" | ", result.Warnings)}");

        // The rest of the spec still applied: a typo costs one argument, not the whole call.
        Assert.AreEqual(12, ((BmSpring)result.Transition).Damping);
    }

    [TestMethod]
    public void Parse_ANonNumericValue_Warns_AndLeavesTheArgumentUnset()
    {
        var result = BmotionTransitionSpec.Parse("spring(stiffness: lots, damping: 12)");

        var spring = (BmSpring)result.Transition!;

        Assert.IsNotEmpty(result.Warnings);
        Assert.AreEqual(12, spring.Damping);
    }

    [TestMethod]
    public void Parse_BounceWithStiffness_WarnsThatOneOfThemIsUnused()
    {
        var result = BmotionTransitionSpec.Parse("spring(bounce: 0.4, duration: 0.6, stiffness: 260, damping: 12)");

        Assert.Contains(warning => warning.Contains("the explicit values are unused", StringComparison.OrdinalIgnoreCase), result.Warnings,
                      $"Nothing said the physics arguments are ignored. Warnings: {string.Join(" | ", result.Warnings)}");
    }

    /// <summary>
    /// A duration alone switches the spring to the derived model just as bounce does - the engine's
    /// test is "Duration.HasValue || Bounce.HasValue" - so the stiffness beside it is just as unused,
    /// and used to go unmentioned because only bounce was looked for.
    /// </summary>
    [TestMethod]
    public void Parse_DurationWithStiffness_WarnsThatTheStiffnessIsUnused()
    {
        var result = BmotionTransitionSpec.Parse("spring(duration: 0.6, stiffness: 260)");

        Assert.Contains(warning => warning.Contains("the explicit values are unused", StringComparison.OrdinalIgnoreCase), result.Warnings,
                      $"Nothing said the stiffness is ignored. Warnings: {string.Join(" | ", result.Warnings)}");
    }

    /// <summary>
    /// 'visualDuration' is motion.dev's spelling of the same argument, and the parser writes it to the
    /// same spring.Duration - so it switches the spring to the derived model too, and the stiffness
    /// beside it is just as unused as it is under the 'duration' spelling.
    /// </summary>
    [TestMethod]
    public void Parse_VisualDurationWithStiffness_WarnsThatTheStiffnessIsUnused()
    {
        var result = BmotionTransitionSpec.Parse("spring(visualDuration: 0.6, stiffness: 260)");

        Assert.Contains(warning => warning.Contains("the explicit values are unused", StringComparison.OrdinalIgnoreCase), result.Warnings,
                      $"Nothing said the stiffness is ignored. Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    public void Parse_BounceWithoutDuration_WarnsThatTheDurationDefaultApplies()
    {
        var result = BmotionTransitionSpec.Parse("spring(bounce: 0.4)");

        Assert.Contains(warning => warning.Contains("duration", StringComparison.OrdinalIgnoreCase), result.Warnings,
                      $"Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    public void Parse_SecondsWrittenWithAUnit_IsStillANumber()
    {
        var tween = (BmTween)BmotionTransitionSpec.Parse("tween(duration: 0.4s)").Transition!;

        Assert.AreEqual(0.4, tween.Duration);
    }

    /// <summary>
    /// Bm.Tween takes the duration first, so "tween(BackOut)" - the way an easing-only tween is
    /// written - lands in the duration slot. Reading it as a bad duration would throw away the one
    /// thing the spec says.
    /// </summary>
    [TestMethod]
    [DataRow("tween(BackOut)", BmEase.BackOut)]
    [DataRow("tween(Linear)", BmEase.Linear)]
    [DataRow("tween(BmEase.InOut)", BmEase.InOut)]
    public void Parse_AnEasingInThePositionalDurationSlot_IsReadAsTheEasing(string spec, BmEase expected)
    {
        var result = BmotionTransitionSpec.Parse(spec);

        Assert.AreEqual(expected, ((BmTween)result.Transition!).Ease);
        Assert.DoesNotContain(warning => warning.Contains("is not a number", StringComparison.Ordinal), result.Warnings,
                       $"The easing was reported as a bad duration. Warnings: {string.Join(" | ", result.Warnings)}");
    }

    /// <summary>A first argument that is neither a number nor an easing is still a bad duration.</summary>
    [TestMethod]
    public void Parse_ANonsenseFirstArgument_IsStillReportedAsABadDuration()
    {
        var result = BmotionTransitionSpec.Parse("tween(swoosh)");

        Assert.Contains(warning => warning.Contains("is not a number", StringComparison.Ordinal), result.Warnings,
                      $"Warnings: {string.Join(" | ", result.Warnings)}");
    }

    /// <summary>
    /// Milliseconds are how the CSS and Web Animations worlds write a duration, so an agent copying
    /// one across writes it that way. Trimming the trailing "s" off it would leave "300m"; dropping
    /// the unit without converting would leave a five-minute tween.
    /// </summary>
    [TestMethod]
    [DataRow("tween(duration: 300ms)", 0.3)]
    [DataRow("tween(duration: 300MS)", 0.3)]
    [DataRow("tween(400ms)", 0.4)]
    public void Parse_ADurationInMilliseconds_IsConvertedToSeconds(string spec, double expected)
    {
        var result = BmotionTransitionSpec.Parse(spec);

        Assert.AreEqual(expected, ((BmTween)result.Transition!).Duration, 1e-9);
        Assert.DoesNotContain(warning => warning.Contains("is not a number", StringComparison.Ordinal), result.Warnings,
                       $"Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    public void Parse_ABezier_KeepsItsOwnCommasTogether()
    {
        var tween = (BmTween)BmotionTransitionSpec.Parse("tween(duration: 0.5, bezier: [0.42, 0, 0.58, 1])").Transition!;

        Assert.AreEqual(0.5, tween.Duration);
        Assert.AreSequenceEqual(new[] { 0.42, 0, 0.58, 1.0 }, tween.Bezier);
    }

    [TestMethod]
    public void Parse_ABezierWithTooFewNumbers_IsRefusedRatherThanTruncated()
    {
        var result = BmotionTransitionSpec.Parse("tween(duration: 0.5, bezier: [0.42, 0])");

        Assert.IsNull(((BmTween)result.Transition!).Bezier);
        Assert.Contains(warning => warning.Contains("four numbers", StringComparison.OrdinalIgnoreCase), result.Warnings,
                      $"Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    public void Parse_APositionalArgumentPastTheSignature_IsReportedRatherThanDropped()
    {
        var result = BmotionTransitionSpec.Parse("tween(0.4, InOut, 99)");

        Assert.Contains(warning => warning.Contains("99", StringComparison.Ordinal), result.Warnings,
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

        Assert.Contains(warning => warning.Contains("GetBmotionEasings", StringComparison.Ordinal), result.Warnings,
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

            Assert.StartsWith("Bm.", first.Canonical, $"'{spec}' produced '{first.Canonical}', which is not a C# call.");

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
            Assert.Contains("1.5", BmotionTransitionSpec.Parse("spring(mass: 1.5)").Canonical);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// Whether an argument exists and whether its value reads as a number are two questions. Answered
    /// as one, a good name with a bad value comes back as a name the kind does not have - which sends
    /// the caller off renaming the argument that was already right.
    /// </summary>
    [TestMethod]
    [DataRow("spring(stiffness: fast)", "spring")]
    [DataRow("tween(duration: quick)", "tween")]
    [DataRow("inertia(velocity: fast)", "inertia")]
    public void Parse_AKnownArgumentWithAnUnreadableValue_IsOnlyReportedAsABadValue(string spec, string kind)
    {
        var result = BmotionTransitionSpec.Parse(spec);

        Assert.IsNull(result.Error);

        Assert.Contains(warning => warning.Contains("is not a number", StringComparison.Ordinal), result.Warnings,
                      $"Nothing said the value could not be read. Warnings: {string.Join(" | ", result.Warnings)}");

        Assert.DoesNotContain(warning => warning.Contains($"is not an argument of a {kind}", StringComparison.Ordinal), result.Warnings,
                       $"A correct argument name was reported as unknown. Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    [DataRow("spring(stifness: 260)", "spring")]
    [DataRow("tween(durration: 0.4)", "tween")]
    [DataRow("inertia(velcity: 500)", "inertia")]
    public void Parse_AMisspelledArgument_StillNamesTheOnesThatExist(string spec, string kind)
    {
        var result = BmotionTransitionSpec.Parse(spec);

        Assert.Contains(warning => warning.Contains($"is not an argument of a {kind}", StringComparison.Ordinal), result.Warnings,
                      $"A misspelled argument passed unremarked. Warnings: {string.Join(" | ", result.Warnings)}");
    }

    [TestMethod]
    [DataRow("spring(stiffness: 100, damping: 0, delay: -1e300)")]
    [DataRow("spring(delay: -21)")]
    [DataRow("tween(0.4, delay: 30)")]
    [DataRow("inertia(velocity: 500, delay: -1e9)")]
    [DataRow("spring(delay: NaN)")]
    public void Parse_ADelayPastTheSimulatedTime_IsRefused(string spec)
    {
        var result = BmotionTransitionSpec.Parse(spec);

        Assert.IsNull(result.Transition);
        Assert.Contains("delay", result.Error!);
    }

    [TestMethod]
    [DataRow("spring(delay: -0.5)", -0.5)]
    [DataRow("tween(0.4, delay: 300ms)", 0.3)]
    [DataRow("inertia(velocity: 500, delay: -20)", -20.0)]
    public void Parse_AnOrdinaryDelay_IsKept(string spec, double delay)
    {
        var result = BmotionTransitionSpec.Parse(spec);

        Assert.IsNull(result.Error, $"'{spec}' was rejected: {result.Error}");
        Assert.IsNotNull(result.Transition);
        Assert.AreEqual(delay, result.Transition.Delay, 1e-9);
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
