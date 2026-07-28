using System.Collections.Generic;
using System.Linq;
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
    public async Task BitFileInputRemoveFileShouldRespectIsEnabled()
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

        component.Render(parameters => parameters.Add(p => p.IsEnabled, false));

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

    private void SetupFiles(BitFileInputInfo[] files)
    {
        Context.JSInterop.Setup<BitFileInputInfo[]>("BitBlazorUI.FileInput.setup", _ => true).SetResult(files);
    }
}
