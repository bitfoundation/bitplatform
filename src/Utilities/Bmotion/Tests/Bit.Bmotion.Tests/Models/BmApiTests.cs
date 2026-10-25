namespace Bit.Bmotion.Tests.Models;

/// <summary>Tests for the public Bm facade, keyframe unions, transitions and timeline helpers.</summary>
[TestClass]
public class BmApiTests
{
    // ── BmKeyframes ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Keyframes_ImplicitFromDouble_IsSingle()
    {
        BmKeyframes k = 1.5;
        Assert.IsTrue(k.IsSingle);
        Assert.AreEqual(1.5, k.First);
        Assert.AreEqual(1.5, (double)k.ToEngineValue());
    }

    [TestMethod]
    public void Keyframes_CollectionExpression_ProducesArrayEngineValue()
    {
        BmKeyframes k = [1, 1.4, 0.8, 1];
        Assert.AreEqual(4, k.Count);
        var engine = (double[])k.ToEngineValue();
        CollectionAssert.AreEqual(new[] { 1.0, 1.4, 0.8, 1.0 }, engine);
    }

    [TestMethod]
    public void Keyframes_ValueEquality_IncludesWildcards()
    {
        BmKeyframes a = [Bm.Current, 100];
        BmKeyframes b = [Bm.Current, 100];
        BmKeyframes c = [0, 100];
        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
    }

    [TestMethod]
    public void Keyframes_Empty_Throws()
        => Assert.ThrowsExactly<ArgumentException>(() => _ = new BmKeyframes());

    [TestMethod]
    public void StringKeyframes_NumberDefaultsToPx()
    {
        BmStringKeyframes k = 200;
        Assert.AreEqual("200px", k.First);
    }

    // ── Bm.To / props ──────────────────────────────────────────────────────────

    [TestMethod]
    public void BmTo_MapsNamedArgumentsToEngineKeys()
    {
        var props = Bm.To(x: 100, opacity: 0.5, backgroundColor: "#ff0000", scale: [1, 2]);
        var d = props.ToJsDictionary();
        Assert.AreEqual(100.0, (double)d["x"]!);
        Assert.AreEqual(0.5, (double)d["opacity"]!);
        Assert.AreEqual("#ff0000", d["backgroundColor"]);
        CollectionAssert.AreEqual(new[] { 1.0, 2.0 }, (double[])d["scale"]!);
    }

    [TestMethod]
    public void BmTo_ValueEquals_IsStructural()
    {
        var a = Bm.To(x: [0, 100], opacity: 1, transition: Bm.Spring(stiffness: 200));
        var b = Bm.To(x: [0, 100], opacity: 1, transition: Bm.Spring(stiffness: 200));
        Assert.IsTrue(a.ValueEquals(b));

        var c = Bm.To(x: [0, 100], opacity: 1, transition: Bm.Spring(stiffness: 201));
        Assert.IsFalse(a.ValueEquals(c));
    }

    [TestMethod]
    public void BmTo_Origin_EmitsTransformOrigin()
    {
        var d = Bm.To(originX: 0, originY: 0.5).ToJsDictionary();
        Assert.AreEqual("0% 50%", d["transformOrigin"]);
    }

    // ── Transitions ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tween_LowersToTweenConfig()
    {
        var config = Bm.Tween(0.4, BmEase.InOut, delay: 0.1, times: [0, 0.5, 1]).ToConfig();
        Assert.AreEqual(BmotionTransitionType.Tween, config.Type);
        Assert.AreEqual(0.4, config.Duration);
        Assert.AreEqual(BmEase.InOut, config.Ease);
        Assert.AreEqual(0.1, config.Delay);
        CollectionAssert.AreEqual(new[] { 0.0, 0.5, 1.0 }, config.Times);
    }

    [TestMethod]
    public void Spring_PhysicsParameters_LowerDirectly()
    {
        var config = Bm.Spring(stiffness: 250, damping: 22, mass: 2).ToConfig();
        Assert.AreEqual(BmotionTransitionType.Spring, config.Type);
        Assert.AreEqual(250, config.Stiffness);
        Assert.AreEqual(22, config.Damping);
        Assert.AreEqual(2, config.Mass);
        Assert.IsNull(config.Bounce); // physics form doesn't opt into duration-based derivation
    }

    [TestMethod]
    public void Spring_DurationForm_OptsIntoBounceDerivation()
    {
        var config = Bm.Spring(duration: 0.6).ToConfig();
        Assert.AreEqual(0.25, config.Bounce);         // default bounce
        Assert.AreEqual(0.6, config.VisualDuration);
    }

    [TestMethod]
    public void Inertia_LowersToInertiaConfig()
    {
        var config = Bm.Inertia(velocity: 500, timeConstant: 600, min: -10, max: 10).ToConfig();
        Assert.AreEqual(BmotionTransitionType.Inertia, config.Type);
        Assert.AreEqual(500, config.InertiaVelocity);
        Assert.AreEqual(600, config.TimeConstant);
        Assert.AreEqual(-10, config.InertiaMin);
        Assert.AreEqual(10, config.InertiaMax);
    }

    [TestMethod]
    public void Repeat_Forever_LowersToInfiniteConfig()
    {
        var config = Bm.Tween(repeat: BmRepeat.Forever).ToConfig();
        Assert.IsTrue(config.IsInfiniteRepeat);

        var mirrored = Bm.Tween(repeat: BmRepeat.Mirror(delay: 0.5)).ToConfig();
        Assert.IsTrue(mirrored.IsInfiniteRepeat);
        Assert.AreEqual(BmRepeatType.Mirror, mirrored.RepeatType);
        Assert.AreEqual(0.5, mirrored.RepeatDelay);
    }

    [TestMethod]
    public void Repeat_ImplicitInt_LowersToFiniteCount()
    {
        var config = Bm.Tween(repeat: 3).ToConfig();
        Assert.IsFalse(config.IsInfiniteRepeat);
        Assert.AreEqual(3, config.Repeat);
    }

    [TestMethod]
    public void PerPropertyOverrides_LowerRecursively()
    {
        var t = Bm.Spring();
        t.Properties = new() { ["opacity"] = Bm.Tween(0.1) };
        var config = t.ToConfig();
        Assert.IsNotNull(config.Properties);
        Assert.AreEqual(BmotionTransitionType.Tween, config.Properties!["opacity"].Type);
        Assert.AreEqual(0.1, config.Properties["opacity"].Duration);
    }

    [TestMethod]
    public void Transition_AreEquivalent_ComparesStructurally()
    {
        Assert.IsTrue(BmTransition.AreEquivalent(Bm.Tween(0.4, BmEase.In), Bm.Tween(0.4, BmEase.In)));
        Assert.IsFalse(BmTransition.AreEquivalent(Bm.Tween(0.4), Bm.Spring()));
        Assert.IsFalse(BmTransition.AreEquivalent(Bm.Tween(0.4), Bm.Tween(0.5)));
    }

    // ── Variants ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Variants_IndexerInitializer_AndCaseInsensitiveLookup()
    {
        var variants = new BmVariants
        {
            ["hidden"] = Bm.To(opacity: 0),
            ["visible"] = Bm.To(opacity: 1),
        };
        Assert.IsTrue(variants.Contains("HIDDEN"));
        Assert.AreEqual(1.0, variants.Get("Visible")!.Opacity!.First);
    }

    [TestMethod]
    public void Variants_DynamicEntry_ReceivesCustomData()
    {
        var variants = new BmVariants();
        variants.Add("visible", custom => Bm.To(x: 10 * (int)custom!));
        Assert.AreEqual(30.0, variants.Get("visible", 3)!.X!.First);
    }

    // ── Stagger ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Stagger_FromFirst_LastAndCenter()
    {
        var first = Bm.Stagger(0.1);
        Assert.AreEqual(0.0, first.DelayFor(0, 5), 1e-9);
        Assert.AreEqual(0.4, first.DelayFor(4, 5), 1e-9);

        var last = Bm.Stagger(0.1, BmStaggerFrom.Last);
        Assert.AreEqual(0.4, last.DelayFor(0, 5), 1e-9);
        Assert.AreEqual(0.0, last.DelayFor(4, 5), 1e-9);

        var center = Bm.Stagger(0.1, BmStaggerFrom.Center, startDelay: 0.2);
        Assert.AreEqual(0.2, center.DelayFor(2, 5), 1e-9);
        Assert.AreEqual(0.4, center.DelayFor(0, 5), 1e-9);
    }

    // ── Sequence ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Sequence_DefaultPositioning_ChainsSegments()
    {
        var seq = new BmSequence()
            .Add("#a", Bm.To(x: 1), Bm.Tween(0.5))
            .Add("#a", Bm.To(y: 1), Bm.Tween(0.3));
        Assert.AreEqual(0.0, seq.Segments[0].Start, 1e-9);
        Assert.AreEqual(0.5, seq.Segments[1].Start, 1e-9);
    }

    [TestMethod]
    public void Sequence_AtOffsets_RelativeStartAndLabels()
    {
        var seq = new BmSequence()
            .Add("#a", Bm.To(x: 1), Bm.Tween(0.5))
            .Add("#b", Bm.To(x: 1), Bm.Tween(0.2), at: "-0.1")   // 0.5 - 0.1
            .Add("#c", Bm.To(x: 1), Bm.Tween(0.2), at: "<")       // with #b's start
            .Label("mark")
            .Add("#d", Bm.To(x: 1), at: "mark")
            .Add("#e", Bm.To(x: 1), at: "2.5");
        Assert.AreEqual(0.4, seq.Segments[1].Start, 1e-9);
        Assert.AreEqual(0.4, seq.Segments[2].Start, 1e-9);
        Assert.AreEqual(0.6, seq.Segments[3].Start, 1e-9);       // cursor after #b/#c = 0.6
        Assert.AreEqual(2.5, seq.Segments[4].Start, 1e-9);
    }

    [TestMethod]
    public void Sequence_UnknownLabel_Throws()
    {
        var seq = new BmSequence();
        Assert.ThrowsExactly<ArgumentException>(() => seq.Add("#a", Bm.To(x: 1), at: "nope"));
    }

    // ── BmValue velocity ──────────────────────────────────────────────────

    [TestMethod]
    public void Value_TracksVelocity_AndJumpResetsIt()
    {
        var value = Bm.Value(0.0);
        long nowMs = 1000;
        value.TimeSource = () => nowMs;

        value.SetSync(0);
        nowMs += 30;
        value.SetSync(30);
        Assert.AreEqual(1000, value.GetVelocity(), 1e-9); // 30 units over 30 ms

        value.Jump(0);
        Assert.AreEqual(0, value.GetVelocity());
    }

    // ── BmDrag ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Drag_ImplicitBool_AndAxisFactories()
    {
        BmDrag off = false;
        Assert.IsFalse(off.Enabled);

        BmDrag on = true;
        Assert.IsTrue(on.Enabled);
        Assert.AreEqual(BmDragAxis.Both, on.Axis);

        Assert.AreEqual(BmDragAxis.X, BmDrag.X.Axis);
        Assert.IsTrue(BmDrag.Y.Enabled);
    }

    // ── BmDragConstraints (element-bounds mode) ────────────────────────────────

    [TestMethod]
    public void DragConstraints_Parent_SetsElementBoundsMode()
    {
        var constraints = BmDragConstraints.Parent();

        Assert.IsTrue(constraints.FromParent);
        Assert.IsNull(constraints.Selector);

        var js = (Dictionary<string, object?>)constraints.ToJsObject();
        Assert.IsTrue((bool)js["parent"]!);
        Assert.IsFalse(js.ContainsKey("left"));
    }

    [TestMethod]
    public void DragConstraints_Within_CarriesSelector()
    {
        var constraints = BmDragConstraints.Within(".drop-zone");

        Assert.AreEqual(".drop-zone", constraints.Selector);
        Assert.IsFalse(constraints.FromParent);

        var js = (Dictionary<string, object?>)constraints.ToJsObject();
        Assert.AreEqual(".drop-zone", js["selector"]);
    }

    [TestMethod]
    public void DragConstraints_Within_RejectsEmptySelector()
        => Assert.ThrowsExactly<ArgumentException>(() => BmDragConstraints.Within("  "));

    // ── BmDragElastic ──────────────────────────────────────────────────────────

    [TestMethod]
    public void DragElastic_ImplicitDouble_IsUniform()
    {
        BmDragElastic elastic = 0.5;

        Assert.AreEqual(0.5, elastic.Left);
        Assert.AreEqual(0.5, elastic.Right);
        Assert.AreEqual(0.5, elastic.Top);
        Assert.AreEqual(0.5, elastic.Bottom);
    }

    [TestMethod]
    public void DragElastic_Edges_UnspecifiedEdgesAreRigid()
    {
        var elastic = BmDragElastic.Edges(right: 0.9, bottom: 0.9);

        Assert.AreEqual(0.0, elastic.Left);
        Assert.AreEqual(0.9, elastic.Right);
        Assert.AreEqual(0.0, elastic.Top);
        Assert.AreEqual(0.9, elastic.Bottom);
    }

    [TestMethod]
    public void DragElastic_ToJsObject_SanitisesValues()
    {
        var elastic = BmDragElastic.Edges(left: double.NaN, right: 5, top: -1, bottom: 0.5);

        var js = elastic.ToJsObject();

        Assert.AreEqual(0.35, js["left"]); // non-finite → default
        Assert.AreEqual(1.0, js["right"]); // clamped to [0, 1]
        Assert.AreEqual(0.0, js["top"]);
        Assert.AreEqual(0.5, js["bottom"]);
    }

    // ── Per-segment keyframe easing ────────────────────────────────────────────

    [TestMethod]
    public void Tween_Eases_FlowsIntoConfig()
    {
        var tween = Bm.Tween(0.5, eases: [BmEase.Linear, BmEase.CircOut]);

        var config = ((BmTransition)tween).ToConfig();

        CollectionAssert.AreEqual(new[] { BmEase.Linear, BmEase.CircOut }, config.Eases);
    }

    [TestMethod]
    public void Tween_Eases_ParticipatesInValueEquality()
    {
        var a = Bm.Tween(0.5, eases: [BmEase.Linear, BmEase.CircOut]);
        var b = Bm.Tween(0.5, eases: [BmEase.Linear, BmEase.CircOut]);
        var c = Bm.Tween(0.5, eases: [BmEase.Linear, BmEase.BackOut]);

        Assert.IsTrue(BmTransition.AreEquivalent(a, b));
        Assert.IsFalse(BmTransition.AreEquivalent(a, c));
    }

    // ── Bm.Template (useMotionTemplate) ────────────────────────────────────────

    [TestMethod]
    public void Template_RecomputesWhenAnyInputChanges()
    {
        var blur = Bm.Value(0.0);
        var bright = Bm.Value(1.0);
        var filter = Bm.Template(
            () => $"blur({blur.Value}px) brightness({bright.Value})", blur, bright);

        Assert.AreEqual("blur(0px) brightness(1)", filter.Value);

        blur.SetSync(8);
        Assert.AreEqual("blur(8px) brightness(1)", filter.Value);

        bright.SetSync(1.5);
        Assert.AreEqual("blur(8px) brightness(1.5)", filter.Value);
    }

    [TestMethod]
    public void Template_DisposeDetachesFromInputs()
    {
        var blur = Bm.Value(0.0);
        var filter = Bm.Template(() => $"blur({blur.Value}px)", blur);

        filter.Dispose();
        blur.SetSync(10);

        Assert.AreEqual("blur(0px)", filter.Value); // no longer follows the input
    }

    // ── Filter property ────────────────────────────────────────────────────────

    [TestMethod]
    public void To_Filter_FlowsIntoEngineValuesAndInitialCss()
    {
        var props = Bm.To(filter: "blur(4px)");

        Assert.AreEqual("blur(4px)", props.ToJsDictionary()["filter"]);
        StringAssert.Contains(props.ToCssStyleString(), "filter:blur(4px);");
    }
}
