using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TextField;

/// <summary>
/// Covers the BitParams cascade of the TextField: the params object carries every parameter that is not about one
/// field alone, every value it carries reaches the field and its markup - the ones of the input base classes too -
/// what the field wrote for itself is left alone, and an empty params object changes nothing. The basic cascade and
/// override are pinned beside the field's own tests.
/// </summary>
[TestClass]
public class BitTextFieldParamsTests : BunitTestContext
{
    // What belongs to one field alone - what identifies it and carries its value, its event callbacks, and what says
    // something about the value currently in it - is written per field, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitTextField.CascadingParameters),
        nameof(BitTextField.AutoFocus),
        nameof(BitTextField.Invalid),
        nameof(BitTextField.ErrorMessage),
        nameof(BitTextField.ErrorMessageTemplate),
        nameof(BitTextField.GhostText),
        nameof(BitTextField.Loading),
    ];

    private static bool IsCallback(Type type)
    {
        return type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitTextFieldParams fieldParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { fieldParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTextField>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitTextFieldParamsShouldCarryEveryParameterThatIsNotPerField()
    {
        // The parameters the field declares itself, plus the ones of the text input base it closes - which the field's
        // own set never sees, and which a params object has to carry all the same.
        var parameters = typeof(BitTextField).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                             .Concat(typeof(BitTextInputBase<string>).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Where(p => IsCallback(p.PropertyType) is false)
                                             .Select(p => p.Name)
                                             .Where(n => _notCascaded.Contains(n) is false)
                                             .ToList();

        var missing = parameters.Where(name => typeof(BitTextFieldParams).GetProperty(name) is null).ToList();

        Assert.AreEqual(0, missing.Count, "BitTextFieldParams is missing: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void BitTextFieldParamsShouldApplyEveryValueItCarries()
    {
        var component = RenderWithParams(new BitTextFieldParams
        {
            Accent = BitColor.Warning,
            AriaDescription = "Cascaded aria description",
            AutoCapitalize = "words",
            AutoComplete = "email",
            AutoCorrect = false,
            Background = BitColorKind.Secondary,
            Border = BitColorKind.Tertiary,
            ClearButtonAriaLabel = "Cascaded clear",
            ClearButtonIconName = "ChromeClose",
            ClearOnEscape = true,
            CountStrategy = v => (v ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            Description = "Cascaded description",
            EnterKeyHint = "search",
            FullWidth = true,
            IconAriaLabel = "Cascaded icon name",
            IconName = "Lock",
            IconPlacement = BitPlacement.Start,
            InputMode = BitInputMode.Email,
            Label = "Cascaded label",
            LabelPlacement = BitPlacement.Start,
            MaxLength = 20,
            MinLength = 2,
            NoBorder = true,
            Pattern = "[a-z]+",
            PermanentGhost = true,
            Placeholder = "Cascaded placeholder",
            Prefix = "https://",
            ReadOnly = true,
            Required = true,
            ShowClearButton = true,
            ShowCount = true,
            Size = BitSize.Large,
            SpellCheck = true,
            Suffix = ".com",
            Title = "Cascaded title",
            Trim = true,
            Underlined = true,
            AriaLabel = "Cascaded aria label",
            TabIndex = "4",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTextField.DefaultValue), "one two three");
        });

        var instance = component.FindComponent<BitTextField>().Instance;
        var root = component.Find(".bit-tfl");
        var input = component.Find(".bit-tfl-inp");

        Assert.IsTrue(instance.ClearOnEscape);
        Assert.IsTrue(instance.Trim);
        Assert.AreEqual("Cascaded clear", instance.ClearButtonAriaLabel);

        Assert.IsTrue(root.ClassList.Contains("bit-tfl-wrn"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-bse"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-brt"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-fwd"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-lst"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-nbd"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-pgt"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-und"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-rdl"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-req"));

        Assert.AreEqual("words", input.GetAttribute("autocapitalize"));
        Assert.AreEqual("email", input.GetAttribute("autocomplete"));
        Assert.AreEqual("off", input.GetAttribute("autocorrect"));
        Assert.AreEqual("search", input.GetAttribute("enterkeyhint"));
        Assert.AreEqual("email", input.GetAttribute("inputmode"));
        Assert.AreEqual("20", input.GetAttribute("maxlength"));
        Assert.AreEqual("2", input.GetAttribute("minlength"));
        Assert.AreEqual("[a-z]+", input.GetAttribute("pattern"));
        Assert.AreEqual("Cascaded placeholder", input.GetAttribute("placeholder"));
        Assert.AreEqual("true", input.GetAttribute("spellcheck"));
        Assert.AreEqual("Cascaded title", input.GetAttribute("title"));
        Assert.AreEqual("Cascaded aria label", input.GetAttribute("aria-label"));
        Assert.AreEqual("4", input.GetAttribute("tabindex"));
        Assert.IsTrue(input.HasAttribute("readonly"));
        Assert.IsTrue(input.HasAttribute("required"));

        Assert.AreEqual("Cascaded label", component.Find(".bit-tfl-lbl").TextContent.Trim());
        Assert.AreEqual("Cascaded description", component.Find(".bit-tfl-des").TextContent.Trim());
        Assert.AreEqual("Cascaded aria description", component.Find(".bit-tfl-dsc:not([role])").TextContent.Trim());
        Assert.AreEqual("https://", component.Find(".bit-tfl-pre").TextContent.Trim());
        Assert.AreEqual(".com", component.Find(".bit-tfl-suf").TextContent.Trim());

        var icon = component.Find(".bit-tfl-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Lock"));
        Assert.AreEqual("Cascaded icon name", icon.GetAttribute("aria-label"));

        // The cascaded strategy counts words, and the cascaded limit is shown beside the count.
        Assert.AreEqual("3/20", component.Find(".bit-tfl-cnt").TextContent.Trim());

        // The clear button follows the cascaded name and icon, but a read-only field cannot use it.
        var clearButton = component.Find(".bit-tfl-cbt");
        Assert.AreEqual("Cascaded clear", clearButton.GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-tfl-cbt > i").ClassList.Contains("bit-icon--ChromeClose"));
        Assert.IsTrue(clearButton.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitTextFieldParamsShouldApplyTheMultilineAndPasswordValues()
    {
        var multiline = RenderWithParams(new BitTextFieldParams
        {
            Multiline = true,
            AutoHeight = true,
            Resizable = true,
            Rows = 4,
            MaxRows = 8,
            PreventEnter = true,
            Wrap = "off",
            SelectOnFocus = true,
        });

        var textarea = multiline.Find("textarea");
        var root = multiline.Find(".bit-tfl");

        Assert.IsTrue(root.ClassList.Contains("bit-tfl-mln"));
        Assert.IsTrue(root.ClassList.Contains("bit-tfl-mla"));
        Assert.AreEqual("4", textarea.GetAttribute("rows"));
        Assert.AreEqual("off", textarea.GetAttribute("wrap"));

        // The cascaded values reach the scripts the multiline mode and the selection are wired with.
        var setup = Context.JSInterop.Invocations["BitBlazorUI.TextField.setupMultilineInput"].Single();
        Assert.AreEqual(true, setup.Arguments[2]);
        Assert.AreEqual(true, setup.Arguments[3]);
        Assert.AreEqual(8, setup.Arguments[4]);
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.TextField.setupSelectOnFocus"].Count);

        var password = RenderWithParams(new BitTextFieldParams
        {
            Type = BitInputType.Password,
            CanRevealPassword = true,
            RevealPasswordAriaLabel = "Cascaded reveal",
            RevealPasswordIconName = "RedEye",
            HidePasswordIconName = "Hide",
        });

        var reveal = password.Find(".bit-tfl-rpb");

        Assert.AreEqual("Cascaded reveal", reveal.GetAttribute("aria-label"));
        Assert.IsTrue(password.Find(".bit-tfl-rpi").ClassList.Contains("bit-icon--RedEye"));

        reveal.Click();

        Assert.IsTrue(password.Find(".bit-tfl-rpi").ClassList.Contains("bit-icon--Hide"));
    }

    [TestMethod]
    public void BitTextFieldParamsShouldApplyTheTemplates()
    {
        var component = RenderWithParams(new BitTextFieldParams
        {
            ShowClearButton = true,
            ShowCount = true,
            Type = BitInputType.Password,
            CanRevealPassword = true,
            LabelTemplate = builder => builder.AddMarkupContent(0, "<em class=\"tpl-label\">L</em>"),
            PrefixTemplate = builder => builder.AddMarkupContent(0, "<em class=\"tpl-prefix\">P</em>"),
            SuffixTemplate = builder => builder.AddMarkupContent(0, "<em class=\"tpl-suffix\">S</em>"),
            DescriptionTemplate = builder => builder.AddMarkupContent(0, "<em class=\"tpl-description\">D</em>"),
            ClearButtonTemplate = builder => builder.AddMarkupContent(0, "<em class=\"tpl-clear\">C</em>"),
            CountTemplate = count => builder => builder.AddMarkupContent(0, $"<em class=\"tpl-count\">[{count}]</em>"),
            RevealPasswordTemplate = revealed => builder => builder.AddMarkupContent(0, $"<em class=\"tpl-reveal\">{(revealed ? "hide" : "show")}</em>"),
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTextField.DefaultValue), "secret");
        });

        Assert.AreEqual("L", component.Find(".bit-tfl-lbl .tpl-label").TextContent);
        Assert.AreEqual("P", component.Find(".bit-tfl-fgp > .tpl-prefix").TextContent);
        Assert.AreEqual("S", component.Find(".bit-tfl-fgp > .tpl-suffix").TextContent);
        Assert.AreEqual("D", component.Find(".bit-tfl-des .tpl-description").TextContent);
        Assert.AreEqual("C", component.Find(".bit-tfl-cbt .tpl-clear").TextContent);
        Assert.AreEqual("[6]", component.Find(".bit-tfl-cnt .tpl-count").TextContent);
        Assert.AreEqual("show", component.Find(".bit-tfl-rpb .tpl-reveal").TextContent);
    }

    [TestMethod]
    public void BitTextFieldParamsShouldApplyTheRateLimitingOfTheInputBase()
    {
        var component = RenderWithParams(new BitTextFieldParams
        {
            Immediate = true,
            DebounceTime = 0,
            ThrottleTime = 60_000,
        });

        var instance = component.FindComponent<BitTextField>().Instance;

        Assert.IsTrue(instance.Immediate);
        Assert.AreEqual(0, instance.DebounceTime);
        Assert.AreEqual(60_000, instance.ThrottleTime);

        // A cascaded Immediate is a real one: the first keystroke of a burst commits the value from the input event.
        component.Find(".bit-tfl-inp").Input("a");

        Assert.AreEqual("a", instance.Value);
    }

    [TestMethod]
    public void BitTextFieldShouldKeepAnOwnFalseOverTheCascadedTrue()
    {
        var component = RenderWithParams(new BitTextFieldParams
        {
            Required = true,
            ReadOnly = true,
            Multiline = true,
            Underlined = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTextField.Required), false);
            builder.AddAttribute(2, nameof(BitTextField.ReadOnly), false);
            builder.AddAttribute(3, nameof(BitTextField.Multiline), false);
        });

        var input = component.Find(".bit-tfl-inp");

        // A false written on the field is a choice as much as a true is.
        Assert.AreEqual("INPUT", input.TagName);
        Assert.IsFalse(input.HasAttribute("required"));
        Assert.IsFalse(input.HasAttribute("readonly"));

        Assert.IsTrue(component.Find(".bit-tfl").ClassList.Contains("bit-tfl-und"));
    }

    [TestMethod]
    public void BitTextFieldParamsUpdateParametersShouldNotOverwriteWhatTheFieldSetItself()
    {
        var @params = new BitTextFieldParams
        {
            Label = "Cascaded label",
            Placeholder = "Cascaded placeholder",
            Size = BitSize.Large,
            Type = BitInputType.Email,
            MaxLength = 5,
            Immediate = true,
            Required = true,
            Title = "Cascaded title",
        };

        var component = RenderComponent<BitTextField>(parameters =>
        {
            parameters.Add(p => p.Label, "Own label");
            parameters.Add(p => p.Placeholder, "Own placeholder");
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Type, BitInputType.Tel);
            parameters.Add(p => p.MaxLength, 12);
            parameters.Add(p => p.Immediate, false);
            parameters.Add(p => p.Required, false);
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual("Own label", instance.Label);
        Assert.AreEqual("Own placeholder", instance.Placeholder);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual(BitInputType.Tel, instance.Type);
        Assert.AreEqual(12, instance.MaxLength);
        Assert.IsFalse(instance.Immediate);
        Assert.IsFalse(instance.Required);

        // Only what the field did not set itself is taken.
        Assert.AreEqual("Cascaded title", instance.Title);
    }

    [TestMethod]
    public void BitTextFieldParamsUpdateParametersShouldIgnoreANullField()
    {
        var @params = new BitTextFieldParams { Label = "Cascaded label" };

        @params.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitTextFieldShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitTextFieldParams());

        var root = component.Find(".bit-tfl");
        var input = component.Find(".bit-tfl-inp");

        Assert.AreEqual("bit-tfl", root.GetAttribute("class"));
        Assert.IsNull(root.GetAttribute("style"));
        Assert.AreEqual("INPUT", input.TagName);
        Assert.AreEqual("text", input.GetAttribute("type"));
        Assert.IsFalse(input.HasAttribute("placeholder"));
        Assert.IsFalse(input.HasAttribute("maxlength"));
        Assert.IsFalse(input.HasAttribute("readonly"));
        Assert.IsFalse(input.HasAttribute("required"));
        Assert.AreEqual(0, component.FindAll(".bit-tfl-lbl").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tfl-ftr").Count);
    }
}
