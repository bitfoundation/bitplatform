using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
    public void BitFileInputShouldRespectDisabled(bool isEnabled)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
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
    public async Task BitFileInputShouldRenderAPoliteLiveRegion()
    {
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" },
                    new() { Name = "file2.txt", Size = 100, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxSize, 50);
        });

        var liveRegion = component.Find(".bit-fin-lvr");

        Assert.AreEqual("status", liveRegion.GetAttribute("role"));
        Assert.AreEqual("polite", liveRegion.GetAttribute("aria-live"));
        Assert.AreEqual("true", liveRegion.GetAttribute("aria-atomic"));
        Assert.AreEqual(string.Empty, liveRegion.TextContent);

        component.Find(".bit-fin-fi").Change(string.Empty);

        // the announcement carries the count of the files and of the invalid ones among them, and every other
        // announcement ends with a zero width space so that a repeated text still counts as an update.
        Assert.AreEqual("2 files selected. 1 of them invalid.\u200B", component.Find(".bit-fin-lvr").TextContent);

        var instance = component.Instance;

        await component.InvokeAsync(() => instance.RemoveFile(instance.Files[1]));

        Assert.AreEqual("1 file selected.", component.Find(".bit-fin-lvr").TextContent);

        await component.InvokeAsync(() => instance.RemoveFile(null));

        Assert.AreEqual("No file selected.\u200B", component.Find(".bit-fin-lvr").TextContent);
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
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>();

        // without a file in the list there would be no remove button to begin with,
        // so the assertion would hold no matter what ShowRemoveButton defaults to.
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.HasCount(1, component.FindAll(".bit-fin-itm"));
        Assert.IsEmpty(component.FindAll(".bit-fin-rbt"));
    }

    [TestMethod]
    public void BitFileInputShouldUseDefaultDeleteIconForRemoveButton()
    {
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
        });

        // the remove buttons live inside the file items, so a file has to be selected first.
        component.Find(".bit-fin-fi").Change(string.Empty);

        var removeButton = component.Find(".bit-fin-rbt");
        var icon = removeButton.QuerySelector("i");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Delete"));
    }

    [TestMethod]
    public void BitFileInputShouldRespectRemoveButtonIconWithBitIconInfo()
    {
        var iconInfo = new BitIconInfo("fa-solid fa-trash", baseClass: "", prefix: "");

        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
            parameters.Add(p => p.RemoveButtonIcon, iconInfo);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var removeButton = component.Find(".bit-fin-rbt");
        var icon = removeButton.QuerySelector("i");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-trash"));
    }

    [TestMethod,
        DataRow("Trash"),
        DataRow("Delete"),
        DataRow("Cancel")
    ]
    public void BitFileInputShouldRespectRemoveButtonIconName(string iconName)
    {
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
            parameters.Add(p => p.RemoveButtonIconName, iconName);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var removeButton = component.Find(".bit-fin-rbt");
        var icon = removeButton.QuerySelector("i");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod]
    public void BitFileInputRemoveButtonIconShouldTakePrecedenceOverIconName()
    {
        var iconInfo = new BitIconInfo("fa-solid fa-trash", baseClass: "", prefix: "");

        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
            parameters.Add(p => p.RemoveButtonIcon, iconInfo);
            parameters.Add(p => p.RemoveButtonIconName, "Delete");
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

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
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" },
                    new() { Name = "file2.txt", Size = 20, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var instance = component.Instance;

        Assert.HasCount(2, instance.Files);

        await component.InvokeAsync(() => instance.RemoveFile(null));

        Assert.IsEmpty(instance.Files);
    }

    [TestMethod]
    public void BitFileInputShouldRespectMaxCount()
    {
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" },
                    new() { Name = "file2.txt", Size = 20, FileId = "2" },
                    new() { Name = "file3.txt", Size = 30, FileId = "3" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxCount, 2);
            parameters.Add(p => p.MaxCountErrorMessage, "too many");
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var files = component.Instance.Files;

        Assert.IsTrue(files[0].IsValid);
        Assert.IsTrue(files[1].IsValid);
        Assert.IsFalse(files[2].IsValid);
        Assert.AreEqual("too many", files[2].Message);
    }

    [TestMethod]
    public void BitFileInputShouldRespectMaxTotalSize()
    {
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" },
                    new() { Name = "file2.txt", Size = 20, FileId = "2" },
                    new() { Name = "file3.txt", Size = 30, FileId = "3" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxTotalSize, 30);
            parameters.Add(p => p.MaxTotalSizeErrorMessage, "too big");
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var files = component.Instance.Files;

        Assert.IsTrue(files[0].IsValid);
        Assert.IsTrue(files[1].IsValid);
        Assert.IsFalse(files[2].IsValid);
        Assert.AreEqual("too big", files[2].Message);
    }

    [TestMethod]
    public void BitFileInputShouldRespectMinSize()
    {
        SetupFiles([new() { Name = "small.txt", Size = 5, FileId = "1" },
                    new() { Name = "big.txt", Size = 50, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MinSize, 10);
            parameters.Add(p => p.MinSizeErrorMessage, "too small");
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var files = component.Instance.Files;

        Assert.IsFalse(files[0].IsValid);
        Assert.AreEqual("too small", files[0].Message);
        Assert.IsTrue(files[1].IsValid);
    }

    [TestMethod]
    public void BitFileInputShouldDescribeTheRemoveButtonOfAnInvalidFileWithItsMessage()
    {
        SetupFiles([new() { Name = "small.txt", Size = 5, FileId = "1" },
                    new() { Name = "big.txt", Size = 50, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MinSize, 10);
            parameters.Add(p => p.MinSizeErrorMessage, "too small");
            parameters.Add(p => p.ShowRemoveButton, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var removeButtons = component.FindAll(".bit-fin-rbt");
        var describedBy = removeButtons[0].GetAttribute("aria-describedby");

        Assert.IsFalse(string.IsNullOrEmpty(describedBy));
        Assert.AreEqual("too small", component.Find($"[id='{describedBy}']").TextContent.Trim());
        Assert.IsFalse(removeButtons[1].HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputShouldRespectAllowDuplicatesWithDuplicateErrorMessage()
    {
        SetupFiles([new() { Name = "file.txt", Size = 10, LastModified = 1, FileId = "1" },
                    new() { Name = "file.txt", Size = 10, LastModified = 1, FileId = "2" },
                    new() { Name = "other.txt", Size = 10, LastModified = 1, FileId = "3" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.AllowDuplicates, false);
            parameters.Add(p => p.DuplicateErrorMessage, "already there");
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var files = component.Instance.Files;

        Assert.IsTrue(files[0].IsValid);
        Assert.IsFalse(files[1].IsValid);
        Assert.AreEqual("already there", files[1].Message);
        Assert.IsTrue(files[2].IsValid);
    }

    [TestMethod]
    public async Task BitFileInputShouldRevalidateDuplicatesAfterRemovingTheOriginal()
    {
        SetupFiles([new() { Name = "file.txt", Size = 10, LastModified = 1, FileId = "1" },
                    new() { Name = "file.txt", Size = 10, LastModified = 1, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.AllowDuplicates, false);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var instance = component.Instance;

        Assert.IsFalse(instance.Files[1].IsValid);

        // removing the original leaves a single copy behind, which is no longer a duplicate of anything.
        await component.InvokeAsync(() => instance.RemoveFile(instance.Files[0]));

        Assert.HasCount(1, instance.Files);
        Assert.IsTrue(instance.Files[0].IsValid);
        Assert.IsNull(instance.Files[0].Message);
    }

    [TestMethod]
    public async Task BitFileInputShouldKeepTheOwnValidationMessageOfADuplicate()
    {
        SetupFiles([new() { Name = "big.txt", Size = 100, LastModified = 1, FileId = "1" },
                    new() { Name = "big.txt", Size = 100, LastModified = 1, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.AllowDuplicates, false);
            parameters.Add(p => p.MaxSize, 50);
            parameters.Add(p => p.MaxSizeErrorMessage, "too big");
            parameters.Add(p => p.DuplicateErrorMessage, "already there");
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var instance = component.Instance;

        // a file failing a validation of its own keeps that message instead of the duplicate one,
        // so that removing the original does not turn an oversized file into a valid one.
        Assert.AreEqual("too big", instance.Files[0].Message);
        Assert.AreEqual("too big", instance.Files[1].Message);

        await component.InvokeAsync(() => instance.RemoveFile(instance.Files[0]));

        Assert.IsFalse(instance.Files[0].IsValid);
        Assert.AreEqual("too big", instance.Files[0].Message);
    }

    [TestMethod]
    public void BitFileInputShouldRespectFileValidator()
    {
        SetupFiles([new() { Name = "ok.txt", Size = 10, FileId = "1" },
                    new() { Name = "bad.txt", Size = 10, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.FileValidator, file => file.Name.StartsWith("bad") ? "rejected" : null);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var files = component.Instance.Files;

        Assert.IsTrue(files[0].IsValid);
        Assert.IsFalse(files[1].IsValid);
        Assert.AreEqual("rejected", files[1].Message);
    }

    [TestMethod]
    public void BitFileInputShouldInvokeOnInvalidWithOnlyInvalidFiles()
    {
        SetupFiles([new() { Name = "ok.txt", Size = 10, FileId = "1" },
                    new() { Name = "huge.txt", Size = 100, FileId = "2" }]);

        BitFileInputInfo[]? invalidFiles = null;

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxSize, 50);
            parameters.Add(p => p.OnInvalid, files => invalidFiles = files);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.IsNotNull(invalidFiles);
        Assert.HasCount(1, invalidFiles);
        Assert.AreEqual("huge.txt", invalidFiles[0].Name);
    }

    [TestMethod]
    public async Task BitFileInputShouldInvokeOnRemoveForEachRemovedFile()
    {
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" },
                    new() { Name = "file2.txt", Size = 20, FileId = "2" }]);

        List<string> removedNames = [];

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.OnRemove, file => removedNames.Add(file.Name));
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var instance = component.Instance;

        await component.InvokeAsync(() => instance.RemoveFile(instance.Files[0]));

        Assert.AreEqual("file1.txt", string.Join(",", removedNames));

        await component.InvokeAsync(() => instance.RemoveFile(null));

        Assert.AreEqual("file1.txt,file2.txt", string.Join(",", removedNames));
    }

    [TestMethod]
    public async Task BitFileInputShouldRecomputeValidationStateAfterRemoval()
    {
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" },
                    new() { Name = "file2.txt", Size = 20, FileId = "2" },
                    new() { Name = "file3.txt", Size = 30, FileId = "3" }]);

        BitFileInputInfo[]? invalidFiles = null;

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxCount, 2);
            parameters.Add(p => p.OnInvalid, files => invalidFiles = files);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var instance = component.Instance;

        Assert.IsNotNull(invalidFiles);
        Assert.HasCount(1, invalidFiles);
        Assert.AreEqual("file3.txt", invalidFiles[0].Name);

        invalidFiles = null;

        // removing a file frees up room, so the file invalidated by the count limit becomes valid again.
        await component.InvokeAsync(() => instance.RemoveFile(instance.Files[0]));

        Assert.HasCount(2, instance.Files);
        Assert.IsTrue(instance.Files.All(f => f.IsValid));
        Assert.AreEqual(0, instance.Files[0].Index);
        Assert.AreEqual(1, instance.Files[1].Index);
        Assert.IsNull(invalidFiles);
    }

    [TestMethod]
    public async Task BitFileInputRemoveFileShouldRespectDisabled()
    {
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" },
                    new() { Name = "file2.txt", Size = 20, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var instance = component.Instance;

        Assert.HasCount(2, instance.Files);

        component.Render(parameters => parameters.Add(p => p.Disabled, true));

        await component.InvokeAsync(() => instance.RemoveFile(instance.Files[0]));
        await component.InvokeAsync(() => instance.RemoveFile(null));

        Assert.HasCount(2, instance.Files);
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
        DataRow(BitColor.TertiaryBorder, "bit-fin-tbr")
    ]
    public void BitFileInputShouldRespectColor(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        var container = component.Find(".bit-fin");

        Assert.IsTrue(container.ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitVariant.Fill, "bit-fin-fil"),
        DataRow(BitVariant.Outline, "bit-fin-otl"),
        DataRow(BitVariant.Text, "bit-fin-txt"),
        DataRow(null, "bit-fin-fil")
    ]
    public void BitFileInputShouldRespectVariant(BitVariant? variant, string expectedClass)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            if (variant.HasValue)
            {
                parameters.Add(p => p.Variant, variant.Value);
            }
        });

        var container = component.Find(".bit-fin");

        Assert.IsTrue(container.ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-fin-sm"),
        DataRow(BitSize.Medium, "bit-fin-md"),
        DataRow(BitSize.Large, "bit-fin-lg")
    ]
    public void BitFileInputShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var container = component.Find(".bit-fin");

        Assert.IsTrue(container.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitFileInputShouldPublishNoColorOrSizeWhileTheyAreUnset()
    {
        var component = RenderComponent<BitFileInput>();

        // An unset Color or Size publishes nothing, so the public --bit-FileInput-* variables restyle the default
        // while an explicit value - which does publish its class - wins over them.
        var container = component.Find(".bit-fin");
        var published = new[]
        {
            "bit-fin-pri", "bit-fin-sec", "bit-fin-ter", "bit-fin-inf", "bit-fin-suc", "bit-fin-wrn", "bit-fin-swr", "bit-fin-err",
            "bit-fin-pbg", "bit-fin-sbg", "bit-fin-tbg", "bit-fin-pfg", "bit-fin-sfg", "bit-fin-tfg", "bit-fin-pbr", "bit-fin-sbr", "bit-fin-tbr",
            "bit-fin-sm", "bit-fin-md", "bit-fin-lg",
        };

        foreach (var cssClass in published)
        {
            Assert.IsFalse(container.ClassList.Contains(cssClass), $"An unset parameter published {cssClass}.");
        }
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

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileInputShouldDisableTheBrowseButtonAlongWithTheInput(bool isEnabled)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
        });

        var button = component.Find(".bit-fin-lbl");

        // the browse button is what the keyboard reaches, so the disabled state has to sit on it too and not
        // only on the hidden input behind it.
        Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitFileInputShouldSetTitleOnTheBrowseButton()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Title, "Pick a file");
        });

        Assert.AreEqual("Pick a file", component.Find(".bit-fin-lbl").GetAttribute("title"));
    }

    [TestMethod]
    public void BitFileInputShouldSetTabIndexOnTheBrowseButton()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("3", component.Find(".bit-fin-lbl").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitFileInputShouldDisableTheRemoveButtonWhenDisabled()
    {
        SetupFiles([new() { Name = "file.txt", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.IsFalse(component.Find(".bit-fin-rbt").HasAttribute("disabled"));

        component.Render(parameters => parameters.Add(p => p.Disabled, true));

        Assert.IsTrue(component.Find(".bit-fin-rbt").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitFileInputShouldMarkAnInvalidFileItem()
    {
        SetupFiles([new() { Name = "small.txt", Size = 5, FileId = "1" },
                    new() { Name = "big.txt", Size = 50, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxSize, 10);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var items = component.FindAll(".bit-fin-itm");

        Assert.IsTrue(items[0].ClassList.Contains("bit-fin-vld"));
        Assert.IsTrue(items[1].ClassList.Contains("bit-fin-inv"));
        Assert.IsNotNull(component.Find(".bit-fin-inv .bit-fin-msg"));
    }

    [TestMethod]
    public void BitFileInputShouldRenderTheFolderOfAFileOfAFolderSelection()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1", RelativePath = "docs/notes/a.txt" },
                    new() { Name = "b.txt", Size = 10, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Directory, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var folders = component.FindAll(".bit-fin-fp");

        // only the file that reported a path gets one, so an ordinary selection keeps the row it always had.
        Assert.HasCount(1, folders);
        Assert.AreEqual("docs/notes", folders[0].TextContent);
    }

    [TestMethod]
    public void BitFileInputShouldTellApartSameNamedFilesOfDifferentFolders()
    {
        SetupFiles([new() { Name = "readme.md", Size = 10, LastModified = 1, FileId = "1", RelativePath = "pkg/one/readme.md" },
                    new() { Name = "readme.md", Size = 10, LastModified = 1, FileId = "2", RelativePath = "pkg/two/readme.md" },
                    new() { Name = "readme.md", Size = 10, LastModified = 1, FileId = "3", RelativePath = "pkg/two/readme.md" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Directory, true);
            parameters.Add(p => p.AllowDuplicates, false);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var files = component.Instance.Files;

        Assert.IsTrue(files[0].IsValid);
        Assert.IsTrue(files[1].IsValid);
        Assert.IsFalse(files[2].IsValid);
    }

    [TestMethod]
    public void BitFileInputShouldRenderAFileTypeIconWhereThereIsNoPreview()
    {
        SetupFiles([new() { Name = "report.docx", Size = 10, FileId = "1" },
                    new() { Name = "archive.zip", Size = 10, FileId = "2" },
                    new() { Name = "clip.mp4", Size = 10, FileId = "3", ContentType = "video/mp4" },
                    new() { Name = "mystery", Size = 10, FileId = "4" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ShowPreview, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var icons = component.FindAll(".bit-fin-fio i");

        Assert.HasCount(4, icons);
        Assert.IsTrue(icons[0].ClassList.Contains("bit-icon--WordDocument"));
        Assert.IsTrue(icons[1].ClassList.Contains("bit-icon--ZipFolder"));
        Assert.IsTrue(icons[2].ClassList.Contains("bit-icon--Video"));
        Assert.IsTrue(icons[3].ClassList.Contains("bit-icon--Page"));
    }

    [TestMethod]
    public void BitFileInputShouldNotRenderFileTypeIconsWithoutShowPreview()
    {
        SetupFiles([new() { Name = "report.docx", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>();

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.IsEmpty(component.FindAll(".bit-fin-fio"));
    }

    [TestMethod]
    public void BitFileInputShouldRespectFileIconSelector()
    {
        SetupFiles([new() { Name = "report.docx", Size = 10, FileId = "1" },
                    new() { Name = "archive.zip", Size = 10, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ShowPreview, true);
            parameters.Add(p => p.FileIconSelector,
                           file => file.Name.EndsWith(".zip") ? null : BitIconInfo.Bit("Attach"));
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var icons = component.FindAll(".bit-fin-fio i");

        // a null from the selector leaves that file without a glyph at all.
        Assert.HasCount(1, icons);
        Assert.IsTrue(icons[0].ClassList.Contains("bit-icon--Attach"));
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileInputShouldMoveTheDropIndicatorToTheRootWhenTheLabelIsHidden(bool hideLabel)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.HideLabel, hideLabel);
        });

        Assert.AreEqual(hideLabel, component.Find(".bit-fin").ClassList.Contains("bit-fin-nlb"));

        // the class builder caches, so the toggle has to survive a re-render with the other value too.
        component.Render(parameters => parameters.Add(p => p.HideLabel, !hideLabel));

        Assert.AreEqual(!hideLabel, component.Find(".bit-fin").ClassList.Contains("bit-fin-nlb"));
    }

    [TestMethod]
    public void BitFileInputShouldMoveTheDropIndicatorToTheRootWhenTheLabelIsTemplated()
    {
        var component = RenderComponent<BitFileInput>();

        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-fin-nlb"));

        // a LabelTemplate replaces the browse button, which is what would have carried the indicator.
        component.Render(parameters => parameters.Add(p => p.LabelTemplate, builder => builder.AddContent(0, "pick")));

        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-fin-nlb"));

        component.Render(parameters => parameters.Add(p => p.LabelTemplate, (Microsoft.AspNetCore.Components.RenderFragment?)null));

        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-fin-nlb"));
    }

    [TestMethod]
    public void BitFileInputShouldNameTheWholePathInTheRemoveButtonLabelOfAFolderSelection()
    {
        SetupFiles([new() { Name = "readme.md", Size = 10, FileId = "1", RelativePath = "pkg/one/readme.md" },
                    new() { Name = "plain.md", Size = 10, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Directory, true);
            parameters.Add(p => p.ShowRemoveButton, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var buttons = component.FindAll(".bit-fin-rbt");

        Assert.AreEqual("Remove pkg/one/readme.md", buttons[0].GetAttribute("aria-label"));
        Assert.AreEqual("Remove plain.md", buttons[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFileInputShouldRenderTheFileNameWithoutItsExtensionApart()
    {
        SetupFiles([new() { Name = "a-very-long-quarterly-report-final.pdf", Size = 10, FileId = "1" },
                    new() { Name = "no-extension", Size = 10, FileId = "2" },
                    new() { Name = ".gitignore", Size = 10, FileId = "3" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        var names = component.FindAll(".bit-fin-fn");
        var stems = component.FindAll(".bit-fin-fnm");
        var extensions = component.FindAll(".bit-fin-fnx");

        // the two boxes still read as the whole name, which is what the ellipsis is allowed to eat into.
        Assert.AreEqual("a-very-long-quarterly-report-final.pdf", names[0].TextContent);
        Assert.AreEqual("a-very-long-quarterly-report-final", stems[0].TextContent);
        Assert.AreEqual(".pdf", extensions[0].TextContent);

        Assert.AreEqual("no-extension", stems[1].TextContent);
        Assert.AreEqual(string.Empty, extensions[1].TextContent);

        // a dotfile is all stem, so the whole name stays in the box the ellipsis is allowed to eat into.
        Assert.AreEqual(".gitignore", stems[2].TextContent);
        Assert.AreEqual(string.Empty, extensions[2].TextContent);
    }

    [TestMethod]
    public async Task BitFileInputShouldMoveTheFocusAfterRemovingAFile()
    {
        SetupFiles([new() { Name = "file1.txt", Size = 10, FileId = "1" },
                    new() { Name = "file2.txt", Size = 20, FileId = "2" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ShowRemoveButton, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.HasCount(2, component.FindAll(".bit-fin-rbt"));

        await component.Find(".bit-fin-rbt").ClickAsync(new());

        // the pressed button is gone with its file, so the focus has to be handed on rather than dropped.
        Context.JSInterop.VerifyFocusAsyncInvoke();

        Assert.HasCount(1, component.FindAll(".bit-fin-rbt"));
        Assert.HasCount(1, component.Instance.Files);
        Assert.AreEqual("file2.txt", component.Instance.Files[0].Name);
    }

    [TestMethod]
    public async Task BitFileInputOpenReadStreamShouldRejectANullFile()
    {
        var component = RenderComponent<BitFileInput>();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => component.Instance.OpenReadStreamAsync(null!));
    }

    [TestMethod]
    public void BitFileInputParamsShouldHaveCorrectParamName()
    {
        var paramName = BitFileInputParams.ParamName;
        var expectedName = $"{nameof(BitParams)}.{nameof(BitFileInput)}";

        Assert.AreEqual(expectedName, paramName);
    }

    [TestMethod]
    public void BitFileInputParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitFileInputParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitFileInputParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitFileInputShouldApplyCascadingParametersFromBitParams()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitFileInputParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                Variant = BitVariant.Outline,
                Label = "Cascaded Label",
                Title = "Cascaded Title",
                Multiple = true,
                Description = "Cascaded Description"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFileInput>(0);
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-fin");
        var button = component.Find(".bit-fin-lbl");

        Assert.IsTrue(root.ClassList.Contains("bit-fin-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-fin-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-fin-otl"));
        Assert.AreEqual("Cascaded Label", button.TextContent.Trim());
        Assert.AreEqual("Cascaded Title", button.GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-fin-fi").HasAttribute("multiple"));
        Assert.AreEqual("Cascaded Description", component.Find(".bit-fin-dsc").TextContent.Trim());
    }

    [TestMethod]
    public void BitFileInputDirectParametersShouldOverrideCascadingParameters()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitFileInputParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                Label = "Cascaded Label",
                MaxSize = 100
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFileInput>(0);
                builder.AddAttribute(1, nameof(BitFileInput.Color), BitColor.Error);
                builder.AddAttribute(2, nameof(BitFileInput.Label), "Own Label");
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-fin");

        Assert.IsTrue(root.ClassList.Contains("bit-fin-err"));
        // what the component left unset still comes from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-fin-lg"));
        Assert.AreEqual("Own Label", component.Find(".bit-fin-lbl").TextContent.Trim());
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileInputShouldRenderTheDropZonePanel(bool showDropZone)
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowDropZone, showDropZone);
        });

        Assert.AreEqual(showDropZone, component.Find(".bit-fin").ClassList.Contains("bit-fin-dzn"));
        Assert.AreEqual(showDropZone, component.FindAll(".bit-fin-dzi").Count == 1);

        // the class builder caches, so the toggle has to survive a re-render with the other value too.
        component.Render(parameters => parameters.Add(p => p.ShowDropZone, !showDropZone));

        Assert.AreEqual(!showDropZone, component.Find(".bit-fin").ClassList.Contains("bit-fin-dzn"));
    }

    [TestMethod]
    public void BitFileInputDropZoneShouldDefaultToTheOutlineVariant()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowDropZone, true);
        });

        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-fin-otl"));

        // an explicit variant still has the last word over that default.
        component.Render(parameters => parameters.Add(p => p.Variant, BitVariant.Fill));

        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-fin-fil"));
    }

    [TestMethod]
    public void BitFileInputShouldRenderTheDefaultDropZoneIcon()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowDropZone, true);
        });

        Assert.IsTrue(component.Find(".bit-fin-dzi").ClassList.Contains("bit-icon--CloudUpload"));
    }

    [TestMethod]
    public void BitFileInputShouldRespectDropZoneIconName()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowDropZone, true);
            parameters.Add(p => p.DropZoneIconName, "FabricFolder");
        });

        Assert.IsTrue(component.Find(".bit-fin-dzi").ClassList.Contains("bit-icon--FabricFolder"));
    }

    [TestMethod]
    public void BitFileInputShouldRespectDropZoneIcon()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowDropZone, true);
            parameters.Add(p => p.DropZoneIconName, "FabricFolder");
            parameters.Add(p => p.DropZoneIcon, BitIconInfo.Fa("solid file-lines"));
            parameters.Add(p => p.Classes, new BitFileInputClassStyles { DropZoneIcon = "custom-dzi" });
            parameters.Add(p => p.Styles, new BitFileInputClassStyles { DropZoneIcon = "color: red;" });
        });

        var icon = component.Find(".bit-fin-dzi");

        // the icon info takes precedence over the name of a built-in icon.
        Assert.IsTrue(icon.ClassList.Contains("fa-file-lines"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--FabricFolder"));
        Assert.IsTrue(icon.ClassList.Contains("custom-dzi"));
        Assert.Contains("color: red", icon.GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void BitFileInputShouldRenderNoDropZoneGlyphForAnEmptyIconName()
    {
        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowDropZone, true);
            parameters.Add(p => p.DropZoneIconName, string.Empty);
        });

        Assert.AreEqual(0, component.FindAll(".bit-fin-dzi").Count);
    }

    [TestMethod]
    public void BitFileInputFileListShouldHaveAnAccessibleName()
    {
        var component = RenderComponent<BitFileInput>();

        Assert.AreEqual("Selected files", component.Find(".bit-fin-fl").GetAttribute("aria-label"));

        component.Render(parameters => parameters.Add(p => p.FileListAriaLabel, "فایل‌های انتخاب شده"));

        Assert.AreEqual("فایل‌های انتخاب شده", component.Find(".bit-fin-fl").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFileInputShouldLazilyLoadThePreviewThumbnails()
    {
        SetupFiles([new() { Name = "a.png", Size = 10, FileId = "1", ContentType = "image/png", PreviewUrl = "blob:a" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowPreview, true);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("lazy", component.Find(".bit-fin-prv").GetAttribute("loading"));
    }

    [TestMethod,
       DataRow("report.PDF", ".pdf"),
       DataRow("archive.tar.gz", ".gz"),
       DataRow("LICENSE", ""),
       DataRow(".gitignore", ""),
       DataRow(".env.local", ".local"),
       DataRow("trailing.", "")
    ]
    public void BitFileInputInfoShouldReportTheExtension(string name, string expected)
    {
        Assert.AreEqual(expected, new BitFileInputInfo { Name = name }.Extension);
    }

    [TestMethod]
    public async Task BitFileInputResetShouldNotAnnounceAnEmptySelection()
    {
        var component = RenderComponent<BitFileInput>();

        var liveRegion = component.Find(".bit-fin-lvr");

        Assert.AreEqual(string.Empty, liveRegion.TextContent);

        await component.InvokeAsync(() => component.Instance.Reset());

        // clearing what was already empty changes nothing, so there is nothing to announce.
        Assert.AreEqual(string.Empty, component.Find(".bit-fin-lvr").TextContent);

        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.Contains("1 file selected.", component.Find(".bit-fin-lvr").TextContent);

        await component.InvokeAsync(() => component.Instance.Reset());

        Assert.Contains("No file selected.", component.Find(".bit-fin-lvr").TextContent);
    }

    [TestMethod]
    public void BitFileInputShouldApplyTheNewCascadingParameters()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitFileInputParams
            {
                ShowDropZone = true,
                DropZoneIconName = "FabricFolder",
                FileListAriaLabel = "Cascaded list"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFileInput>(0);
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-fin-dzn"));
        Assert.IsTrue(component.Find(".bit-fin-dzi").ClassList.Contains("bit-icon--FabricFolder"));
        Assert.AreEqual("Cascaded list", component.Find(".bit-fin-fl").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFileInputFileListShouldTakeATabStopOnlyWhenNothingInsideItCan()
    {
        // an empty list has nothing to scroll, so it stays out of the tab order.
        var component = RenderComponent<BitFileInput>();

        Assert.IsFalse(component.Find(".bit-fin-fl").HasAttribute("tabindex"));

        // a list given a ceiling scrolls, and without the remove buttons there is nothing inside it
        // a keyboard can reach, so the list itself has to be reachable to be scrollable.
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("0", component.Find(".bit-fin-fl").GetAttribute("tabindex"));

        // the remove buttons are that way in, so the list does not take a stop of its own on top of them.
        component.Render(parameters => parameters.Add(p => p.ShowRemoveButton, true));

        Assert.IsFalse(component.Find(".bit-fin-fl").HasAttribute("tabindex"));
    }

    [TestMethod]
    public void BitFileInputFileListShouldLeaveTheTabStopToACustomFileViewTemplate()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.FileViewTemplate, file => builder => builder.AddContent(0, file.Name));
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        // the template owns what is inside the list, focusable or not, so the list does not compete with it.
        Assert.IsFalse(component.Find(".bit-fin-fl").HasAttribute("tabindex"));
    }

    [TestMethod]
    public void BitFileInputValidationShouldRequireAValidFile()
    {
        var component = RenderComponent<BitFileInputValidationTest>();

        var form = component.Find("form");

        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-inv"));

        form.Submit();

        Assert.AreEqual(0, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-input-hidden").GetAttribute("aria-invalid"));

        // a selection of nothing but rejected files is still no value.
        SetupFiles([new() { Name = "big.txt", Size = 100, FileId = "1" }]);
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.IsNull(component.Instance.TestModel.Value);
        Assert.IsTrue(component.Find(".bit-fin").ClassList.Contains("bit-inv"));

        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "2" },
                    new() { Name = "big.txt", Size = 100, FileId = "3" }]);
        component.Find(".bit-fin-fi").Change(string.Empty);

        // the model receives the names of the valid files, and the field is validated right away.
        Assert.AreEqual("a.txt", component.Instance.TestModel.Value);
        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.IsNull(component.Find(".bit-input-hidden").GetAttribute("aria-invalid"));

        form.Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
    }

    [TestMethod]
    public async Task BitFileInputValueShouldJoinTheNamesOfTheValidFilesOnly()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" },
                    new() { Name = "big.txt", Size = 100, FileId = "2" },
                    new() { Name = "c.txt", Size = 20, FileId = "3" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxSize, 50);
        });

        var instance = component.Instance;

        Assert.IsNull(instance.Value);

        component.Find(".bit-fin-fi").Change(string.Empty);

        // the file the size validation rejected is in the list but not in the value.
        Assert.HasCount(3, instance.Files);
        Assert.AreEqual("a.txt,c.txt", instance.Value);
        Assert.AreEqual("a.txt,c.txt", component.Find(".bit-input-hidden").GetAttribute("value"));

        await component.InvokeAsync(() => instance.RemoveFile(instance.Files[0]));

        Assert.AreEqual("c.txt", instance.Value);

        await component.InvokeAsync(() => instance.RemoveFile(null));

        Assert.IsNull(instance.Value);
        Assert.IsFalse(component.Find(".bit-input-hidden").HasAttribute("value"));
    }

    [TestMethod]
    public void BitFileInputShouldCarryItsValueInAHiddenInput()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Name, "attachments");
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object>
            {
                ["data-test"] = "carrier",
                ["form"] = "upload-form"
            });
        });

        var root = component.Find(".bit-fin");
        var carrier = component.Find("input.bit-input-hidden");

        // the carrier is not the native file input, which holds what the browser picked rather than the list.
        Assert.IsTrue(root.LastElementChild!.ClassList.Contains("bit-input-hidden"));
        Assert.AreEqual("text", carrier.GetAttribute("type"));
        Assert.AreEqual("attachments", carrier.GetAttribute("name"));
        // Required stays off it: the browser would block the submit over a control nobody can see.
        Assert.IsFalse(carrier.HasAttribute("required"));
        Assert.AreEqual("carrier", carrier.GetAttribute("data-test"));
        Assert.AreEqual("upload-form", carrier.GetAttribute("form"));
        Assert.AreEqual("-1", carrier.GetAttribute("tabindex"));
        Assert.AreEqual("true", carrier.GetAttribute("aria-hidden"));
        Assert.IsFalse(carrier.HasAttribute("value"));
        Assert.IsFalse(component.Find(".bit-fin-fi").HasAttribute("data-test"));
        Assert.IsFalse(component.Find(".bit-fin-fi").HasAttribute("name"));

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("a.txt", component.Find("input.bit-input-hidden").GetAttribute("value"));
    }

    [TestMethod]
    public void BitFileInputShouldReportTheFilesThroughOnFilesChangeAndTheValueThroughOnChange()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" },
                    new() { Name = "big.txt", Size = 100, FileId = "2" }]);

        var filesChanges = new List<BitFileInputInfo[]>();
        var values = new List<string?>();

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxSize, 50);
            parameters.Add(p => p.OnFilesChange, (BitFileInputInfo[] files) => filesChanges.Add(files));
            parameters.Add(p => p.OnChange, (string? value) => values.Add(value));
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        // the files callback hands over the whole list, the rejected file included; the value is the valid names.
        Assert.HasCount(1, filesChanges);
        CollectionAssert.AreEqual(new[] { "a.txt", "big.txt" }, filesChanges[0].Select(f => f.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "a.txt" }, values);

        // the same selection again is a change of the files, not of the value.
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.HasCount(2, filesChanges);
        Assert.HasCount(1, values);
    }

    [TestMethod,
        DataRow(null),
        DataRow("")
    ]
    public void BitFileInputShouldClearTheSelectionWhenTheValueIsEmptiedFromOutside(string? emptied)
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);

        string? value = null;
        var valueChanges = 0;
        var filesChanges = 0;
        var changes = 0;

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, (string? v) => { value = v; valueChanges++; });
            parameters.Add(p => p.OnFilesChange, () => filesChanges++);
            parameters.Add(p => p.OnChange, () => changes++);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("a.txt", value);
        Assert.HasCount(1, component.Instance.Files);

        component.Render(parameters => parameters.Add(p => p.Value, emptied));

        // a form reset or a model cleared after a save clears the selection it stood for - quietly, since the
        // change came from the consumer rather than the user.
        Assert.IsEmpty(component.Instance.Files);
        Assert.IsEmpty(component.FindAll(".bit-fin-itm"));
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.FileInput.reset"].Count);
        Assert.AreEqual(1, valueChanges);
        Assert.AreEqual(1, filesChanges);
        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public void BitFileInputShouldLeaveTheSelectionAloneForAPushedValueThatNamesFiles()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);

        string? value = null;

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, (string? v) => value = v);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("a.txt", value);

        // a browser cannot be handed files from code, so a value naming other files changes nothing.
        component.Render(parameters => parameters.Add(p => p.Value, "other.txt"));

        Assert.HasCount(1, component.Instance.Files);
        Assert.AreEqual("a.txt", component.Instance.Files[0].Name);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.FileInput.reset"].Count);

        // the selection writes its own value back the next time it changes.
        SetupFiles([new() { Name = "b.txt", Size = 10, FileId = "2" }]);
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("b.txt", value);
    }

    [TestMethod]
    public async Task BitFileInputResetShouldPublishANullValueWithoutTheChangeCallbacks()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);

        string? value = null;
        var valueChanges = 0;
        var filesChanges = 0;
        var changes = 0;
        var removes = 0;

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, (string? v) => { value = v; valueChanges++; });
            parameters.Add(p => p.OnFilesChange, () => filesChanges++);
            parameters.Add(p => p.OnChange, () => changes++);
            parameters.Add(p => p.OnRemove, () => removes++);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("a.txt", value);
        Assert.AreEqual(1, valueChanges);
        Assert.AreEqual(1, filesChanges);
        Assert.AreEqual(1, changes);

        await component.InvokeAsync(() => component.Instance.Reset());

        // the binding follows the emptied list, but nothing reports a change the user did not make.
        Assert.IsNull(value);
        Assert.IsNull(component.Instance.Value);
        Assert.AreEqual(2, valueChanges);
        Assert.AreEqual(1, filesChanges);
        Assert.AreEqual(1, changes);
        Assert.AreEqual(0, removes);
        Assert.IsEmpty(component.Instance.Files);
    }

    [TestMethod]
    public async Task BitFileInputReadOnlyShouldKeepTheFilesButRefuseEveryChange()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);

        var removes = 0;

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
            parameters.Add(p => p.OnRemove, () => removes++);
        });

        component.Find(".bit-fin-fi").Change(string.Empty);

        component.Render(parameters => parameters.Add(p => p.ReadOnly, true));

        var root = component.Find(".bit-fin");
        var browse = component.Find(".bit-fin-lbl");
        var remove = component.Find(".bit-fin-rbt");

        Assert.IsTrue(root.ClassList.Contains("bit-fin-rdl"));
        Assert.IsFalse(root.ClassList.Contains("bit-dis"));

        // the buttons stay in the tab order and say they are unavailable rather than being taken away.
        Assert.AreEqual("true", browse.GetAttribute("aria-disabled"));
        Assert.IsFalse(browse.HasAttribute("disabled"));
        Assert.AreEqual("true", remove.GetAttribute("aria-disabled"));
        Assert.IsFalse(remove.HasAttribute("disabled"));
        Assert.IsTrue(component.Find("input.bit-input-hidden").HasAttribute("readonly"));

        await remove.ClickAsync(new());

        Assert.HasCount(1, component.Instance.Files);
        Assert.AreEqual(0, removes);

        await browse.ClickAsync(new());
        await component.InvokeAsync(() => component.Instance.Browse());

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.FileInput.browse"].Count);

        // an editable one browses again.
        component.Render(parameters => parameters.Add(p => p.ReadOnly, false));

        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-fin-rdl"));
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-disabled"));

        await component.InvokeAsync(() => component.Instance.Browse());

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.FileInput.browse"].Count);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitFileInputReadOnlyShouldTurnDropAndPasteOff(bool readOnly)
    {
        RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.AllowDrop, true);
            parameters.Add(p => p.AllowPaste, true);
        });

        var args = Context.JSInterop.Invocations
                                    .Single(i => i.Identifier == "BitBlazorUI.FileInput.setupDragDrop").Arguments;

        Assert.AreEqual(readOnly is false, args[4]);
        Assert.AreEqual(readOnly is false, args[5]);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitFileInputShouldTakeReadOnlyAndRequiredFromTheCascade(bool readOnly)
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitFileInputParams
            {
                ReadOnly = readOnly,
                Required = true
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFileInput>(0);
                builder.CloseComponent();
            });
        });

        var fileInput = component.FindComponent<BitFileInput>().Instance;
        var carrier = component.Find("input.bit-input-hidden");

        Assert.AreEqual(readOnly, fileInput.ReadOnly);
        Assert.IsTrue(fileInput.Required);
        Assert.AreEqual(readOnly, component.Find(".bit-fin").ClassList.Contains("bit-fin-rdl"));
        Assert.AreEqual(readOnly ? "true" : null, component.Find(".bit-fin-lbl").GetAttribute("aria-disabled"));
        Assert.IsFalse(carrier.HasAttribute("required"));
        Assert.AreEqual(readOnly, carrier.HasAttribute("readonly"));
    }

    [TestMethod]
    public void BitFileInputDirectReadOnlyShouldOverrideTheCascadedOne()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitFileInputParams
            {
                ReadOnly = true,
                Required = true
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFileInput>(0);
                builder.AddAttribute(1, nameof(BitFileInput.ReadOnly), false);
                builder.CloseComponent();
            });
        });

        // an explicit false is a written parameter like any other, so the cascade does not fill it in.
        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-fin-rdl"));
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-disabled"));

        // what the markup left unwritten still comes from the cascade.
        Assert.IsTrue(component.FindComponent<BitFileInput>().Instance.Required);
    }

    [TestMethod]
    public async Task BitFileInputShouldClearAPushedValueWhenItsSelectionHoldsNoValidFile()
    {
        SetupFiles([new() { Name = "big.txt", Size = 100, FileId = "1" }]);

        string? value = "old.pdf";

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.MaxSize, 50);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, (string? v) => value = v);
        });

        // a selection of nothing but rejected files holds no value, whatever value was pushed in before it.
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.IsNull(value);
        Assert.IsNull(component.Instance.Value);

        // and a reset clears a value pushed in from outside too, as it promises to.
        component.Render(parameters => parameters.Add(p => p.Value, "older.pdf"));

        await component.InvokeAsync(() => component.Instance.Reset());

        Assert.IsNull(value);
        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitFileInputReadOnlyShouldTakeNoSelectionThroughItsNativeInput()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);

        var filesChanges = 0;
        string? value = null;

        var component = RenderComponent<BitFileInput>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, (string? v) => value = v);
            parameters.Add(p => p.OnFilesChange, () => filesChanges++);
        });

        var nativeInput = component.Find(".bit-fin-fi");

        // a label of the app's own pointed at InputId opens no dialog on a disabled native input...
        Assert.IsTrue(nativeInput.HasAttribute("disabled"));
        Assert.AreEqual("true", nativeInput.GetAttribute("aria-disabled"));

        // ...and a selection that reaches it all the same is not taken.
        nativeInput.Change(string.Empty);

        Assert.AreEqual(0, component.Instance.Files?.Count ?? 0);
        Assert.AreEqual(0, filesChanges);
        Assert.IsNull(value);
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.FileInput.setup"]);

        // an editable one takes it again.
        component.Render(parameters => parameters.Add(p => p.ReadOnly, false));

        Assert.IsFalse(component.Find(".bit-fin-fi").HasAttribute("disabled"));

        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("a.txt", value);
        Assert.AreEqual(1, filesChanges);
    }

    [TestMethod]
    public void BitFileInputShouldDeclareAFailedValidationOnItsBrowseButton()
    {
        var component = RenderComponent<BitFileInputValidationTest>();

        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-invalid"));

        component.Find("form").Submit();

        // the input the value travels through is out of the accessibility tree, so the button the user operates is
        // what tells a screen reader that the field is in error.
        Assert.AreEqual("true", component.Find(".bit-fin-lbl").GetAttribute("aria-invalid"));

        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public async Task BitFileInputResetShouldLeaveTheEditContextAlone()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1" }]);

        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitFileInput>(0);
                builder.AddAttribute(1, nameof(BitFileInput.AutoReset), true);
                builder.AddAttribute(2, nameof(BitFileInput.Value), model.Value);
                builder.AddAttribute(3, nameof(BitFileInput.ValueChanged),
                                     EventCallback.Factory.Create<string?>(this, v => model.Value = v));
                builder.AddAttribute(4, nameof(BitFileInput.ValueExpression),
                                     (System.Linq.Expressions.Expression<Func<string?>>)(() => model.Value));
                builder.CloseComponent();
            }));
        });

        // a selection is the user's, so the field is validated and marked modified.
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("a.txt", model.Value);
        Assert.IsTrue(editContext.IsModified());

        editContext.MarkAsUnmodified();

        // an AutoReset browse empties the list before the dialog opens: the value follows the list, but the field is
        // neither validated nor marked modified, so a required one does not turn invalid while the dialog is open.
        var fileInput = component.FindComponent<BitFileInput>();

        await component.InvokeAsync(() => fileInput.Instance.Browse());

        Assert.IsNull(model.Value);
        Assert.AreEqual(0, fileInput.Instance.Files?.Count ?? 0);
        Assert.IsFalse(editContext.IsModified());
        Assert.IsFalse(editContext.GetValidationMessages().Any());
        Assert.IsFalse(component.Find(".bit-fin").ClassList.Contains("bit-inv"));
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-invalid"));
    }

    private void SetupFiles(BitFileInputInfo[] files)
    {
        Context.JSInterop.Setup<BitFileInputInfo[]>("BitBlazorUI.FileInput.setup", _ => true).SetResult(files);
    }
}
