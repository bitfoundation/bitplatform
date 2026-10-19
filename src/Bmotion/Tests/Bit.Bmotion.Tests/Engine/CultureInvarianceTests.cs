using System.Globalization;
using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Every number Bit.Bmotion writes into CSS must be culture-invariant: under <c>de-DE</c> a decimal
/// comma, and under <c>fa-IR</c> a Persian decimal separator and a non-ASCII minus sign, would turn
/// <c>translateX(-1.5px)</c> into invalid CSS the browser silently drops. Each test produces the
/// same output under the invariant culture and under the hostile ones and requires them to match.
/// </summary>
[TestClass]
public class CultureInvarianceTests
{
    private static readonly string[] HostileCultures = ["de-DE", "fa-IR"];

    // Runs the producer under the given culture (UI culture too) and restores the thread's culture.
    private static async Task<string> Under(string culture, Func<Task<string>> produce)
    {
        var (previous, previousUi) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            return await produce();
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (previous, previousUi);
        }
    }

    private static async Task AssertCultureInvariant(Func<Task<string>> produce)
    {
        var invariant = await Under("", produce);
        foreach (var culture in HostileCultures)
            Assert.AreEqual(invariant, await Under(culture, produce), $"CSS output changed under {culture}");
    }

    // Values chosen to exercise a decimal separator and a minus sign in every kind of CSS number.
    private static BmProps Fractional => Bm.To(
        x: -1.5, y: 2.25, rotate: -12.75, scale: 0.5,
        opacity: 0.125,
        width: 10.5, // a bare number becomes a px dimension
        cssVars: new() { ["--depth"] = "3" });

    [TestMethod]
    public void TheHostileCultures_ReallyFormatNumbersDifferently()
    {
        // Guard against a vacuous suite: if these cultures formatted like invariant, nothing below would prove anything.
        Assert.AreEqual("1,5", 1.5.ToString(CultureInfo.GetCultureInfo("de-DE")));
        Assert.AreNotEqual("-1.5", (-1.5).ToString(CultureInfo.GetCultureInfo("fa-IR")));
    }

    [TestMethod]
    public Task InitialInlineStyle_IsCultureInvariant()
        => AssertCultureInvariant(() =>
        {
            using var ctx = new BmotionTestContext();
            var cut = ctx.Render<Bmotion>(ps => ps
                .Add(p => p.Id, "box")
                .Add(p => p.Initial, Fractional)
                .Add(p => p.ChildContent, (RenderFragment)(b =>
                {
                    b.OpenElement(0, "div");
                    b.CloseElement();
                })));
            var style = cut.Find("div").GetAttribute("style")!;
            StringAssert.Contains(style, "1.5");
            return Task.FromResult(style);
        });

    [TestMethod]
    public Task FrameLoopOutput_IsCultureInvariant()
        => AssertCultureInvariant(async () =>
        {
            await using var engine = new BmotionAnimationEngine(new FakeBmotionInterop());
            engine.RegisterElement("el", null);
            // width is not compositor-eligible, so the whole target stays on the frame loop.
            await engine.AnimateToAsync("el", Fractional.ToJsDictionary(), Bm.Tween(1, ease: BmEase.Linear).ToConfig());
            engine.ComputeFrame(0);
            var frame = engine.ComputeFrame(333)!["el"];
            return string.Join(";", frame.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}:{kv.Value}"));
        });

    [TestMethod]
    public Task CompositorKeyframes_AreCultureInvariant()
        => AssertCultureInvariant(async () =>
        {
            var interop = new FakeBmotionInterop();
            await using var engine = new BmotionAnimationEngine(interop);
            engine.RegisterElement("el", new Dictionary<string, object?> { ["x"] = 0.75, ["opacity"] = 0.25 });
            await engine.AnimateToAsync("el",
                new Dictionary<string, object?> { ["x"] = -1.5, ["rotate"] = 7.5, ["opacity"] = 0.125 },
                Bm.Tween(0.5).ToConfig());
            var keyframes = (Dictionary<string, object>[])interop.ArgsOf("playWaapiAnimation").Single()[2]!;
            return string.Join("|", keyframes.Select(k => $"{k["transform"]};{k["opacity"]}"));
        });

    [TestMethod]
    public Task BareNumberDimensions_AreCultureInvariant()
        => AssertCultureInvariant(() =>
        {
            BmStringKeyframes width = 12.5;
            Assert.AreEqual("12.5px", width.First);
            return Task.FromResult(width.First);
        });
}
