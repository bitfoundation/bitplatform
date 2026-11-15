using System;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TextField;

/// <summary>
/// The Mask of BitTextField: '#' a digit, 'A' a letter, '*' either, a backslash for a literal; the text laid out as it is
/// typed (and written into the input by a script that keeps the caret), the value formatted or raw (MaskRawValue), and a
/// value from outside shown laid out.
/// </summary>
[TestClass]
public class BitTextFieldMaskTests : BunitTestContext
{
    private const string SetMaskedValue = "BitBlazorUI.TextField.setMaskedValue";

    private IRenderedComponent<BitTextField> RenderMasked(string mask, bool raw = false, bool immediate = true, string? value = null)
        => RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Mask, mask);
            parameters.Add(p => p.MaskRawValue, raw);
            parameters.Add(p => p.Immediate, immediate);
            if (value is not null) parameters.Add(p => p.Value, value);
        });

    [TestMethod]
    [DataRow("##/##/####", "1", "1")]
    [DataRow("##/##/####", "123", "12/3")]
    [DataRow("##/##/####", "12312025", "12/31/2025")]
    [DataRow("##/##/####", "12ab31", "12/31", DisplayName = "What a slot does not take is dropped")]
    [DataRow("##/##/####", "1231202599", "12/31/2025", DisplayName = "What is typed beyond the last slot is dropped")]
    [DataRow("AA-####", "ca1234", "ca-1234")]
    [DataRow("AA-####", "1ca", "ca", DisplayName = "A digit where a letter goes is dropped")]
    [DataRow("****-****", "x7k2!9qpl", "x7k2-9qpl")]
    [DataRow(@"\A\B-###", "5", "AB-5", DisplayName = "An escaped token is a literal")]
    [DataRow(@"\A\B-###", "AB-123", "AB-123")]
    public void BitTextFieldShouldLayTheTextOutOverTheMaskAsItIsTyped(string mask, string typed, string expected)
    {
        var component = RenderMasked(mask);

        component.Find(".bit-tfl-inp").Input(typed);

        Assert.AreEqual(expected, component.Instance.Value);
    }

    [TestMethod]
    public void BitTextFieldShouldWriteTheLayoutIntoTheInputThroughItsScript()
    {
        var component = RenderMasked("##/##/####");

        component.Find(".bit-tfl-inp").Input("1231");

        var write = Context.JSInterop.Invocations[SetMaskedValue].Single();
        CollectionAssert.AreEqual(new object?[] { "12/31", "1231", "##/##/####" }, write.Arguments.Skip(1).ToArray(),
            "the layout, the text it is only written over, and the mask the caret is put back by");

        // A text that is already laid out is not written again.
        component.Find(".bit-tfl-inp").Input("12/31/2");
        Assert.HasCount(1, Context.JSInterop.Invocations[SetMaskedValue]);
    }

    [TestMethod]
    public void BitTextFieldShouldCommitTheRawValueWhenAskedTo()
    {
        var component = RenderMasked("##/##/####", raw: true);

        component.Find(".bit-tfl-inp").Input("12/31/2025");

        Assert.AreEqual("12312025", component.Instance.Value);
        Assert.AreEqual("12/31/2025", component.Find(".bit-tfl-inp").GetAttribute("value"), "the field still shows the layout");
    }

    [TestMethod]
    [DataRow(false, "12312025", DisplayName = "Raw text from outside")]
    [DataRow(false, "12/31/2025", DisplayName = "Formatted text from outside")]
    [DataRow(true, "12312025", DisplayName = "A raw value")]
    public void BitTextFieldShouldShowAValueFromOutsideLaidOut(bool raw, string value)
    {
        var component = RenderMasked("##/##/####", raw: raw, value: value);

        Assert.AreEqual("12/31/2025", component.Find(".bit-tfl-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTextFieldShouldLayOutTheTextItCommitsOnChange()
    {
        var component = RenderMasked("(###) ###-####", immediate: false);

        component.Find(".bit-tfl-inp").Change("4155550123");

        Assert.AreEqual("(415) 555-0123", component.Instance.Value);
    }

    [TestMethod]
    public void BitTextFieldShouldIgnoreTheMaskWhenMultiline()
    {
        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Mask, "##/##/####");
            parameters.Add(p => p.Multiline, true);
            parameters.Add(p => p.Immediate, true);
        });

        component.Find(".bit-tfl-inp").Input("12312025");

        Assert.AreEqual("12312025", component.Instance.Value);
        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == SetMaskedValue));
    }

    [TestMethod]
    public void BitTextFieldShouldLeaveAnEmptyMaskedFieldEmpty()
    {
        var component = RenderMasked("##/##/####");
        component.Find(".bit-tfl-inp").Input("1231");

        component.Find(".bit-tfl-inp").Input("");

        Assert.IsTrue(string.IsNullOrEmpty(component.Instance.Value));
    }
}
