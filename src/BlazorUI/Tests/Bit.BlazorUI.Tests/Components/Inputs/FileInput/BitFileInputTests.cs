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
        Assert.AreEqual(0, labels.Count);
    }

    [TestMethod]
    public void BitFileInputShouldSetAriaLabelledByWhenLabelIsVisible()
    {
        var component = RenderComponent<BitFileInput>();

        var fileInput = component.Find(".bit-fin-fi");
        var ariaLabelledBy = fileInput.GetAttribute("aria-labelledby");
        
        Assert.IsNotNull(ariaLabelledBy);
        Assert.IsTrue(ariaLabelledBy.Contains("FileInput-"));
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
        Assert.AreEqual(0, fileList.Count);
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
        Assert.IsTrue(id1.StartsWith("FileInput-"));
        Assert.IsTrue(id2.StartsWith("FileInput-"));
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

        var removeButtons = component.FindAll(".bit-fin-usi");
        
        Assert.AreEqual(0, removeButtons.Count);
    }

    [TestMethod]
    public void BitFileInputPublicPropertiesShouldBeAccessible()
    {
        var component = RenderComponent<BitFileInput>();
        var instance = component.Instance;

        Assert.IsNotNull(instance.Files);
        Assert.AreEqual(0, instance.Files.Count);
        Assert.IsNotNull(instance.InputId);
        Assert.IsTrue(instance.InputId.StartsWith("FileInput-"));
    }

    [TestMethod]
    public void BitFileInputRemoveFileShouldClearAllFilesWhenNullProvided()
    {
        var component = RenderComponent<BitFileInput>();
        var instance = component.Instance;

        // RemoveFile with null should not throw
        instance.RemoveFile(null);
        
        Assert.AreEqual(0, instance.Files.Count);
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
        Assert.AreEqual(0, originalLabels.Count);
    }

    [TestMethod]
    public void BitFileInputShouldRenderEmptyFileListWhenNoFilesSelected()
    {
        var component = RenderComponent<BitFileInput>();

        var fileList = component.FindAll(".bit-fin-fl");
        
        // File list div should be rendered but empty when no files are selected
        Assert.AreEqual(1, fileList.Count);
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
            Assert.IsFalse(style.Contains("visibility:hidden"));
            Assert.IsFalse(style.Contains("display:none"));
        }
        else
        {
            Assert.IsTrue(style.Contains(expectedStyle));
        }
    }
}
