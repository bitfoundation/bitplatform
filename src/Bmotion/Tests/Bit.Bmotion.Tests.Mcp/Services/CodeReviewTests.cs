namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The review pass that closes the loop after an agent writes Bmotion markup.
/// <para>
/// Every rule it applies exists because the mistake it catches is silent - it compiles, deploys and
/// renders, and then does nothing. So the tests come in pairs: each rule has to fire on markup that
/// makes the mistake, and stay quiet on markup that does not. The second half is what keeps the
/// review usable at all; a rule that fires on correct code teaches an agent to ignore the review,
/// which costs more than never having run it.
/// </para>
/// </summary>
[TestClass]
public class CodeReviewTests
{
    /// <summary>
    /// One piece of markup per rule, each making exactly the mistake the rule is named after. It
    /// doubles as the coverage check below: a rule with no offender here is a rule nothing proves
    /// still fires.
    /// </summary>
    private static readonly Dictionary<string, string> Offenders = new(StringComparer.Ordinal)
    {
        ["exit-without-presence"] = """
            @if (show)
            {
                <Bmotion Initial="Bm.To(opacity: 0)"
                         Animate="Bm.To(opacity: 1)"
                         Exit="Bm.To(opacity: 0)">
                    <div class="panel">Content</div>
                </Bmotion>
            }
            """,

        ["spring-duration-without-bounce"] = """
            <Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Spring(duration: 0.5)">
                <div class="box" />
            </Bmotion>
            """,

        ["animate-without-initial"] = """
            <Bmotion Animate="Bm.To(opacity: 1)">
                <div class="card">Content</div>
            </Bmotion>
            """,

        ["missing-key-in-loop"] = """
            @foreach (var item in Items)
            {
                <Bmotion Initial="Bm.To(opacity: 0)" Animate="Bm.To(opacity: 1)">
                    <li>@item.Title</li>
                </Bmotion>
            }
            """,

        ["nested-quotes-in-attribute"] = """
            <Bmotion Animate="Bm.To(backgroundColor: "#FD7F36")">
                <div class="box" />
            </Bmotion>
            """,

        ["component-as-animated-root"] = """
            <Bmotion Initial="Bm.To(opacity: 0)" Animate="Bm.To(opacity: 1)">
                <ProductCard Product="Product" />
            </Bmotion>
            """,

        ["empty-bmotion"] = """
            <Bmotion Initial="Bm.To(opacity: 0)" Animate="Bm.To(opacity: 1)" />
            """,

        ["frame-loop-only-properties"] = """
            <Bmotion Initial='Bm.To(height: "0px")' Animate='Bm.To(height: "320px")'>
                <div class="drawer" />
            </Bmotion>
            """,

        ["drag-without-capability-guard"] = """
            <Bmotion Drag="true" DragConstraints="new BmDragConstraints { Left = -100, Right = 100 }">
                <div class="handle" />
            </Bmotion>
            """,

        ["eased-infinite-rotation"] = """
            <Bmotion Animate="Bm.To(rotate: 360)"
                     Transition="Bm.Tween(1, repeat: BmRepeat.Forever)">
                <div class="spinner" />
            </Bmotion>
            """,

        ["resting-state-in-gesture"] = """
            <Bmotion WhileHover="Bm.To(scale: 1)" WhileTap="Bm.To(scale: 0.97)">
                <button class="card">Open</button>
            </Bmotion>
            """,

        ["reduced-motion-not-configured"] = """
            builder.Services.AddBitBmotionServices();
            """,
    };

    /// <summary>
    /// A rule listed in RulesApplied but unreachable is worse than no rule: the report says it was
    /// checked. Every declared rule needs markup here that provokes it.
    /// </summary>
    [TestMethod]
    public void Review_EveryDeclaredRule_HasMarkupThatProvokesIt()
    {
        CollectionAssert.AreEquivalent(BmotionCodeReview.Rules, Offenders.Keys.ToArray(),
                                       "The rule list and the offending samples have drifted apart.");
    }

    [TestMethod]
    public void Review_EachRule_FiresOnTheMistakeItIsNamedAfter()
    {
        foreach (var (rule, code) in Offenders)
        {
            var review = BmotionCodeReview.Review(code);

            Assert.IsTrue(review.Findings.Any(finding => finding.Rule == rule),
                          $"'{rule}' did not fire. It reported instead: " +
                          $"{string.Join(", ", review.Findings.Select(finding => finding.Rule).DefaultIfEmpty("nothing"))}.");
        }
    }

    [TestMethod]
    public void Review_EveryFinding_NamesALine_AndSaysWhatToDo()
    {
        foreach (var (rule, code) in Offenders)
        {
            var lineCount = code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Length;

            foreach (var finding in BmotionCodeReview.Review(code).Findings)
            {
                Assert.AreNotEqual(string.Empty, finding.Message.Trim(), $"'{finding.Rule}' reported an empty message.");
                Assert.AreNotEqual(string.Empty, finding.Fix.Trim(), $"'{finding.Rule}' reported no correction.");
                CollectionAssert.Contains(new[] { "Error", "Warning", "Suggestion" }, finding.Severity);

                Assert.IsNotNull(finding.Line, $"'{finding.Rule}' (on the '{rule}' sample) mapped to no line.");
                Assert.IsTrue(finding.Line >= 1 && finding.Line <= lineCount,
                              $"'{finding.Rule}' points at line {finding.Line} of a {lineCount}-line sample.");
            }
        }
    }

    /// <summary>
    /// The correct forms of the same markup. A rule that also fires on these is a rule an agent
    /// learns to ignore.
    /// </summary>
    [TestMethod]
    public void Review_TheCorrectFormOfEachMistake_IsNotReported()
    {
        var clean = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["exit-without-presence"] = """
                <BmotionAnimatePresence IsPresent="show">
                    <Bmotion Initial="Bm.To(opacity: 0)"
                             Animate="Bm.To(opacity: 1)"
                             Exit="Bm.To(opacity: 0)">
                        <div class="panel">Content</div>
                    </Bmotion>
                </BmotionAnimatePresence>
                """,

            ["spring-duration-without-bounce"] = """
                <Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Spring(bounce: 0.2, duration: 0.5)">
                    <div class="box" />
                </Bmotion>
                """,

            ["animate-without-initial"] = """
                <Bmotion Animate="_open ? Bm.To(opacity: 1) : Bm.To(opacity: 0)">
                    <div class="card">Content</div>
                </Bmotion>
                """,

            ["empty-bmotion"] = """
                <Bmotion Initial="Bm.To(opacity: 0)" Animate="Bm.To(opacity: 1)">
                    <div class="card">Content</div>
                </Bmotion>
                """,
            ["missing-key-in-loop"] = """
                @foreach (var item in Items)
                {
                    <Bmotion @key="item.Id" Initial="Bm.To(opacity: 0)" Animate="Bm.To(opacity: 1)">
                        <li>@item.Title</li>
                    </Bmotion>
                }
                """,

            ["nested-quotes-in-attribute"] = """
                <Bmotion Animate='Bm.To(backgroundColor: "#FD7F36")'>
                    <div class="box" />
                </Bmotion>
                """,

            ["component-as-animated-root"] = """
                <Bmotion Initial="Bm.To(opacity: 0)" Animate="Bm.To(opacity: 1)">
                    <div class="wrapper"><ProductCard Product="Product" /></div>
                </Bmotion>
                """,

            ["frame-loop-only-properties"] = """
                <Bmotion Initial="Bm.To(opacity: 0, y: 20)" Animate="Bm.To(opacity: 1, y: 0)">
                    <div class="card" />
                </Bmotion>
                """,

            ["eased-infinite-rotation"] = """
                <Bmotion Animate="Bm.To(rotate: 360)"
                         Transition="Bm.Tween(1, BmEase.Linear, repeat: BmRepeat.Forever)">
                    <div class="spinner" />
                </Bmotion>
                """,

            ["resting-state-in-gesture"] = """
                <Bmotion WhileHover="Bm.To(scale: 1.04)" WhileTap="Bm.To(scale: 0.97)">
                    <button class="card">Open</button>
                </Bmotion>
                """,

            ["reduced-motion-not-configured"] = """
                builder.Services.AddBitBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User);
                """,

            ["drag-without-capability-guard"] = """
                @if (Caps.SupportsFrameLoop)
                {
                    <Bmotion Drag="true">
                        <div class="handle" />
                    </Bmotion>
                }
                """,
        };

        CollectionAssert.AreEquivalent(Offenders.Keys.ToArray(), clean.Keys.ToArray(),
                                       "Every rule needs a correct form here as well as an offender: a rule with " +
                                       "no false-positive case is a rule nothing keeps honest.");

        foreach (var (rule, code) in clean)
        {
            var review = BmotionCodeReview.Review(code);

            Assert.IsFalse(review.Findings.Any(finding => finding.Rule == rule),
                           $"'{rule}' fired on markup that gets it right:\n{code}");
        }
    }

    /// <summary>
    /// Only the bound Animate value tells an entrance apart from a state change. An underscore
    /// anywhere else on the tag - a BEM class, an id, some unrelated attribute - is not state, and a
    /// rule that reads it as state goes quiet on exactly the markup it exists to catch.
    /// </summary>
    [TestMethod]
    public void Review_AnimateWithoutInitial_IsStillReportedWhenTheUnderscoreIsNotInAnimate()
    {
        var code = """
            <Bmotion Animate="Bm.To(opacity: 1)" class="card_body" id="hero_panel">
                <div class="card">Content</div>
            </Bmotion>
            """;

        Assert.IsTrue(BmotionCodeReview.Review(code).Findings.Any(finding => finding.Rule == "animate-without-initial"),
                      "An underscore outside the Animate value silenced the rule.");
    }

    [TestMethod]
    public void Review_PointsAtTheLineTheMistakeIsOn()
    {
        var code = """
            <div class="wrapper">
                <Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Spring(duration: 0.5)">
                    <div class="box" />
                </Bmotion>
            </div>
            """;

        var finding = BmotionCodeReview.Review(code).Findings.Single(entry => entry.Rule == "spring-duration-without-bounce");

        Assert.AreEqual(2, finding.Line);
    }

    [TestMethod]
    public void Review_TheMostSeriousFindingsComeFirst()
    {
        // One markup, three mistakes at three severities: a self-closed Bmotion (Error), a spring
        // duration with no bounce (Warning) and a frame-loop-only property (Suggestion).
        var code = """
            <Bmotion Animate="Bm.To(height: "0px")" Transition="Bm.Spring(duration: 0.5)" />
            """;

        var findings = BmotionCodeReview.Review(code).Findings;

        var order = findings.Select(finding => finding.Severity switch
        {
            "Error" => 0,
            "Warning" => 1,
            _ => 2
        }).ToArray();

        CollectionAssert.AreEqual(order.OrderBy(rank => rank).ToArray(), order,
                                  $"Reported in the order: {string.Join(", ", findings.Select(f => f.Severity))}.");
    }

    [TestMethod]
    public void Review_Passed_MeansNothingAboveASuggestionWasFound()
    {
        // An Error fails the review.
        Assert.IsFalse(BmotionCodeReview.Review("""<Bmotion Animate="Bm.To(x: 100)" />""").Passed);

        // A Warning fails it too.
        var warning = BmotionCodeReview.Review("""
            <Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Spring(duration: 0.5)">
                <div class="box" />
            </Bmotion>
            """);

        Assert.IsTrue(warning.Findings.All(finding => finding.Severity != "Error"));
        Assert.IsFalse(warning.Passed);

        // A Suggestion alone does not.
        var suggestion = BmotionCodeReview.Review("""
            <Bmotion Initial='Bm.To(height: "0px")' Animate='Bm.To(height: "320px")'>
                <div class="drawer" />
            </Bmotion>
            """);

        Assert.IsTrue(suggestion.Findings.All(finding => finding.Severity == "Suggestion"));
        Assert.IsTrue(suggestion.Passed);
    }

    [TestMethod]
    public void Review_CleanMarkup_PassesWithNothingToReport()
    {
        var code = """
            <BmotionAnimatePresence IsPresent="show">
                <Bmotion Initial="Bm.To(opacity: 0, scale: 0.95)"
                         Animate="Bm.To(opacity: 1, scale: 1)"
                         Exit="Bm.To(opacity: 0, scale: 0.95)"
                         Transition="Bm.Spring(bounce: 0.15, duration: 0.35)">
                    <div class="dialog">Content</div>
                </Bmotion>
            </BmotionAnimatePresence>
            """;

        var review = BmotionCodeReview.Review(code);

        Assert.AreEqual(0, review.Findings.Length,
                        $"Reported: {string.Join(", ", review.Findings.Select(finding => $"{finding.Rule}@{finding.Line}"))}.");
        Assert.IsTrue(review.Passed);
    }

    /// <summary>
    /// An empty result has to be distinguishable from an unchecked one, which is what RulesApplied
    /// is for - so it is present even when nothing was reviewed.
    /// </summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   \n  \t ")]
    public void Review_NothingToReview_PassesAndStillSaysWhatWouldHaveBeenChecked(string? code)
    {
        var review = BmotionCodeReview.Review(code);

        Assert.IsTrue(review.Passed);
        Assert.AreEqual(0, review.Findings.Length);
        CollectionAssert.AreEqual(BmotionCodeReview.Rules, review.RulesApplied);
    }

    [TestMethod]
    public void Review_AlwaysReportsTheRulesItApplied()
    {
        foreach (var code in Offenders.Values)
        {
            CollectionAssert.AreEqual(BmotionCodeReview.Rules, BmotionCodeReview.Review(code).RulesApplied);
        }
    }

    [TestMethod]
    public void Review_TheSameFrameLoopProperty_IsReportedOnceRatherThanPerOccurrence()
    {
        var code = """
            <Bmotion Initial='Bm.To(height: "0px")' Animate='Bm.To(height: "320px")'>
                <div class="drawer" />
            </Bmotion>
            """;

        var findings = BmotionCodeReview.Review(code)
            .Findings.Where(finding => finding.Rule == "frame-loop-only-properties")
            .ToArray();

        Assert.AreEqual(1, findings.Length, "The same property was reported for both ends of the animation.");
    }

    /// <summary>
    /// Variant-driven and timeline-driven markup animates on a state change rather than on mount,
    /// so the missing-Initial rule has to hold its tongue there.
    /// </summary>
    [TestMethod]
    public void Review_VariantAndTimelineDrivenMarkup_IsNotAskedForAnInitial()
    {
        foreach (var code in new[]
        {
            """
            <Bmotion Variants="Container" State="visible">
                <div class="list" />
            </Bmotion>
            """,
            """
            <Bmotion Timeline="BmScrollTimeline.Page()" Animate="Bm.To(scaleX: [0, 1])">
                <div class="progress-bar" />
            </Bmotion>
            """,
        })
        {
            Assert.IsFalse(BmotionCodeReview.Review(code).Findings.Any(finding => finding.Rule == "animate-without-initial"),
                           $"An Initial was demanded of:\n{code}");
        }
    }

    /// <summary>Nesting Bmotion's own components is normal; only a foreign component is a finding.</summary>
    [TestMethod]
    public void Review_ANestedBmotionComponent_IsNotMistakenForAnUnanimatableRoot()
    {
        var code = """
            <Bmotion Initial="Bm.To(opacity: 0)" Animate="Bm.To(opacity: 1)">
                <BmotionSplitText Text="Hello" />
            </Bmotion>
            """;

        Assert.IsFalse(BmotionCodeReview.Review(code).Findings.Any(finding => finding.Rule == "component-as-animated-root"));
    }

    [TestMethod]
    public void Review_IsTotal_NoInputThrows()
    {
        foreach (var code in new[]
        {
            "<Bmotion", "<<<<", "\"\"\"", "Bm.To(", "@foreach (", new string('<', 2_000),
            "<Bmotion Animate=\"Bm.To(x: 1)\">\n" + string.Join('\n', Enumerable.Repeat("<div />", 500)),
        })
        {
            var review = BmotionCodeReview.Review(code);

            Assert.IsNotNull(review.Findings);
            CollectionAssert.AreEqual(BmotionCodeReview.Rules, review.RulesApplied);
        }
    }

    [TestMethod]
    public void Review_ReadsBothLineEndings()
    {
        var windows = "<Bmotion Animate=\"Bm.To(x: 100)\" Transition=\"Bm.Spring(duration: 0.5)\">\r\n    <div />\r\n</Bmotion>";
        var unix = windows.Replace("\r\n", "\n", StringComparison.Ordinal);

        var fromWindows = BmotionCodeReview.Review(windows);
        var fromUnix = BmotionCodeReview.Review(unix);

        CollectionAssert.AreEqual(fromUnix.Findings.Select(finding => $"{finding.Rule}@{finding.Line}").ToArray(),
                                  fromWindows.Findings.Select(finding => $"{finding.Rule}@{finding.Line}").ToArray());
    }
}
