using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.NumberField;

/// <summary>
/// Covers the BitParams cascade of the NumberField: what a BitNumberFieldParams fills in, what it leaves alone
/// because the field wrote it for itself, that one non-generic params object reaches a field of any value type,
/// and that what the field derives from its parameters in setters of its own - the bounds, the step, the
/// precision, the input mode - follows the cascaded values.
/// </summary>
[TestClass]
public class BitNumberFieldParamsTests : BunitTestContext
{
    // What belongs to one field alone - its label, its value, its templates, its callbacks and the state the
    // app drives it through - is written per field, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitNumberField<int>.CascadingParameters),
        nameof(BitNumberField<int>.AriaValueNow),
        nameof(BitNumberField<int>.DescriptionTemplate),
        nameof(BitNumberField<int>.ErrorMessage),
        nameof(BitNumberField<int>.ErrorMessageTemplate),
        nameof(BitNumberField<int>.Invalid),
        nameof(BitNumberField<int>.Label),
        nameof(BitNumberField<int>.LabelTemplate),
        nameof(BitNumberField<int>.Loading),
        nameof(BitNumberField<int>.LoadingTemplate),
        nameof(BitNumberField<int>.OnBlur),
        nameof(BitNumberField<int>.OnClear),
        nameof(BitNumberField<int>.OnClick),
        nameof(BitNumberField<int>.OnDecrement),
        nameof(BitNumberField<int>.OnEnter),
        nameof(BitNumberField<int>.OnEscape),
        nameof(BitNumberField<int>.OnFocus),
        nameof(BitNumberField<int>.OnFocusIn),
        nameof(BitNumberField<int>.OnFocusOut),
        nameof(BitNumberField<int>.OnIncrement),
        nameof(BitNumberField<int>.OnKeyDown),
        nameof(BitNumberField<int>.OnKeyUp),
        nameof(BitNumberField<int>.OnMaxReached),
        nameof(BitNumberField<int>.OnMinReached),
        nameof(BitNumberField<int>.PrefixTemplate),
        nameof(BitNumberField<int>.SuffixTemplate),
        nameof(BitNumberField<int>.ClearButtonTemplate),
    ];

    private static RenderFragment RenderNumberField<TValue>(Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<BitNumberField<TValue>>(0);
            extraAttributes?.Invoke(builder);
            builder.CloseComponent();
        };
    }

    private IRenderedComponent<BitParams> RenderWithParams<TValue>(BitNumberFieldParams fieldParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { fieldParams });
            parameters.AddChildContent(RenderNumberField<TValue>(extraAttributes));
        });
    }

    [TestMethod]
    public void BitNumberFieldParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.BitNumberField", BitNumberFieldParams.ParamName);
        Assert.AreEqual("BitParams.BitNumberField", BitNumberFieldParams.ParamName);
    }

    [TestMethod]
    public void BitNumberFieldParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitNumberFieldParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitNumberFieldParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitNumberFieldParamsShouldCarryEveryParameterThatIsNotPerField()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name)
                                                     .Concat(typeof(BitInputBase<int>).GetProperties().Select(p => p.Name))
                                                     .Concat(typeof(BitTextInputBase<int>).GetProperties().Select(p => p.Name))
                                                     .ToHashSet();

        var parameters = typeof(BitNumberField<int>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                     .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                     .Select(p => p.Name)
                                                     .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitNumberFieldParams).GetProperty(name), $"BitNumberFieldParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitNumberFieldParamsShouldNameNothingTheFieldDoesNotHave()
    {
        var fieldParameters = typeof(BitNumberField<int>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                          .Select(p => p.Name)
                                                          .ToHashSet();

        var paramsProperties = typeof(BitNumberFieldParams).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                           .Where(p => p.CanWrite)
                                                           .Select(p => p.Name);

        foreach (var name in paramsProperties)
        {
            Assert.IsTrue(fieldParameters.Contains(name), $"BitNumberFieldParams.{name} is no parameter of the field.");
        }
    }

    [TestMethod]
    public void BitNumberFieldShouldApplyCascadedClasses()
    {
        var component = RenderWithParams<int>(new BitNumberFieldParams
        {
            Accent = BitColor.Error,
            Background = BitColorKind.Secondary,
            Border = BitColorKind.Tertiary,
            Size = BitSize.Large,
            Mode = BitSpinButtonMode.Inline,
            LabelPlacement = BitPlacement.Start,
            FullWidth = true,
            NoBorder = true,
            Underlined = true,
            Classes = new() { Root = "cascaded-root", Input = "cascaded-input" },
        });

        var root = component.Find(".bit-nfl");

        Assert.IsTrue(root.ClassList.Contains("bit-nfl-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-bse"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-brt"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-min"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-lst"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-fwd"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-nbd"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-und"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-nfl-inp").ClassList.Contains("cascaded-input"));

        // The cascaded mode reaches the markup: two spin buttons where none would be drawn without it.
        Assert.AreEqual(2, component.FindAll(".bit-nfl-sbn").Count);
    }

    [TestMethod]
    public void BitNumberFieldShouldApplyCascadedStyles()
    {
        var component = RenderWithParams<int>(new BitNumberFieldParams
        {
            Mode = BitSpinButtonMode.Compact,
            Styles = new() { Root = "--s: root;", Input = "--s: input;", IncrementButton = "--s: increment;" },
        });

        StringAssert.Contains(component.Find(".bit-nfl").GetAttribute("style"), "--s: root;");
        Assert.AreEqual("--s: input;", component.Find(".bit-nfl-inp").GetAttribute("style"));
        Assert.AreEqual("--s: increment;", component.Find(".bit-nfl-aup").GetAttribute("style"));
    }

    [TestMethod]
    public void BitNumberFieldShouldApplyCascadedBoundsStepAndPrecision()
    {
        var component = RenderWithParams<double>(new BitNumberFieldParams
        {
            Min = "2",
            Max = "8",
            Step = "0.5",
            PageStep = "3",
            Precision = 1,
        }, builder => builder.AddAttribute(1, nameof(BitNumberField<double>.DefaultValue), 5d));

        var input = component.Find("input");
        var instance = component.FindComponent<BitNumberField<double>>().Instance;

        // Min/Max/Step/PageStep are strings parsed by setters of their own, so a cascaded value only reaches the
        // bounds and the step when the cascade runs them.
        Assert.AreEqual("2", input.GetAttribute("aria-valuemin"));
        Assert.AreEqual("8", input.GetAttribute("aria-valuemax"));

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual(5.5, instance.Value);

        input.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        Assert.AreEqual(8d, instance.Value);

        input.KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.AreEqual(2d, instance.Value);

        component.Find("input").Change("3.26");
        Assert.AreEqual(3.3, instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldShouldApplyCascadedInputModeAndEnterKeyHint()
    {
        var component = RenderWithParams<int>(new BitNumberFieldParams
        {
            InputMode = BitInputMode.Text,
            EnterKeyHint = "next",
        });

        var input = component.Find("input");

        // The input mode is resolved in a setter of its own, so the cascade has to run it.
        Assert.AreEqual("text", input.GetAttribute("inputmode"));
        Assert.AreEqual("next", input.GetAttribute("enterkeyhint"));
    }

    [TestMethod]
    public void BitNumberFieldShouldApplyCascadedCulture()
    {
        var component = RenderWithParams<double>(new BitNumberFieldParams
        {
            Culture = CultureInfo.GetCultureInfo("de-DE"),
        }, builder => builder.AddAttribute(1, nameof(BitNumberField<double>.DefaultValue), 1234.5));

        Assert.AreEqual("1234,5", component.Find("input").GetAttribute("value"));
    }

    [TestMethod]
    public void BitNumberFieldShouldApplyCascadedTextsAndFlags()
    {
        var component = RenderWithParams<int?>(new BitNumberFieldParams
        {
            Mode = BitSpinButtonMode.Compact,
            Placeholder = "Cascaded placeholder",
            Prefix = "$",
            Suffix = "kg",
            Title = "Cascaded title",
            Description = "Cascaded description",
            AriaDescription = "Cascaded aria description",
            IncrementAriaLabel = "More",
            DecrementAriaLabel = "Less",
            IncrementTitle = "One more",
            DecrementTitle = "One less",
            IconName = "Add",
            IconAriaLabel = "Cascaded icon",
            ShowClearButton = true,
            ClearButtonAriaLabel = "Wipe",
            ClearButtonIconName = "Clear",
            IncrementIconName = "Up",
            DecrementIconName = "Down",
            HideInput = false,
            IsInputReadOnly = true,
            LoadingAriaLabel = "Cascaded loading",
        }, builder => builder.AddAttribute(1, nameof(BitNumberField<int?>.DefaultValue), (int?)3));

        var input = component.Find("input");

        Assert.AreEqual("Cascaded placeholder", input.GetAttribute("placeholder"));
        Assert.IsTrue(input.HasAttribute("readonly"));
        Assert.AreEqual("$", component.Find(".bit-nfl-pre span").TextContent.Trim());
        Assert.AreEqual("kg", component.Find(".bit-nfl-suf span").TextContent.Trim());
        Assert.AreEqual("Cascaded title", component.Find(".bit-nfl-cnt").GetAttribute("title"));
        Assert.AreEqual("Cascaded description", component.Find(".bit-nfl-des span").TextContent.Trim());
        Assert.AreEqual("Cascaded aria description", component.Find(".bit-nfl-dsc[id]").TextContent);

        var incrementButton = component.Find(".bit-nfl-aup");
        Assert.AreEqual("More", incrementButton.GetAttribute("aria-label"));
        Assert.AreEqual("One more", incrementButton.GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-nfl-aup i").ClassList.Contains("bit-icon--Up"));

        var decrementButton = component.Find(".bit-nfl-adn");
        Assert.AreEqual("Less", decrementButton.GetAttribute("aria-label"));
        Assert.AreEqual("One less", decrementButton.GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-nfl-adn i").ClassList.Contains("bit-icon--Down"));

        var icon = component.Find(".bit-nfl-lic");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Add"));
        Assert.AreEqual("Cascaded icon", icon.GetAttribute("aria-label"));

        var clearButton = component.Find(".bit-nfl-cbt");
        Assert.AreEqual("Wipe", clearButton.GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-nfl-cbt i").ClassList.Contains("bit-icon--Clear"));
    }

    [TestMethod]
    public void BitNumberFieldShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams<int>(new BitNumberFieldParams
        {
            Dir = BitDir.Rtl,
            Disabled = true,
            Class = "cascaded",
            Style = "margin: 1px;",
            Id = "cascaded-id",
            AriaLabel = "Cascaded aria label",
            TabIndex = "3",
            HtmlAttributes = new Dictionary<string, object> { ["data-cascaded"] = "yes" },
        });

        var root = component.Find(".bit-nfl");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        Assert.AreEqual("cascaded-id", root.Id);
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));

        var input = component.Find("input");
        Assert.IsTrue(input.HasAttribute("disabled"));
        Assert.AreEqual("Cascaded aria label", input.GetAttribute("aria-label"));
        Assert.AreEqual("3", component.FindComponent<BitNumberField<int>>().Instance.TabIndex);
    }

    [TestMethod]
    public void BitNumberFieldShouldApplyTheCascadedReadOnlyAndRequired()
    {
        // The two input-base parameters BitInputBaseParams carries for the other inputs reach a field of any value
        // type through the field's own params object.
        var component = RenderWithParams<decimal>(new BitNumberFieldParams { ReadOnly = true, Required = true });

        var field = component.FindComponent<BitNumberField<decimal>>().Instance;
        Assert.IsTrue(field.ReadOnly);
        Assert.IsTrue(field.Required);

        var input = component.Find("input");
        Assert.IsTrue(input.HasAttribute("readonly"));
        Assert.IsTrue(input.HasAttribute("required"));
    }

    [TestMethod]
    public void BitNumberFieldShouldKeepItsOwnReadOnlyAndRequiredOverTheCascadedOnes()
    {
        var component = RenderWithParams<int>(new BitNumberFieldParams { ReadOnly = true, Required = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitNumberField<int>.ReadOnly), false);
            builder.AddAttribute(2, nameof(BitNumberField<int>.Required), false);
        });

        // A false written on the field is a choice as much as a true is.
        var field = component.FindComponent<BitNumberField<int>>().Instance;
        Assert.IsFalse(field.ReadOnly);
        Assert.IsFalse(field.Required);
    }

    [TestMethod]
    public void BitNumberFieldShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams<int>(new BitNumberFieldParams
        {
            Accent = BitColor.Error,
            Size = BitSize.Large,
            Mode = BitSpinButtonMode.Compact,
            Placeholder = "Cascaded placeholder",
            Min = "10",
            Max = "20",
            Step = "5",
            Underlined = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitNumberField<int>.Accent), (BitColor?)BitColor.Success);
            builder.AddAttribute(2, nameof(BitNumberField<int>.Size), (BitSize?)BitSize.Small);
            builder.AddAttribute(3, nameof(BitNumberField<int>.Mode), (BitSpinButtonMode?)BitSpinButtonMode.Spread);
            builder.AddAttribute(4, nameof(BitNumberField<int>.Placeholder), "Own placeholder");
            builder.AddAttribute(5, nameof(BitNumberField<int>.Min), "0");
            builder.AddAttribute(6, nameof(BitNumberField<int>.Step), "2");
            builder.AddAttribute(7, nameof(BitNumberField<int>.DefaultValue), 1);
        });

        var root = component.Find(".bit-nfl");
        var input = component.Find("input");
        var instance = component.FindComponent<BitNumberField<int>>().Instance;

        Assert.IsTrue(root.ClassList.Contains("bit-nfl-suc"));
        Assert.IsFalse(root.ClassList.Contains("bit-nfl-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-nfl-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-msp"));
        Assert.AreEqual("Own placeholder", input.GetAttribute("placeholder"));
        Assert.AreEqual("0", input.GetAttribute("aria-valuemin"));

        // What the field left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-nfl-und"));
        Assert.AreEqual("20", input.GetAttribute("aria-valuemax"));

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual(3, instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldParamsShouldReachAFieldOfAnyValueType()
    {
        var @params = new BitNumberFieldParams { Size = BitSize.Large, Step = "0.25" };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(builder =>
            {
                builder.AddContent(0, RenderNumberField<decimal>(b => b.AddAttribute(1, nameof(BitNumberField<decimal>.DefaultValue), 1m)));
                builder.AddContent(1, RenderNumberField<byte?>());
            });
        });

        foreach (var root in component.FindAll(".bit-nfl"))
        {
            Assert.IsTrue(root.ClassList.Contains("bit-nfl-lg"));
        }

        component.FindAll("input")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual(1.25m, component.FindComponent<BitNumberField<decimal>>().Instance.Value);
    }

    [TestMethod]
    public void BitNumberFieldParamsUpdateParametersShouldSetAllProperties()
    {
        static string? normalizer(string? value) => value;

        var @params = new BitNumberFieldParams
        {
            Accent = BitColor.Warning,
            AriaDescription = "Cascaded aria description",
            AriaPositionInSet = 2,
            AriaSetSize = 5,
            AriaValueText = "two",
            Background = BitColorKind.Secondary,
            Border = BitColorKind.Tertiary,
            Classes = new() { Root = "cascaded-root" },
            ClearButtonAriaLabel = "Cascaded clear",
            ClearButtonIcon = BitIconInfo.Bi("x"),
            ClearButtonIconName = "Clear",
            ContinuousSpinDelay = 111,
            ContinuousSpinInterval = 22,
            Culture = CultureInfo.GetCultureInfo("fr-FR"),
            DecrementAriaLabel = "Less",
            DecrementIcon = BitIconInfo.Bi("dash"),
            DecrementIconName = "Down",
            DecrementTitle = "One less",
            Description = "Cascaded description",
            DigitsNormalizer = normalizer,
            EnterKeyHint = "done",
            FullWidth = true,
            HideInput = true,
            Icon = BitIconInfo.Bi("hash"),
            IconAriaLabel = "Cascaded icon",
            IconName = "Add",
            IncrementAriaLabel = "More",
            IncrementIcon = BitIconInfo.Bi("plus"),
            IncrementIconName = "Up",
            IncrementTitle = "One more",
            InputMode = BitInputMode.Text,
            InvertMouseWheel = true,
            IsInputReadOnly = true,
            LabelPlacement = BitPlacement.End,
            LoadingAriaLabel = "Cascaded loading",
            Max = "90",
            Min = "10",
            Mode = BitSpinButtonMode.Spread,
            NoBorder = true,
            NoClamp = true,
            NoMouseWheel = true,
            NoSelectOnFocus = true,
            NormalizeDigits = true,
            NumberFormat = "N1",
            PageStep = "7",
            ParsingErrorMessage = "Cascaded parsing error",
            Placeholder = "Cascaded placeholder",
            Precision = 3,
            Prefix = "Cascaded prefix",
            ShowClearButton = true,
            Size = BitSize.Small,
            SnapToStep = true,
            Step = "3",
            Styles = new() { Root = "--s: root;" },
            Suffix = "Cascaded suffix",
            Title = "Cascaded title",
            Underlined = true,
            AriaLabel = "Cascaded aria label",
            TabIndex = "5",
        };

        var component = RenderWithParams<double>(@params);

        var instance = component.FindComponent<BitNumberField<double>>().Instance;

        Assert.AreEqual(BitColor.Warning, instance.Accent);
        Assert.AreEqual("Cascaded aria description", instance.AriaDescription);
        Assert.AreEqual(2, instance.AriaPositionInSet);
        Assert.AreEqual(5, instance.AriaSetSize);
        Assert.AreEqual("two", instance.AriaValueText);
        Assert.AreEqual(BitColorKind.Secondary, instance.Background);
        Assert.AreEqual(BitColorKind.Tertiary, instance.Border);
        Assert.AreSame(@params.Classes, instance.Classes);
        Assert.AreEqual("Cascaded clear", instance.ClearButtonAriaLabel);
        Assert.AreEqual(@params.ClearButtonIcon, instance.ClearButtonIcon);
        Assert.AreEqual("Clear", instance.ClearButtonIconName);
        Assert.AreEqual(111, instance.ContinuousSpinDelay);
        Assert.AreEqual(22, instance.ContinuousSpinInterval);
        Assert.AreEqual(CultureInfo.GetCultureInfo("fr-FR"), instance.Culture);
        Assert.AreEqual("Less", instance.DecrementAriaLabel);
        Assert.AreEqual(@params.DecrementIcon, instance.DecrementIcon);
        Assert.AreEqual("Down", instance.DecrementIconName);
        Assert.AreEqual("One less", instance.DecrementTitle);
        Assert.AreEqual("Cascaded description", instance.Description);
        Assert.AreEqual((Func<string?, string?>)normalizer, instance.DigitsNormalizer);
        Assert.AreEqual("done", instance.EnterKeyHint);
        Assert.IsTrue(instance.FullWidth);
        Assert.IsTrue(instance.HideInput);
        Assert.AreEqual(@params.Icon, instance.Icon);
        Assert.AreEqual("Cascaded icon", instance.IconAriaLabel);
        Assert.AreEqual("Add", instance.IconName);
        Assert.AreEqual("More", instance.IncrementAriaLabel);
        Assert.AreEqual(@params.IncrementIcon, instance.IncrementIcon);
        Assert.AreEqual("Up", instance.IncrementIconName);
        Assert.AreEqual("One more", instance.IncrementTitle);
        Assert.AreEqual(BitInputMode.Text, instance.InputMode);
        Assert.IsTrue(instance.InvertMouseWheel);
        Assert.IsTrue(instance.IsInputReadOnly);
        Assert.AreEqual(BitPlacement.End, instance.LabelPlacement);
        Assert.AreEqual("Cascaded loading", instance.LoadingAriaLabel);
        Assert.AreEqual("90", instance.Max);
        Assert.AreEqual("10", instance.Min);
        Assert.AreEqual(BitSpinButtonMode.Spread, instance.Mode);
        Assert.IsTrue(instance.NoBorder);
        Assert.IsTrue(instance.NoClamp);
        Assert.IsTrue(instance.NoMouseWheel);
        Assert.IsTrue(instance.NoSelectOnFocus);
        Assert.IsTrue(instance.NormalizeDigits);
        Assert.AreEqual("N1", instance.NumberFormat);
        Assert.AreEqual("7", instance.PageStep);
        Assert.AreEqual("Cascaded parsing error", instance.ParsingErrorMessage);
        Assert.AreEqual("Cascaded placeholder", instance.Placeholder);
        Assert.AreEqual(3, instance.Precision);
        Assert.AreEqual("Cascaded prefix", instance.Prefix);
        Assert.IsTrue(instance.ShowClearButton);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.IsTrue(instance.SnapToStep);
        Assert.AreEqual("3", instance.Step);
        Assert.AreSame(@params.Styles, instance.Styles);
        Assert.AreEqual("Cascaded suffix", instance.Suffix);
        Assert.AreEqual("Cascaded title", instance.Title);
        Assert.IsTrue(instance.Underlined);
        Assert.AreEqual("Cascaded aria label", instance.AriaLabel);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitNumberFieldParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitNumberFieldParams
        {
            Accent = BitColor.Success,
            Size = BitSize.Large,
            Placeholder = "Params placeholder",
            Step = "5",
        };

        var component = RenderComponent<BitNumberField<int>>(parameters =>
        {
            parameters.Add(p => p.Accent, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Placeholder, "Existing placeholder");
            parameters.Add(p => p.Step, "2");
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual(BitColor.Error, instance.Accent);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing placeholder", instance.Placeholder);
        Assert.AreEqual("2", instance.Step);
    }

    [TestMethod]
    public void BitNumberFieldParamsUpdateParametersShouldIgnoreANullField()
    {
        var @params = new BitNumberFieldParams { Accent = BitColor.Success };

        @params.UpdateParameters<int>(null!);
    }

    [TestMethod]
    public void BitNumberFieldShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams<int>(new BitNumberFieldParams());

        var root = component.Find(".bit-nfl");

        // Nothing cascades, so Accent, Background, Border and Size stay unset and publish nothing, and no spin
        // button is drawn.
        foreach (var cssClass in new[] { "bit-nfl-pri", "bit-nfl-bpr", "bit-nfl-brp", "bit-nfl-md", "bit-nfl-mcp", "bit-nfl-und", "bit-nfl-nbd", "bit-nfl-fwd" })
        {
            Assert.IsFalse(root.ClassList.Contains(cssClass), $"An empty params object published {cssClass}.");
        }

        Assert.AreEqual(0, component.FindAll("button").Count);
        Assert.AreEqual("numeric", component.Find("input").GetAttribute("inputmode"));
    }

    [TestMethod]
    public void TheHtmlAttributesOfTheInstanceShouldWinOverTheCascadedOnes()
    {
        var component = RenderWithParams<int>(new BitNumberFieldParams
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-both"] = "from-cascade", ["data-cascaded"] = "yes" },
        }, builder =>
        {
            builder.AddAttribute(1, "data-both", "from-instance");
        });

        var root = component.Find(".bit-nfl");

        Assert.AreEqual("from-instance", root.GetAttribute("data-both"));
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
    }

    [TestMethod]
    public void TheBoundsOfADroppedParamsObjectShouldGo()
    {
        var component = RenderWithParams<int>(new BitNumberFieldParams { Min = "2", Max = "8" });

        Assert.AreEqual("2", component.Find("input").GetAttribute("aria-valuemin"));
        Assert.AreEqual("8", component.Find("input").GetAttribute("aria-valuemax"));

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitNumberFieldParams { Size = BitSize.Large } }));

        // The setters of Min and Max run again for the value put back, so the bounds go with the params object.
        var input = component.Find("input");
        Assert.IsNull(input.GetAttribute("aria-valuemin"));
        Assert.IsNull(input.GetAttribute("aria-valuemax"));
        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-lg"));
    }
}
