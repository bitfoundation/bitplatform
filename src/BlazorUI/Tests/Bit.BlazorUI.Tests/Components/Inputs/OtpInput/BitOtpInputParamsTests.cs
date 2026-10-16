using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.OtpInput;

/// <summary>
/// Covers the BitParams cascade of the OTP input: what a BitOtpInputParams fills in - its own parameters, the ones of
/// the input base and the ones of the component base alike - what it leaves alone because the component wrote it for
/// itself, and that what the component derives from its parameters (the inputs it renders, the characters it accepts,
/// the form it submits) follows the cascaded values.
/// </summary>
[TestClass]
public class BitOtpInputParamsTests : BunitTestContext
{
    // What identifies one code rather than how every code entry looks: the callbacks, the templates and the cascade
    // itself are left out of the params object on purpose.
    private static readonly string[] _notCascaded =
    [
        nameof(BitOtpInput.CascadingParameters),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitOtpInputParams otpParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { otpParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitOtpInput>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitOtpInputParamsShouldHaveCorrectParamName()
    {
        var paramName = BitOtpInputParams.ParamName;
        var expectedName = $"{nameof(BitParams)}.{nameof(BitOtpInput)}";

        Assert.AreEqual(expectedName, paramName);
        Assert.AreEqual("BitParams.BitOtpInput", paramName);
    }

    [TestMethod]
    public void BitOtpInputParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitOtpInputParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.IsInstanceOfType<BitInputBaseParams<string?>>(@params);
        Assert.AreEqual(BitOtpInputParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitOtpInputShouldApplyCascadingParametersFromBitParams()
    {
        var com = RenderWithParams(new BitOtpInputParams
        {
            Length = 6,
            Label = "Cascaded label",
            Accent = BitColor.Success,
            Size = BitSize.Large,
            Variant = BitVariant.Fill,
            Merged = true,
            Vertical = true,
            FullWidth = true,
            Placeholder = "0",
            SingleTabStop = true,
        });

        var root = com.Find(".bit-otp");

        Assert.IsTrue(root.ClassList.Contains("bit-otp-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-otp-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-otp-fil"));
        Assert.IsTrue(root.ClassList.Contains("bit-otp-mrg"));
        Assert.IsTrue(root.ClassList.Contains("bit-otp-vrt"));
        Assert.IsTrue(root.ClassList.Contains("bit-otp-fwi"));

        Assert.AreEqual("Cascaded label", com.Find(".bit-otp-lbl").TextContent.Trim());

        var inputs = com.FindAll(".bit-otp-inp");

        Assert.AreEqual(6, inputs.Count);
        Assert.AreEqual("0", inputs[0].GetAttribute("placeholder"));
        Assert.AreEqual("-1", inputs[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitOtpInputDirectParametersShouldOverrideCascadingParameters()
    {
        var com = RenderWithParams(new BitOtpInputParams
        {
            Length = 6,
            Accent = BitColor.Success,
            Size = BitSize.Large,
            Variant = BitVariant.Fill,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitOtpInput.Length), 4);
            builder.AddAttribute(2, nameof(BitOtpInput.Accent), (BitColor?)BitColor.Error);
        });

        var root = com.Find(".bit-otp");

        // What the component wrote for itself wins over the cascade.
        Assert.AreEqual(4, com.FindAll(".bit-otp-inp").Count);
        Assert.IsTrue(root.ClassList.Contains("bit-otp-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-otp-suc"));

        // What it left unset is still filled in from the cascade, parameter by parameter.
        Assert.IsTrue(root.ClassList.Contains("bit-otp-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-otp-fil"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldApplyTheCascadedRestrictionsToTheTypedCharacters()
    {
        // The rules that decide which characters a code may hold are exactly what a BitParams is there to carry, so
        // they have to reach the filtering and not only the rendering.
        var com = RenderWithParams(new BitOtpInputParams
        {
            Length = 4,
            Uppercase = true,
            Pattern = "^[A-Z]$",
        });

        var otpInput = com.FindComponent<BitOtpInput>();

        await otpInput.FindAll(".bit-otp-inp")[0].InputAsync(new ChangeEventArgs { Value = "a" });

        Assert.AreEqual("A", otpInput.Instance.Value);

        await otpInput.FindAll(".bit-otp-inp")[1].InputAsync(new ChangeEventArgs { Value = "1" });

        Assert.AreEqual("A", otpInput.Instance.Value);
    }

    [TestMethod]
    public void BitOtpInputShouldApplyTheCascadedParametersOfTheInputBase()
    {
        // ReadOnly and Required are declared by BitInputBase rather than by the component, and they are taken out of
        // the ParameterView before the sets of BitComponentBase and the component are filled, so a cascade reaching
        // them is what this covers.
        var com = RenderWithParams(new BitOtpInputParams
        {
            Length = 4,
            ReadOnly = true,
            Required = true,
        });

        var root = com.Find(".bit-otp");

        Assert.IsTrue(root.ClassList.Contains("bit-otp-rdl"));
        Assert.IsTrue(root.ClassList.Contains("bit-otp-req"));

        var inputs = com.FindAll(".bit-otp-inp");

        Assert.AreEqual(4, inputs.Count);

        foreach (var input in inputs)
        {
            Assert.IsTrue(input.HasAttribute("readonly"));
            Assert.IsTrue(input.HasAttribute("required"));
        }
    }

    [TestMethod]
    public void BitOtpInputShouldKeepItsOwnInputBaseParametersOverTheCascadedOnes()
    {
        // The whole point of the third assigned-parameter set: a component that writes ReadOnly="false" for itself
        // has written it, so the cascade must not turn it back on.
        var com = RenderWithParams(new BitOtpInputParams
        {
            ReadOnly = true,
            Required = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitOtpInput.ReadOnly), false);
            builder.AddAttribute(2, nameof(BitOtpInput.Required), false);
        });

        var root = com.Find(".bit-otp");

        Assert.IsFalse(root.ClassList.Contains("bit-otp-rdl"));
        Assert.IsFalse(root.ClassList.Contains("bit-otp-req"));
        Assert.IsFalse(com.Find(".bit-otp-inp").HasAttribute("readonly"));
        Assert.IsFalse(com.Find(".bit-otp-inp").HasAttribute("required"));
    }

    [TestMethod]
    public void BitOtpInputShouldApplyTheCascadedBaseParameters()
    {
        var com = RenderWithParams(new BitOtpInputParams
        {
            Id = "cascaded-id",
            Class = "cascaded-class",
            Style = "margin: 1px;",
            Dir = BitDir.Rtl,
            Disabled = true,
            TabIndex = "3",
            AriaLabel = "Cascaded code",
            Visibility = BitVisibility.Hidden,
        });

        var root = com.Find(".bit-otp");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));

        // The name of the code belongs on the group of the inputs, and the TabIndex on every one of them.
        Assert.AreEqual("Cascaded code", com.Find(".bit-otp-iwr").GetAttribute("aria-label"));

        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.AreEqual("3", input.GetAttribute("tabindex"));
            Assert.IsTrue(input.HasAttribute("disabled"));
        }
    }

    [TestMethod]
    public void BitOtpInputParamsShouldApplyClassesAndStyles()
    {
        var com = RenderWithParams(new BitOtpInputParams
        {
            Length = 2,
            Label = "Code",
            Classes = new BitOtpInputClassStyles { Root = "custom-root", Label = "custom-label", Input = "custom-input" },
            Styles = new BitOtpInputClassStyles { Root = "color: red;", Input = "color: blue" },
        });

        Assert.IsTrue(com.Find(".bit-otp").ClassList.Contains("custom-root"));
        StringAssert.Contains(com.Find(".bit-otp").GetAttribute("style"), "color: red;");
        Assert.IsTrue(com.Find(".bit-otp-lbl").ClassList.Contains("custom-label"));

        foreach (var input in com.FindAll(".bit-otp-inp"))
        {
            Assert.IsTrue(input.ClassList.Contains("custom-input"));
            Assert.AreEqual("color: blue", input.GetAttribute("style"));
        }
    }

    [TestMethod]
    public void BitOtpInputParamsUpdateParametersShouldSetAllProperties()
    {
        Func<string, string> transformer = v => v;

        var @params = new BitOtpInputParams
        {
            Accent = BitColor.Warning,
            AutoFocus = true,
            AutoShift = true,
            AutoSubmit = true,
            BlurOnFill = true,
            Classes = new BitOtpInputClassStyles { Root = "cascaded-root" },
            Description = "Cascaded description",
            FullWidth = true,
            InputAriaLabelFormat = "Digit {0} of {1}",
            InputMode = BitInputMode.Tel,
            Invalid = true,
            IsLoading = true,
            Label = "Cascaded label",
            Length = 7,
            Lowercase = true,
            Mask = "*",
            Merged = true,
            NormalizeDigits = true,
            NoSmsAutoFill = true,
            PasteTransformer = transformer,
            Pattern = "^[0-9]$",
            Placeholder = "-",
            Reversed = true,
            Separator = "-",
            SeparatorInterval = 3,
            Sequential = true,
            SingleTabStop = true,
            Size = BitSize.Small,
            Styles = new BitOtpInputClassStyles { Root = "color: red;" },
            Type = BitInputType.Number,
            Uppercase = true,
            Variant = BitVariant.Text,
            Vertical = true,
            ReadOnly = true,
            Required = true,
        };

        var com = RenderWithParams(@params);

        var instance = com.FindComponent<BitOtpInput>().Instance;

        Assert.AreEqual(BitColor.Warning, instance.Accent);
        Assert.IsTrue(instance.AutoFocus);
        Assert.IsTrue(instance.AutoShift);
        Assert.IsTrue(instance.AutoSubmit);
        Assert.IsTrue(instance.BlurOnFill);
        Assert.AreSame(@params.Classes, instance.Classes);
        Assert.AreEqual("Cascaded description", instance.Description);
        Assert.IsTrue(instance.FullWidth);
        Assert.AreEqual("Digit {0} of {1}", instance.InputAriaLabelFormat);
        Assert.AreEqual(BitInputMode.Tel, instance.InputMode);
        Assert.IsTrue(instance.Invalid);
        Assert.IsTrue(instance.IsLoading);
        Assert.AreEqual("Cascaded label", instance.Label);
        Assert.AreEqual(7, instance.Length);
        Assert.IsTrue(instance.Lowercase);
        Assert.AreEqual("*", instance.Mask);
        Assert.IsTrue(instance.Merged);
        Assert.IsTrue(instance.NormalizeDigits);
        Assert.IsTrue(instance.NoSmsAutoFill);
        Assert.AreSame(transformer, instance.PasteTransformer);
        Assert.AreEqual("^[0-9]$", instance.Pattern);
        Assert.AreEqual("-", instance.Placeholder);
        Assert.IsTrue(instance.Reversed);
        Assert.AreEqual("-", instance.Separator);
        Assert.AreEqual(3, instance.SeparatorInterval);
        Assert.IsTrue(instance.Sequential);
        Assert.IsTrue(instance.SingleTabStop);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreSame(@params.Styles, instance.Styles);
        Assert.AreEqual(BitInputType.Number, instance.Type);
        Assert.IsTrue(instance.Uppercase);
        Assert.AreEqual(BitVariant.Text, instance.Variant);
        Assert.IsTrue(instance.Vertical);
        Assert.IsTrue(instance.ReadOnly);
        Assert.IsTrue(instance.Required);

        // And the markup follows what was cascaded, not only the properties.
        Assert.AreEqual(7, com.FindAll(".bit-otp-inp").Count);
        Assert.AreEqual("Digit 1 of 7", com.Find(".bit-otp-inp").GetAttribute("aria-label"));
        Assert.AreEqual("tel", com.Find(".bit-otp-inp").GetAttribute("inputmode"));
        Assert.AreEqual("off", com.Find(".bit-otp-inp").GetAttribute("autocomplete"));
        Assert.AreEqual(2, com.FindAll(".bit-otp-sep").Count);
        Assert.AreEqual("Cascaded description", com.Find(".bit-otp-dsc").TextContent.Trim());
        Assert.IsTrue(com.Find(".bit-otp").ClassList.Contains("bit-inv"));
        Assert.IsTrue(com.Find(".bit-otp").ClassList.Contains("bit-otp-ldg"));
    }

    [TestMethod]
    public void BitOtpInputParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitOtpInputParams
        {
            Length = 6,
            Accent = BitColor.Success,
            Label = "Params label",
        };

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.Accent, BitColor.Error);
            parameters.Add(p => p.Label, "Existing label");
        });

        var instance = com.Instance;

        @params.UpdateParameters(instance);

        // The values stay put because HasNotBeenSet returns false for every one of them.
        Assert.AreEqual(4, instance.Length);
        Assert.AreEqual(BitColor.Error, instance.Accent);
        Assert.AreEqual("Existing label", instance.Label);
    }

    [TestMethod]
    public void BitOtpInputShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var com = RenderWithParams(new BitOtpInputParams());

        var root = com.Find(".bit-otp");

        // Nothing cascades, so the defaults hold: five text inputs, the outline variant, no accent and no size class.
        Assert.AreEqual(5, com.FindAll(".bit-otp-inp").Count);
        Assert.IsTrue(root.ClassList.Contains("bit-otp-otl"));
        Assert.IsFalse(root.ClassList.Contains("bit-otp-pri"));
        Assert.IsFalse(root.ClassList.Contains("bit-otp-md"));
        Assert.IsFalse(root.ClassList.Contains("bit-otp-rdl"));
        Assert.IsFalse(root.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public async Task BitOtpInputShouldTakeAutoSubmitFromTheCascade()
    {
        var com = RenderWithParams(new BitOtpInputParams { Length = 1, AutoSubmit = true });

        await com.Find(".bit-otp-inp").InputAsync(new ChangeEventArgs { Value = "1" });

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.OtpInput.submit"].Count);
    }

    [TestMethod]
    public void BitOtpInputShouldNotCascadeTheValueOfASingleField()
    {
        // Value and DefaultValue identify one field, so they are deliberately not part of the params class: a code
        // shared by every input under the cascade is never what a consumer means. (The Name of the component is not
        // checked here because the params class carries a Name of its own, the discriminator that IBitComponentParams
        // is resolved by.)
        var properties = typeof(BitOtpInputParams).GetProperties();

        Assert.IsNull(Array.Find(properties, p => p.Name == nameof(BitOtpInput.Value)));
        Assert.IsNull(Array.Find(properties, p => p.Name == nameof(BitOtpInput.DefaultValue)));
    }

    [TestMethod]
    public void BitOtpInputParamsShouldCarryEveryPlainParameterOfTheComponent()
    {
        // A parameter added to the component without its counterpart here is one a BitParams cascade silently
        // ignores. Callbacks, templates and the cascade itself are the ones deliberately left out.
        var paramsProperties = typeof(BitOtpInputParams).GetProperties().Select(p => p.Name).ToHashSet();

        var missing = typeof(BitOtpInput).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Where(p => p.PropertyType.Name.StartsWith("EventCallback") is false)
                                         .Where(p => p.PropertyType.Name.StartsWith("RenderFragment") is false)
                                         .Select(p => p.Name)
                                         .Where(n => _notCascaded.Contains(n) is false)
                                         .Where(n => paramsProperties.Contains(n) is false)
                                         .ToList();

        CollectionAssert.AreEqual(new List<string>(), missing, string.Join(", ", missing));
    }
}
