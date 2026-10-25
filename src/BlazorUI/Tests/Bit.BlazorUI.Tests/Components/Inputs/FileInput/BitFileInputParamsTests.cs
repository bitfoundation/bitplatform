using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileInput;

/// <summary>
/// Covers the BitParams cascade of the FileInput: what a BitFileInputParams fills in, what it leaves alone because
/// the file input wrote it for itself, and that the inherited input parameters - ReadOnly, Required - travel with
/// the component's own.
/// </summary>
[TestClass]
public class BitFileInputParamsTests : BunitTestContext
{
    // What belongs to one file input alone - its templates and its callbacks - is written per component, so it
    // has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitFileInput.CascadingParameters),
        nameof(BitFileInput.DescriptionTemplate),
        nameof(BitFileInput.FileViewTemplate),
        nameof(BitFileInput.LabelTemplate),
        nameof(BitFileInput.OnFilesChange),
        nameof(BitFileInput.OnInvalid),
        nameof(BitFileInput.OnRemove),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitFileInputParams fileInputParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { fileInputParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFileInput>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    private void SetupFiles(BitFileInputInfo[] files)
    {
        Context.JSInterop.Setup<BitFileInputInfo[]>("BitBlazorUI.FileInput.setup", _ => true).SetResult(files);
    }

    [TestMethod]
    public void BitFileInputParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitFileInput)}", BitFileInputParams.ParamName);
        Assert.AreEqual("BitParams.BitFileInput", BitFileInputParams.ParamName);
    }

    [TestMethod]
    public void BitFileInputParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitFileInputParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.IsInstanceOfType<BitInputBaseParams<string?>>(@params);
        Assert.AreEqual(BitFileInputParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitFileInputParamsShouldCarryEveryParameterThatIsNotPerComponent()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name)
                                                     .Concat(typeof(BitInputBase<string?>).GetProperties().Select(p => p.Name))
                                                     .Where(n => n is not (nameof(BitFileInput.ReadOnly) or nameof(BitFileInput.Required)))
                                                     .ToHashSet();

        var parameters = typeof(BitFileInput).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Select(p => p.Name)
                                             .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitFileInputParams).GetProperty(name), $"BitFileInputParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitFileInputParamsShouldNameNothingTheComponentDoesNotHave()
    {
        var componentParameters = typeof(BitFileInput).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                      .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                      .Select(p => p.Name)
                                                      .ToHashSet();

        var paramsProperties = typeof(BitFileInputParams).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                         .Where(p => p.CanWrite)
                                                         .Select(p => p.Name);

        foreach (var name in paramsProperties)
        {
            Assert.IsTrue(componentParameters.Contains(name), $"BitFileInputParams.{name} is no parameter of the component.");
        }
    }

    [TestMethod]
    public void BitFileInputShouldApplyCascadedClassesAndStyles()
    {
        var component = RenderWithParams(new BitFileInputParams
        {
            Color = BitColor.Error,
            Size = BitSize.Small,
            Variant = BitVariant.Text,
            ShowDropZone = true,
            HideLabel = false,
            Classes = new() { Root = "cascaded-root", Label = "cascaded-label", DropZoneIcon = "cascaded-dzi" },
            Styles = new() { Root = "--s: root;", Label = "--s: label;" },
        });

        var root = component.Find(".bit-fin");
        var label = component.Find(".bit-fin-lbl");

        Assert.IsTrue(root.ClassList.Contains("bit-fin-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-fin-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-fin-txt"));
        Assert.IsTrue(root.ClassList.Contains("bit-fin-dzn"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "--s: root;");
        Assert.IsTrue(label.ClassList.Contains("cascaded-label"));
        Assert.AreEqual("--s: label;", label.GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-fin-dzi").ClassList.Contains("cascaded-dzi"));
    }

    [TestMethod]
    public void BitFileInputShouldApplyCascadedSelectionRules()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" },
                    new() { Name = "b.pdf", Size = 10, FileId = "2" },
                    new() { Name = "c.txt", Size = 100, FileId = "3" },
                    new() { Name = "d.txt", Size = 10, FileId = "4" }]);

        var component = RenderWithParams(new BitFileInputParams
        {
            Multiple = true,
            AllowedExtensions = [".txt"],
            NotAllowedExtensionErrorMessage = "wrong type",
            MaxSize = 50,
            MaxSizeErrorMessage = "too big",
            MaxCount = 1,
            MaxCountErrorMessage = "too many",
            ShowRemoveButton = true,
            RemoveButtonTitle = "Drop",
            FileListAriaLabel = "Cascaded list",
        });

        Assert.IsTrue(component.Find(".bit-fin-fi").HasAttribute("multiple"));
        Assert.AreEqual(".txt", component.Find(".bit-fin-fi").GetAttribute("accept"));

        component.Find(".bit-fin-fi").Change(string.Empty);

        var files = component.FindComponent<BitFileInput>().Instance.Files;

        Assert.IsTrue(files[0].IsValid);
        Assert.AreEqual("wrong type", files[1].Message);
        Assert.AreEqual("too big", files[2].Message);
        Assert.AreEqual("too many", files[3].Message);
        Assert.AreEqual("Drop", component.Find(".bit-fin-rbt").GetAttribute("title"));
        Assert.AreEqual("Cascaded list", component.Find(".bit-fin-fl").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFileInputShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitFileInputParams
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

        var root = component.Find(".bit-fin");
        var button = component.Find(".bit-fin-lbl");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        Assert.AreEqual("cascaded-id", root.Id);
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
        Assert.IsTrue(button.HasAttribute("disabled"));
        Assert.AreEqual("Cascaded aria label", button.GetAttribute("aria-label"));
        Assert.AreEqual("3", button.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitFileInputShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitFileInputParams
        {
            Color = BitColor.Error,
            Size = BitSize.Large,
            Variant = BitVariant.Text,
            Label = "Cascaded label",
            Multiple = true,
            MaxSize = 100,
            ShowDropZone = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitFileInput.Color), (BitColor?)BitColor.Success);
            builder.AddAttribute(2, nameof(BitFileInput.Size), (BitSize?)BitSize.Small);
            builder.AddAttribute(3, nameof(BitFileInput.Label), "Own label");
            builder.AddAttribute(4, nameof(BitFileInput.Multiple), false);
            builder.AddAttribute(5, nameof(BitFileInput.MaxSize), 5L);
        });

        var root = component.Find(".bit-fin");
        var instance = component.FindComponent<BitFileInput>().Instance;

        Assert.IsTrue(root.ClassList.Contains("bit-fin-suc"));
        Assert.IsFalse(root.ClassList.Contains("bit-fin-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-fin-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-fin-lg"));
        Assert.AreEqual("Own label", component.Find(".bit-fin-lbl").TextContent.Trim());
        // An explicit false is a written parameter like any other, so the cascade does not fill it in.
        Assert.IsFalse(component.Find(".bit-fin-fi").HasAttribute("multiple"));
        Assert.AreEqual(5L, instance.MaxSize);

        // What the component left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-fin-txt"));
        Assert.IsTrue(root.ClassList.Contains("bit-fin-dzn"));
    }

    [TestMethod]
    public void BitFileInputParamsUpdateParametersShouldSetAllProperties()
    {
        static string? announce(IReadOnlyList<BitFileInputInfo> files) => "announced";
        static BitIconInfo? selectIcon(BitFileInputInfo file) => null;
        static string format(long size) => size.ToString();
        static string? validate(BitFileInputInfo file) => null;

        var @params = new BitFileInputParams
        {
            Accept = ".pdf",
            AllowDrop = false,
            AllowDuplicates = false,
            AllowedExtensions = [".pdf"],
            AllowPaste = false,
            AnnouncementProvider = announce,
            Append = true,
            AutoReset = true,
            Capture = "user",
            Classes = new() { Root = "cascaded-root" },
            Color = BitColor.Warning,
            Description = "Cascaded description",
            Directory = true,
            DropZoneIcon = BitIconInfo.Bi("cloud"),
            DropZoneIconName = "CloudUpload",
            DuplicateErrorMessage = "duplicate",
            FileIconSelector = selectIcon,
            FileListAriaLabel = "Cascaded list",
            FileSizeFormatter = format,
            FileValidator = validate,
            HideFileList = true,
            HideLabel = true,
            Label = "Cascaded label",
            MaxCount = 3,
            MaxCountErrorMessage = "too many",
            MaxSize = 100,
            MaxSizeErrorMessage = "too big",
            MaxTotalSize = 200,
            MaxTotalSizeErrorMessage = "total too big",
            MinSize = 10,
            MinSizeErrorMessage = "too small",
            Multiple = true,
            NotAllowedExtensionErrorMessage = "not allowed",
            ReadImageDimensions = true,
            RemoveButtonIcon = BitIconInfo.Bi("trash"),
            RemoveButtonIconName = "Delete",
            RemoveButtonTitle = "Drop",
            ShowDropZone = true,
            ShowPreview = true,
            ShowRemoveButton = true,
            Size = BitSize.Small,
            Styles = new() { Root = "--s: root;" },
            Title = "Cascaded title",
            Variant = BitVariant.Outline,
            ReadOnly = true,
            Required = true,
            AriaLabel = "Cascaded aria label",
            TabIndex = "5",
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitFileInput>().Instance;

        Assert.AreEqual(".pdf", instance.Accept);
        Assert.IsFalse(instance.AllowDrop);
        Assert.IsFalse(instance.AllowDuplicates);
        Assert.AreSame(@params.AllowedExtensions, instance.AllowedExtensions);
        Assert.IsFalse(instance.AllowPaste);
        Assert.AreEqual((Func<IReadOnlyList<BitFileInputInfo>, string?>)announce, instance.AnnouncementProvider);
        Assert.IsTrue(instance.Append);
        Assert.IsTrue(instance.AutoReset);
        Assert.AreEqual("user", instance.Capture);
        Assert.AreSame(@params.Classes, instance.Classes);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual("Cascaded description", instance.Description);
        Assert.IsTrue(instance.Directory);
        Assert.AreEqual(@params.DropZoneIcon, instance.DropZoneIcon);
        Assert.AreEqual("CloudUpload", instance.DropZoneIconName);
        Assert.AreEqual("duplicate", instance.DuplicateErrorMessage);
        Assert.AreEqual((Func<BitFileInputInfo, BitIconInfo?>)selectIcon, instance.FileIconSelector);
        Assert.AreEqual("Cascaded list", instance.FileListAriaLabel);
        Assert.AreEqual((Func<long, string>)format, instance.FileSizeFormatter);
        Assert.AreEqual((Func<BitFileInputInfo, string?>)validate, instance.FileValidator);
        Assert.IsTrue(instance.HideFileList);
        Assert.IsTrue(instance.HideLabel);
        Assert.AreEqual("Cascaded label", instance.Label);
        Assert.AreEqual(3, instance.MaxCount);
        Assert.AreEqual("too many", instance.MaxCountErrorMessage);
        Assert.AreEqual(100L, instance.MaxSize);
        Assert.AreEqual("too big", instance.MaxSizeErrorMessage);
        Assert.AreEqual(200L, instance.MaxTotalSize);
        Assert.AreEqual("total too big", instance.MaxTotalSizeErrorMessage);
        Assert.AreEqual(10L, instance.MinSize);
        Assert.AreEqual("too small", instance.MinSizeErrorMessage);
        Assert.IsTrue(instance.Multiple);
        Assert.AreEqual("not allowed", instance.NotAllowedExtensionErrorMessage);
        Assert.IsTrue(instance.ReadImageDimensions);
        Assert.AreEqual(@params.RemoveButtonIcon, instance.RemoveButtonIcon);
        Assert.AreEqual("Delete", instance.RemoveButtonIconName);
        Assert.AreEqual("Drop", instance.RemoveButtonTitle);
        Assert.IsTrue(instance.ShowDropZone);
        Assert.IsTrue(instance.ShowPreview);
        Assert.IsTrue(instance.ShowRemoveButton);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreSame(@params.Styles, instance.Styles);
        Assert.AreEqual("Cascaded title", instance.Title);
        Assert.AreEqual(BitVariant.Outline, instance.Variant);
        Assert.IsTrue(instance.ReadOnly);
        Assert.IsTrue(instance.Required);
        Assert.AreEqual("Cascaded aria label", instance.AriaLabel);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitFileInputParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitFileInputParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Label = "Params label",
            MaxSize = 100,
            ReadOnly = true,
        };

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Label, "Existing label");
            parameters.Add(p => p.MaxSize, 5);
            parameters.Add(p => p.ReadOnly, false);
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing label", instance.Label);
        Assert.AreEqual(5L, instance.MaxSize);
        Assert.IsFalse(instance.ReadOnly);
    }

    [TestMethod]
    public void BitFileInputParamsUpdateParametersShouldIgnoreANullComponent()
    {
        var @params = new BitFileInputParams { Color = BitColor.Success };

        @params.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitFileInputShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitFileInputParams());

        var root = component.Find(".bit-fin");

        // Nothing cascades, so Color and Size stay unset and publish nothing, and the Variant keeps its default.
        foreach (var cssClass in new[] { "bit-fin-pri", "bit-fin-md", "bit-fin-dzn", "bit-fin-rdl", "bit-fin-nlb" })
        {
            Assert.IsFalse(root.ClassList.Contains(cssClass), $"An empty params object published {cssClass}.");
        }

        Assert.IsTrue(root.ClassList.Contains("bit-fin-fil"));
        Assert.AreEqual("Browse", component.Find(".bit-fin-lbl").TextContent.Trim());
    }

    [TestMethod]
    public void TheHtmlAttributesOfTheInstanceShouldWinOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitFileInputParams
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-both"] = "from-cascade", ["data-cascaded"] = "yes" },
        }, builder =>
        {
            builder.AddAttribute(1, "data-both", "from-instance");
        });

        var root = component.Find(".bit-fin");

        Assert.AreEqual("from-instance", root.GetAttribute("data-both"));
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
    }

    [TestMethod]
    public void TheValuesOfADroppedParamsObjectShouldGo()
    {
        var component = RenderWithParams(new BitFileInputParams { ShowDropZone = true, Label = "Cascaded label", ReadOnly = true });

        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-fin-dzn"));
        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-fin-rdl"));
        Assert.AreEqual("Cascaded label", component.Find(".bit-fin-lbl").TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitFileInputParams { Size = BitSize.Large } }));

        var root = component.Find(".bit-fin");

        Assert.IsFalse(root.ClassList.Contains("bit-fin-dzn"));
        Assert.IsFalse(root.ClassList.Contains("bit-fin-rdl"));
        Assert.IsTrue(root.ClassList.Contains("bit-fin-lg"));
        Assert.AreEqual("Browse", component.Find(".bit-fin-lbl").TextContent.Trim());
    }
}
