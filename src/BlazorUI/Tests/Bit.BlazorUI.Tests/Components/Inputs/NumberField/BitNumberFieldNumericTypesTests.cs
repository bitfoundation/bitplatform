using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.NumberField;

/// <summary>
/// Covers the numeric edge cases of BitNumberField across all supported value types: spinning
/// small integer types (whose arithmetic gets promoted to int), clamping at the type bounds
/// instead of wrapping around, and the precision/rounding semantics.
/// </summary>
[TestClass]
public class BitNumberFieldNumericTypesTests : BunitTestContext
{
    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private void Spin<TValue>(IRenderedComponent<BitNumberField<TValue>> component, bool up)
    {
        component.Find("input").KeyDown(new KeyboardEventArgs { Key = up ? "ArrowUp" : "ArrowDown" });
    }

    [TestMethod]
    public void BitNumberFieldShouldIncrementAndDecrementByteValues()
    {
        var component = RenderComponent<BitNumberField<byte>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (byte)5);
        });

        Spin(component, up: true);
        Assert.AreEqual((byte)6, component.Instance.Value);

        Spin(component, up: false);
        Assert.AreEqual((byte)5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldIncrementAndDecrementSByteValues()
    {
        var component = RenderComponent<BitNumberField<sbyte>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (sbyte)-5);
        });

        Spin(component, up: true);
        Assert.AreEqual((sbyte)-4, component.Instance.Value);

        Spin(component, up: false);
        Assert.AreEqual((sbyte)-5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldIncrementAndDecrementShortValues()
    {
        var component = RenderComponent<BitNumberField<short>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (short)100);
            parameters.Add(p => p.Step, "10");
        });

        Spin(component, up: true);
        Assert.AreEqual((short)110, component.Instance.Value);

        Spin(component, up: false);
        Assert.AreEqual((short)100, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldIncrementAndDecrementUShortValues()
    {
        var component = RenderComponent<BitNumberField<ushort>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, (ushort)1);
        });

        Spin(component, up: true);
        Assert.AreEqual((ushort)2, component.Instance.Value);

        Spin(component, up: false);
        Spin(component, up: false);
        Assert.AreEqual((ushort)0, component.Instance.Value);

        // Decrementing below zero clamps at the unsigned type's minimum instead of overflowing.
        Spin(component, up: false);
        Assert.AreEqual((ushort)0, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldIncrementAndDecrementUIntValuesBeyondIntRange()
    {
        var component = RenderComponent<BitNumberField<uint>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 3_000_000_000u); // above int.MaxValue
        });

        Spin(component, up: true);
        Assert.AreEqual(3_000_000_001u, component.Instance.Value);

        Spin(component, up: false);
        Assert.AreEqual(3_000_000_000u, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldIncrementAndDecrementULongValuesBeyondLongRange()
    {
        var component = RenderComponent<BitNumberField<ulong>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 10_000_000_000_000_000_000ul); // above long.MaxValue
        });

        Spin(component, up: true);
        Assert.AreEqual(10_000_000_000_000_000_001ul, component.Instance.Value);

        Spin(component, up: false);
        Assert.AreEqual(10_000_000_000_000_000_000ul, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldIncrementAndDecrementDecimalValues()
    {
        var component = RenderComponent<BitNumberField<decimal>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1.5m);
            parameters.Add(p => p.Step, "0.5");
        });

        Spin(component, up: true);
        Assert.AreEqual(2.0m, component.Instance.Value);

        Spin(component, up: false);
        Assert.AreEqual(1.5m, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldIncrementAndDecrementFloatValues()
    {
        var component = RenderComponent<BitNumberField<float>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1.5f);
            parameters.Add(p => p.Step, "0.5");
        });

        Spin(component, up: true);
        Assert.AreEqual(2.0f, component.Instance.Value);

        Spin(component, up: false);
        Assert.AreEqual(1.5f, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldClampIncrementAtTypeMaxInsteadOfWrapping()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, int.MaxValue);
        });

        Spin(component, up: true);
        Assert.AreEqual(int.MaxValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldClampDecrementAtTypeMinInsteadOfWrapping()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, int.MinValue);
        });

        Spin(component, up: false);
        Assert.AreEqual(int.MinValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldClampByteIncrementAtTypeMax()
    {
        var component = RenderComponent<BitNumberField<byte>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, byte.MaxValue);
        });

        Spin(component, up: true);
        Assert.AreEqual(byte.MaxValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldClampLongDecrementAtTypeMin()
    {
        var component = RenderComponent<BitNumberField<long>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, long.MinValue);
        });

        Spin(component, up: false);
        Assert.AreEqual(long.MinValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotRoundTypedDecimalsWithoutPrecisionOrStep()
    {
        // A plain double field must keep the fractional digits the user typed (no default rounding).
        var component = RenderComponent<BitNumberField<double>>();

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "1.23" });

        Assert.AreEqual(1.23, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldDerivePrecisionFromFractionalStep()
    {
        // Step "0.1" implies a precision of 1 decimal place, even without the digit normalization.
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.Step, "0.1");
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "1.23" });

        Assert.AreEqual(1.2, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotRoundWithIntegralStep()
    {
        // An integral step must not imply any rounding of the typed value (and in particular a step
        // like "10" must not crash the negative-precision rounding path).
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.Step, "10");
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "23.7" });

        Assert.AreEqual(23.7, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotRoundWithIntegralStepWhenNormalizationEnabled()
    {
        // The normalization code path recomputes the precision from the Step; an integral step must
        // not produce a negative precision that crashes Math.Round.
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.NormalizeDigits, true);
            parameters.Add(p => p.Step, "10");
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "23" });

        Assert.AreEqual(23d, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldRoundToPowerOfTenWithNegativePrecision()
    {
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.Precision, -1);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "27" });

        Assert.AreEqual(30d, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldRoundDecimalTypeToExplicitPrecision()
    {
        var component = RenderComponent<BitNumberField<decimal>>(parameters =>
        {
            parameters.Add(p => p.Precision, 2);
        });

        var input = component.Find("input");
        input.Change(new ChangeEventArgs { Value = "1.239" });

        Assert.AreEqual(1.24m, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldFallBackToStepOneWhenStepIsUnparsable()
    {
        // An unparsable Step must fall back to 1 (and not crash on non-int value types).
        var component = RenderComponent<BitNumberField<double>>(parameters =>
        {
            parameters.Add(p => p.Step, "abc");
            parameters.Add(p => p.DefaultValue, 5d);
        });

        Spin(component, up: true);
        Assert.AreEqual(6d, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldHaveSpinbuttonRole()
    {
        var component = RenderComponent<BitNumberField<int>>();

        var input = component.Find("input");
        Assert.AreEqual("spinbutton", input.GetAttribute("role"));
    }

    [TestMethod]
    public void BitNumberFieldShouldNotRenderAriaLabelledByWithoutLabel()
    {
        var component = RenderComponent<BitNumberField<int>>();
        Assert.IsFalse(component.Find("input").HasAttribute("aria-labelledby"));

        var labeled = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Label, "The label");
        });
        var labelId = labeled.Find("label").GetAttribute("id");
        Assert.AreEqual(labelId, labeled.Find("input").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitNumberFieldShouldThrowForUnsupportedValueType()
    {
        Assert.Throws<InvalidOperationException>(() => RenderComponent<BitNumberField<System.DateTime>>());
    }

    [TestMethod]
    public void BitNumberFieldShouldCommitPendingTypedTextBeforeSteppingWhenAvailable()
    {
        // The live input text is read through JS right before a keyboard step, so stepping applies to
        // what the user currently sees instead of the stale committed value.
        Context.JSInterop.Mode = JSRuntimeMode.Strict;
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult("50");
        Context.JSInterop.SetupVoid("BitBlazorUI.Utils.selectText", _ => true);

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 5);
        });

        component.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.AreEqual(51, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldFormatValueWithInvariantCulture()
    {
        // The displayed value must parse back with the invariant culture regardless of the current
        // thread culture (a culture-sensitive ToString would render "1,5" in e.g. German).
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            var component = RenderComponent<BitNumberField<double>>(parameters =>
            {
                parameters.Add(p => p.DefaultValue, 1.5);
            });

            Assert.AreEqual("1.5", component.Find("input").GetAttribute("value"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void BitNumberFieldShouldParseCultureFormattedValueWhenNumberFormatIsSet()
    {
        // With NumberFormat, the display uses the current culture (German: group '.', decimal ','),
        // so committing that text back must reverse the same culture's separators.
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            var component = RenderComponent<BitNumberField<double>>(parameters =>
            {
                parameters.Add(p => p.NumberFormat, "N2");
            });

            var input = component.Find("input");
            input.Change(new ChangeEventArgs { Value = "1.234,56" });

            Assert.AreEqual(1234.56, component.Instance.Value);
            Assert.AreEqual("1.234,56", component.Find("input").GetAttribute("value"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void BitNumberFieldShouldUseProvidedClearButtonAriaLabel()
    {
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.ClearButtonAriaLabel, "پاک کردن");
            parameters.Add(p => p.DefaultValue, 10);
        });

        Assert.AreEqual("پاک کردن", component.Find(".bit-nfl-cbt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNumberFieldShouldCommitValueOnInputWhenImmediate()
    {
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
        });

        component.Find("input").Input(new ChangeEventArgs { Value = "42" });

        Assert.AreEqual(42, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotSelectTextOnFocusWhenNoSelectOnFocus()
    {
        // Strict JS interop: any selectText invocation would throw as an unplanned call.
        Context.JSInterop.Mode = JSRuntimeMode.Strict;

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.NoSelectOnFocus, true);
            parameters.Add(p => p.DefaultValue, 5);
        });

        component.Find("input").Focus();
    }

    [TestMethod]
    public void BitNumberFieldShouldChangeValueOnShiftMouseWheel()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 5);
        });

        var input = component.Find("input");

        input.Wheel(new WheelEventArgs { ShiftKey = true, DeltaY = -1 });
        Assert.AreEqual(6, component.Instance.Value);

        input.Wheel(new WheelEventArgs { ShiftKey = true, DeltaY = +1 });
        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotChangeValueOnMouseWheelWithoutShift()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 5);
        });

        component.Find("input").Wheel(new WheelEventArgs { ShiftKey = false, DeltaY = -1 });

        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldReverseMouseWheelDirectionWhenInverted()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 5);
            parameters.Add(p => p.InvertMouseWheel, true);
        });

        var input = component.Find("input");

        input.Wheel(new WheelEventArgs { ShiftKey = true, DeltaY = -1 });
        Assert.AreEqual(4, component.Instance.Value);

        input.Wheel(new WheelEventArgs { ShiftKey = true, DeltaY = +1 });
        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotChangeValueOnMouseWheelWhenReadOnly()
    {
        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 5);
            parameters.Add(p => p.ReadOnly, true);
        });

        component.Find("input").Wheel(new WheelEventArgs { ShiftKey = true, DeltaY = -1 });

        Assert.AreEqual(5, component.Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldClearButtonShouldBeAccessible()
    {
        var component = RenderComponent<BitNumberField<int?>>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, 10);
        });

        var clearButton = component.Find(".bit-nfl-cbt");

        // The clear button must not be hidden from assistive technologies while remaining clickable,
        // and it stays out of the tab order so the input remains the single tab stop.
        Assert.IsFalse(clearButton.HasAttribute("aria-hidden"));
        Assert.IsTrue(clearButton.HasAttribute("aria-label"));
        Assert.AreEqual("-1", clearButton.GetAttribute("tabindex"));

        clearButton.Click();
        Assert.IsNull(component.Instance.Value);
    }
}
