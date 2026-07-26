using System.Threading.Tasks;

using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileInput;

[TestClass]
public class BitFileInputTests : BunitTestContext
{
    [TestMethod]
    public void BitFileInputShouldRenderCorrectly()
    {
        var component = RenderComponent<BitFileInput>();

        var container = component.Find(".bit-fin");
        var fileInput = component.Find(".bit-fin-fi");
        var label = component.Find(".bit-fin-lbl");

        Assert.IsNotNull(container);
        Assert.IsNotNull(fileInput);
        Assert.IsNotNull(label);
        Assert.AreEqual("file", fileInput.GetAttribute("type"));
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileInputShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var fileInput = component.Find(".bit-fin-fi");
        var container = component.Find(".bit-fin");

        Assert.AreEqual(!isEnabled, fileInput.HasAttribute("disabled"));
        
        if (isEnabled)
        {
            Assert.IsFalse(container.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(container.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileInputShouldSetMultipleAttribute(bool isMultiple)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, isMultiple);
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.AreEqual(isMultiple, fileInput.HasAttribute("multiple"));
    }

    [TestMethod,
        DataRow(".jpg,.png"),
        DataRow("image/*"),
        DataRow("application/pdf"),
        DataRow(".doc,.docx,.pdf")
    ]
    public void BitFileInputShouldSetAcceptAttribute(string accept)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Accept, accept);
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.AreEqual(accept, fileInput.GetAttribute("accept"));
    }

    [TestMethod]
    public void BitFileInputShouldNotSetAcceptAttributeByDefault()
    {
        var component = RenderComponent<BitFileInput>();

        var fileInput = component.Find(".bit-fin-fi");

        // the default AllowedExtensions (["*"]) must not produce an accept attribute
        Assert.IsFalse(fileInput.HasAttribute("accept"));
    }

    [TestMethod]
    public void BitFileInputShouldUseAllowedExtensionsWhenAcceptIsNull()
    {
        var extensions = new[] { ".jpg", ".png", ".gif" };

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.AllowedExtensions, extensions);
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.AreEqual(".jpg,.png,.gif", fileInput.GetAttribute("accept"));
    }

    [TestMethod]
    public void BitFileInputShouldNormalizeAllowedExtensionsInTheAcceptAttribute()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            // the leading dot is optional and the entries get trimmed.
            parameters.Add(p => p.AllowedExtensions, new[] { "jpg", " .png ", "gif" });
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.AreEqual(".jpg,.png,.gif", fileInput.GetAttribute("accept"));
    }

    [TestMethod]
    public void BitFileInputShouldKeepMimeTypesOfAllowedExtensionsInTheAcceptAttribute()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.AllowedExtensions, new[] { "image/*", "application/pdf", "txt" });
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.AreEqual("image/*,application/pdf,.txt", fileInput.GetAttribute("accept"));
    }

    [TestMethod,
        DataRow("*"),
        DataRow("*.*"),
        DataRow("*/*")
    ]
    public void BitFileInputShouldNotSetAcceptAttributeForAllowAllExtensions(string wildcard)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.AllowedExtensions, new[] { wildcard });
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.IsFalse(fileInput.HasAttribute("accept"));
    }

    [TestMethod]
    public void BitFileInputShouldNotSetAcceptAttributeForEmptyAllowedExtensions()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.AllowedExtensions, []);
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.IsFalse(fileInput.HasAttribute("accept"));
    }

    [TestMethod]
    public void BitFileInputShouldRenderTheDescriptionAndWireItToTheBrowseButton()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Description, "PDF only, up to 5 MB.");
        });

        var description = component.Find(".bit-fin-dsc");
        var button = component.Find(".bit-fin-lbl");

        Assert.AreEqual("PDF only, up to 5 MB.", description.TextContent.Trim());
        Assert.AreEqual(description.GetAttribute("id"), button.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputShouldPreferTheDescriptionTemplateOverTheDescription()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Description, "plain text");
            parameters.Add(p => p.DescriptionTemplate, builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-description");
                builder.AddContent(2, "templated");
                builder.CloseElement();
            });
        });

        var description = component.Find(".bit-fin-dsc");

        Assert.IsNotNull(description.QuerySelector(".custom-description"));
        Assert.DoesNotContain("plain text", description.TextContent);
    }

    [TestMethod]
    public void BitFileInputShouldNotRenderTheDescriptionByDefault()
    {
        var component = RenderComponent<BitFileInput>();

        var button = component.Find(".bit-fin-lbl");

        Assert.IsEmpty(component.FindAll(".bit-fin-dsc"));
        Assert.IsFalse(button.HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputShouldSetAriaLabelOnTheBrowseButton()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Select a document to attach");
        });

        var button = component.Find(".bit-fin-lbl");

        Assert.AreEqual("Select a document to attach", button.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFileInputShouldRenderAPoliteLiveRegion()
    {
        var component = RenderComponent<BitFileInput>();

        var liveRegion = component.Find(".bit-fin-lvr");

        Assert.AreEqual("status", liveRegion.GetAttribute("role"));
        Assert.AreEqual("polite", liveRegion.GetAttribute("aria-live"));
        Assert.AreEqual("true", liveRegion.GetAttribute("aria-atomic"));
    }

    [TestMethod,
        DataRow("Select File"),
        DataRow("Choose Files"),
        DataRow("Browse")
    ]
    public void BitFileInputShouldDisplayLabel(string label)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var labelButton = component.Find(".bit-fin-lbl");

        Assert.AreEqual(label, labelButton.TextContent);
    }

    [TestMethod]
    public void BitFileInputShouldDisplayDefaultLabelWhenNotProvided()
    {
        var component = RenderComponent<BitFileInput>();

        var labelButton = component.Find(".bit-fin-lbl");

        Assert.AreEqual("Browse", labelButton.TextContent);
    }

    [TestMethod]
    public void BitFileInputShouldHideLabelWhenHideLabelIsTrue()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.HideLabel, true);
        });

        var labels = component.FindAll(".bit-fin-lbl");

        Assert.IsEmpty(labels);
    }

    [TestMethod]
    public void BitFileInputShouldSetAriaLabelledByWhenLabelIsVisible()
    {
        var component = RenderComponent<BitFileInput>();

        var fileInput = component.Find(".bit-fin-fi");
        var ariaLabelledBy = fileInput.GetAttribute("aria-labelledby");
        
        Assert.IsNotNull(ariaLabelledBy);
        Assert.Contains("FileInput-", ariaLabelledBy);
    }

    [TestMethod]
    public void BitFileInputShouldNotSetAriaLabelledByWhenLabelIsHidden()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.HideLabel, true);
        });

        var fileInput = component.Find(".bit-fin-fi");
        
        Assert.IsFalse(fileInput.HasAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitFileInputShouldHideFileListWhenHideFileListIsTrue()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.HideFileList, true);
        });

        var fileList = component.FindAll(".bit-fin-fl");
        
        Assert.IsEmpty(fileList);
    }

    [TestMethod]
    public void BitFileInputShouldGenerateUniqueInputId()
    {
        var component1 = RenderComponent<BitFileInput>();
        var component2 = RenderComponent<BitFileInput>();

        var input1 = component1.Find(".bit-fin-fi");
        var input2 = component2.Find(".bit-fin-fi");

        var id1 = input1.GetAttribute("id");
        var id2 = input2.GetAttribute("id");

        Assert.IsNotNull(id1);
        Assert.IsNotNull(id2);
        Assert.AreNotEqual(id1, id2);
        Assert.StartsWith("FileInput-", id1);
        Assert.StartsWith("FileInput-", id2);
    }

    [TestMethod]
    public void BitFileInputShouldHaveCorrectRootElementClass()
    {
        var component = RenderComponent<BitFileInput>();

        var container = component.Find(".bit-fin");
        
        Assert.IsNotNull(container);
        Assert.IsTrue(container.ClassList.Contains("bit-fin"));
    }

    [TestMethod,
        DataRow(BitDir.Ltr),
        DataRow(BitDir.Rtl),
        DataRow(BitDir.Auto)
    ]
    public void BitFileInputShouldRespectDir(BitDir dir)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var container = component.Find(".bit-fin");
        var dirAttribute = container.GetAttribute("dir");
        
        Assert.AreEqual(dir.ToString().ToLower(), dirAttribute);
    }

    [TestMethod]
    public void BitFileInputShouldApplyCustomClass()
    {
        var customClass = "custom-class";

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Class, customClass);
        });

        var container = component.Find(".bit-fin");
        
        Assert.IsTrue(container.ClassList.Contains(customClass));
    }

    [TestMethod]
    public void BitFileInputShouldApplyCustomStyle()
    {
        var customStyle = "color: red;";

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Style, customStyle);
        });

        var container = component.Find(".bit-fin");
        
        Assert.IsTrue(container.GetAttribute("style")?.Contains("color: red") ?? false);
    }

    [TestMethod]
    public void BitFileInputShouldApplyCustomId()
    {
        var customId = "custom-file-input";

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Id, customId);
        });

        var container = component.Find($"#{customId}");
        
        Assert.IsNotNull(container);
    }

    [TestMethod]
    public void BitFileInputShouldNotShowRemoveButtonByDefault()
    {
        var component = RenderComponent<BitFileInput>();

        var removeButtons = component.FindAll(".bit-fin-rbt");
        
        Assert.IsEmpty(removeButtons);
    }

    [Ignore]
    [TestMethod]
    public void BitFileInputShouldUseDefaultDeleteIconForRemoveButton()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
        });

        var removeButton = component.Find(".bit-fin-rbt");
        var icon = removeButton.QuerySelector("i");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Delete"));
    }

    [Ignore]
    [TestMethod]
    public void BitFileInputShouldRespectRemoveButtonIconWithBitIconInfo()
    {
        var iconInfo = new BitIconInfo("fa-solid fa-trash", baseClass: "", prefix: "");

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
            parameters.Add(p => p.RemoveButtonIcon, iconInfo);
        });

        var removeButton = component.Find(".bit-fin-rbt");
        var icon = removeButton.QuerySelector("i");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-trash"));
    }

    [Ignore]
    [TestMethod,
        DataRow("Trash"),
        DataRow("Delete"),
        DataRow("Cancel")
    ]
    public void BitFileInputShouldRespectRemoveButtonIconName(string iconName)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
            parameters.Add(p => p.RemoveButtonIconName, iconName);
        });

        var removeButton = component.Find(".bit-fin-rbt");
        var icon = removeButton.QuerySelector("i");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [Ignore]
    [TestMethod]
    public void BitFileInputRemoveButtonIconShouldTakePrecedenceOverIconName()
    {
        var iconInfo = new BitIconInfo("fa-solid fa-trash", baseClass: "", prefix: "");

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
            parameters.Add(p => p.RemoveButtonIcon, iconInfo);
            parameters.Add(p => p.RemoveButtonIconName, "Delete");
        });

        var removeButton = component.Find(".bit-fin-rbt");
        var icon = removeButton.QuerySelector("i");

        Assert.IsNotNull(icon);
        // Icon (BitIconInfo) takes precedence over IconName
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-trash"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Delete"));
    }

    [TestMethod]
    public void BitFileInputPublicPropertiesShouldBeAccessible()
    {
        var component = RenderComponent<BitFileInput>();
        var instance = component.Instance;

        Assert.IsNotNull(instance.Files);
        Assert.IsEmpty(instance.Files);
        Assert.IsNotNull(instance.InputId);
        Assert.StartsWith("FileInput-", instance.InputId);
    }

    [TestMethod]
    public async Task BitFileInputRemoveFileShouldClearAllFilesWhenNullProvided()
    {
        var component = RenderComponent<BitFileInput>();

        var instance = component.Instance;

        // RemoveFile with null should not throw
        await instance.RemoveFile(null);

        Assert.IsEmpty(instance.Files);
    }

    [TestMethod]
    public void BitFileInputShouldRenderLabelTemplateWhenProvided()
    {
        var templateText = "Custom Label Template";

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-label");
                builder.AddContent(2, templateText);
                builder.CloseElement();
            });
        });

        var customLabel = component.Find(".custom-label");

        Assert.AreEqual(templateText, customLabel.TextContent);
        
        // Original label should not be rendered
        var originalLabels = component.FindAll(".bit-fin-lbl");

        Assert.IsEmpty(originalLabels);
    }

    [TestMethod]
    public void BitFileInputShouldRenderEmptyFileListWhenNoFilesSelected()
    {
        var component = RenderComponent<BitFileInput>();

        var fileList = component.FindAll(".bit-fin-fl");
        
        // File list div should be rendered but empty when no files are selected
        Assert.HasCount(1, fileList);
        Assert.AreEqual(0, fileList[0].ChildElementCount);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitFileInputShouldHandleMultipleParameter(bool multiple)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, multiple);
        });

        var fileInput = component.Find(".bit-fin-fi");
        
        Assert.AreEqual(multiple, fileInput.HasAttribute("multiple"));
    }

    [TestMethod]
    public void BitFileInputShouldHaveCorrectFileInputType()
    {
        var component = RenderComponent<BitFileInput>();

        var fileInput = component.Find("input[type='file']");
        
        Assert.IsNotNull(fileInput);
        Assert.IsTrue(fileInput.ClassList.Contains("bit-fin-fi"));
    }

    [TestMethod]
    public void BitFileInputLabelButtonShouldHaveCorrectType()
    {
        var component = RenderComponent<BitFileInput>();

        var labelButton = component.Find("button.bit-fin-lbl");
        
        Assert.IsNotNull(labelButton);
        Assert.AreEqual("button", labelButton.GetAttribute("type"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitFileInputShouldSetWebkitDirectoryAttribute(bool directory)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Directory, directory);
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.AreEqual(directory, fileInput.HasAttribute("webkitdirectory"));
    }

    [TestMethod,
        DataRow("user"),
        DataRow("environment")
    ]
    public void BitFileInputShouldSetCaptureAttribute(string capture)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Capture, capture);
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.AreEqual(capture, fileInput.GetAttribute("capture"));
    }

    [TestMethod]
    public void BitFileInputShouldNotSetCaptureAttributeByDefault()
    {
        var component = RenderComponent<BitFileInput>();

        var fileInput = component.Find(".bit-fin-fi");

        Assert.IsFalse(fileInput.HasAttribute("capture"));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-fin-pri"),
        DataRow(BitColor.Secondary, "bit-fin-sec"),
        DataRow(BitColor.Tertiary, "bit-fin-ter"),
        DataRow(BitColor.Info, "bit-fin-inf"),
        DataRow(BitColor.Success, "bit-fin-suc"),
        DataRow(BitColor.Warning, "bit-fin-wrn"),
        DataRow(BitColor.SevereWarning, "bit-fin-swr"),
        DataRow(BitColor.Error, "bit-fin-err"),
        DataRow(BitColor.PrimaryBackground, "bit-fin-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-fin-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-fin-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-fin-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-fin-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-fin-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-fin-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-fin-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-fin-tbr"),
        DataRow(null, "bit-fin-pri")
    ]
    public void BitFileInputShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            if (color.HasValue)
            {
                parameters.Add(p => p.Color, color.Value);
            }
        });

        var container = component.Find(".bit-fin");

        Assert.IsTrue(container.ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-fin-sm"),
        DataRow(BitSize.Medium, "bit-fin-md"),
        DataRow(BitSize.Large, "bit-fin-lg"),
        DataRow(null, "bit-fin-md")
    ]
    public void BitFileInputShouldRespectSize(BitSize? size, string expectedClass)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            if (size.HasValue)
            {
                parameters.Add(p => p.Size, size.Value);
            }
        });

        var container = component.Find(".bit-fin");

        Assert.IsTrue(container.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitFileInputShouldRespectClassStyles()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitFileInputClassStyles { Root = "custom-root", Label = "custom-label" });
            parameters.Add(p => p.Styles, new BitFileInputClassStyles { Root = "margin: 1px;", Label = "padding: 1px;" });
        });

        var container = component.Find(".bit-fin");
        var label = component.Find(".bit-fin-lbl");

        Assert.IsTrue(container.ClassList.Contains("custom-root"));
        Assert.Contains("margin: 1px", container.GetAttribute("style") ?? string.Empty);
        Assert.IsTrue(label.ClassList.Contains("custom-label"));
        Assert.Contains("padding: 1px", label.GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void BitFileInputFileListShouldHaveListRole()
    {
        var component = RenderComponent<BitFileInput>();

        var fileList = component.Find(".bit-fin-fl");

        Assert.AreEqual("list", fileList.GetAttribute("role"));
    }

    [TestMethod]
    public void BitFileInputShouldSetAriaLabelOnInput()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Select a document");
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.AreEqual("Select a document", fileInput.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFileInputShouldNotSetAriaLabelledByWhenLabelTemplateIsProvided()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, "Custom label");
                builder.CloseElement();
            });
        });

        var fileInput = component.Find(".bit-fin-fi");

        Assert.IsFalse(fileInput.HasAttribute("aria-labelledby"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, ""),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitFileInputShouldApplyVisibilityStyle(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var container = component.Find(".bit-fin");
        var style = container.GetAttribute("style") ?? string.Empty;
        
        if (string.IsNullOrEmpty(expectedStyle))
        {
            Assert.DoesNotContain("visibility:hidden", style);
            Assert.DoesNotContain("display:none", style);
        }
        else
        {
            Assert.Contains(expectedStyle, style);
        }
    }
}
