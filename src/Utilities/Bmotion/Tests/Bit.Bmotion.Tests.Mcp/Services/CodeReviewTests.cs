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

        ["spring-physics-overridden-by-duration"] = """
            <Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Spring(stiffness: 300, duration: 0.5)">
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
            <Bmotion Initial='Bm.To(d: "M 0 0 L 100 0")' Animate='Bm.To(d: "M 0 0 L 100 40")'>
                <path class="wave" />
            </Bmotion>
            """,

        ["frame-loop-only-transition"] = """
            <Bmotion Animate="Bm.To(x: 100)"
                     Transition="Bm.Tween(1, repeat: BmRepeat.Reverse())">
                <div class="box" />
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

            // Duration on its own is a complete spring - it derives stiffness and damping with
            // bounce defaulting to 0.25 - and this rule used to fire on it, which is a warning
            // about correct code.
            ["spring-physics-overridden-by-duration"] = """
                <Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Spring(duration: 0.5)">
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

            // A length and a colour used to be reported here. The browser plays both now, on every
            // runtime, so naming them would be a warning about code that works.
            ["frame-loop-only-properties"] = """
                <Bmotion Initial='Bm.To(opacity: 0, height: "0px", backgroundColor: "#fff")'
                         Animate='Bm.To(opacity: 1, height: "320px", backgroundColor: "#FD7F36")'>
                    <div class="drawer" />
                </Bmotion>
                """,

            // A mirror repeats forever as one recorded cycle the browser loops - nothing to report.
            ["frame-loop-only-transition"] = """
                <Bmotion Animate="Bm.To(x: 100)"
                         Transition="Bm.Tween(1, repeat: BmRepeat.Mirror())">
                    <div class="box" />
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

    /// <summary>
    /// An app that lazy loads the library sets its options in a layout instead of registering them. Leaving the
    /// reduced-motion policy out there costs the same users the same thing.
    /// </summary>
    [TestMethod]
    public void Review_ReducedMotion_IsCheckedWhereALazyLoadingAppSetsItsOptions()
    {
        var offender = """
            protected override void OnInitialized() => Services.ConfigureBmotionServices(o =>
            {
                o.Prerender = BmPrerenderMode.Animate;
            });
            """;
        var clean = """
            protected override void OnInitialized() => Services.ConfigureBmotionServices(o =>
            {
                o.ReducedMotion = BmReducedMotionMode.User;
                o.Prerender = BmPrerenderMode.Animate;
            });
            """;

        var finding = BmotionCodeReview.Review(offender).Findings.Single(entry => entry.Rule == "reduced-motion-not-configured");

        Assert.AreEqual(1, finding.Line);
        Assert.StartsWith("ConfigureBmotionServices(", finding.Fix, StringComparison.Ordinal);
        Assert.IsFalse(BmotionCodeReview.Review(clean).Findings.Any(entry => entry.Rule == "reduced-motion-not-configured"));
    }

    [TestMethod]
    public void Review_PointsAtTheLineTheMistakeIsOn()
    {
        var code = """
            <div class="wrapper">
                <Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Spring(stiffness: 300, duration: 0.5)">
                    <div class="box" />
                </Bmotion>
            </div>
            """;

        var finding = BmotionCodeReview.Review(code).Findings.Single(entry => entry.Rule == "spring-physics-overridden-by-duration");

        Assert.AreEqual(2, finding.Line);
    }

    [TestMethod]
    public void Review_TheMostSeriousFindingsComeFirst()
    {
        // One markup, three mistakes at three severities: a self-closed Bmotion (Error), a spring
        // configured in both forms at once (Warning) and a frame-loop-only property (Suggestion).
        var code = """
            <Bmotion Animate="Bm.To(d: "M 0 0 L 100 0")" Transition="Bm.Spring(stiffness: 300, duration: 0.5)" />
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
            <Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Spring(stiffness: 300, duration: 0.5)">
                <div class="box" />
            </Bmotion>
            """);

        Assert.IsTrue(warning.Findings.All(finding => finding.Severity != "Error"));
        Assert.IsFalse(warning.Passed);

        // A Suggestion alone does not.
        var suggestion = BmotionCodeReview.Review("""
            <Bmotion Initial='Bm.To(d: "M 0 0 L 100 0")' Animate='Bm.To(d: "M 0 0 L 100 40")'>
                <path class="wave" />
            </Bmotion>
            """);

        Assert.AreNotEqual(0, suggestion.Findings.Length, "The sample no longer provokes the Suggestion it is here for.");
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
            <Bmotion Initial='Bm.To(d: "M 0 0 L 100 0")' Animate='Bm.To(d: "M 0 0 L 100 40")'>
                <path class="wave" />
            </Bmotion>
            """;

        var findings = BmotionCodeReview.Review(code)
            .Findings.Where(finding => finding.Rule == "frame-loop-only-properties")
            .ToArray();

        Assert.AreEqual(1, findings.Length, "The same property was reported for both ends of the animation.");
    }

    /// <summary>
    /// Everything the old rule warned about that the browser now plays on every runtime - lengths,
    /// colours, filters, shadows, layout properties - must stay quiet, or the review teaches an agent
    /// to rewrite working animations into worse ones.
    /// </summary>
    [TestMethod]
    public void Review_PropertiesTheBrowserPlays_AreNoLongerReportedAsFrameLoopOnly()
    {
        var code = """
            <Bmotion Initial='Bm.To(width: "0px", top: "0px", filter: "blur(0px)", boxShadow: "0 0 0 rgba(0,0,0,0)")'
                     Animate='Bm.To(width: "320px", top: "40px", filter: "blur(4px)", boxShadow: "0 8px 30px rgba(0,0,0,.2)")'>
                <div class="card" />
            </Bmotion>
            """;

        var reported = BmotionCodeReview.Review(code)
            .Findings.Where(finding => finding.Rule.StartsWith("frame-loop-only", StringComparison.Ordinal))
            .ToArray();

        Assert.AreEqual(0, reported.Length, string.Join(" | ", reported.Select(finding => finding.Message)));
    }

    /// <summary>
    /// A transition's OnUpdate needs a C# call every frame, which only the frame loop gives it. The
    /// component's own OnUpdate parameter is different - the animations it reports on still play in
    /// the browser - so it must not be mistaken for one.
    /// </summary>
    [TestMethod]
    public void Review_ATransitionOnUpdate_IsReported_ButTheComponentsOwnOnUpdateIsNot()
    {
        var transition = """
            <Bmotion Animate="Bm.To(x: 100)"
                     Transition="new BmTween { Duration = 0.4, OnUpdate = v => { _progress = v; } }">
                <div class="box" />
            </Bmotion>
            """;

        var component = """
            <Bmotion Animate="Bm.To(x: 100)" OnUpdate="OnFrame">
                <div class="box" />
            </Bmotion>
            """;

        var finding = BmotionCodeReview.Review(transition).Findings.SingleOrDefault(entry => entry.Rule == "frame-loop-only-transition");

        Assert.IsNotNull(finding, "A transition OnUpdate was not reported.");
        Assert.AreEqual(2, finding.Line);
        StringAssert.Contains(finding.Message, "OnUpdate");

        Assert.IsFalse(BmotionCodeReview.Review(component).Findings.Any(entry => entry.Rule == "frame-loop-only-transition"),
                       "The component's OnUpdate parameter was reported as a transition callback.");
    }

    /// <summary>
    /// A repeat with a count is recorded unrolled, and the engine records at most ten seconds - past
    /// that only the frame loop plays it. A timeline has no such limit, so a long plain tween, or a
    /// loop with no delay between iterations, is fine however long it runs.
    /// </summary>
    [TestMethod]
    public void Review_ARepeatThatUnrollsPastTenSeconds_IsReported_ButALongTimelineIsNot()
    {
        var tooLong = """
            <Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Tween(4, repeat: BmRepeat.Mirror(2))">
                <div class="box" />
            </Bmotion>
            """;

        var finding = BmotionCodeReview.Review(tooLong).Findings.SingleOrDefault(entry => entry.Rule == "frame-loop-only-transition");

        Assert.IsNotNull(finding, "A 12-second recording was not reported.");
        StringAssert.Contains(finding.Message, "12 seconds");

        foreach (var fine in new[]
        {
            """<Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Tween(12)"><div /></Bmotion>""",
            """<Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Tween(4, repeat: BmRepeat.Loop(5))"><div /></Bmotion>""",
            """<Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Tween(1, repeat: BmRepeat.Reverse(2))"><div /></Bmotion>""",
            """<Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Tween(4, repeat: BmRepeat.Mirror())"><div /></Bmotion>""",
        })
        {
            Assert.IsFalse(BmotionCodeReview.Review(fine).Findings.Any(entry => entry.Rule == "frame-loop-only-transition"),
                           $"Reported as frame-loop-only:\n{fine}");
        }
    }

    [TestMethod]
    [DataRow("BmRepeat.Reverse()")]
    [DataRow("BmRepeat.Reverse(delay: 0.5)")]
    [DataRow("BmRepeat.Reverse(-1)")]
    [DataRow("BmRepeat.Reverse(count: -1, delay: 0.2)")]
    public void Review_AnEndlessReverse_IsReportedHoweverItIsWritten(string repeat)
    {
        var code = $"""<Bmotion Animate="Bm.To(x: 100)" Transition="Bm.Tween(1, repeat: {repeat})"><div /></Bmotion>""";

        Assert.IsTrue(BmotionCodeReview.Review(code).Findings.Any(entry => entry.Rule == "frame-loop-only-transition"),
                      $"'{repeat}' was not recognised as repeating forever.");
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
