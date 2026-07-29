using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileUpload;

[TestClass]
public class BitFileUploadTests : BunitTestContext
{
    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitUploadFileHasBasicClass(bool isEnabled)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitFileUpload = com.Find(".bit-upl-fi");

        Assert.IsNotNull(bitFileUpload);
    }

    [TestMethod]
    public void BitFileUploadShouldRenderCorrectly()
    {
        var com = RenderComponent<BitFileUpload>();

        var root = com.Find(".bit-upl");
        var input = com.Find(".bit-upl-fi");
        var label = com.Find(".bit-upl-lbl");

        Assert.IsNotNull(root);
        Assert.IsNotNull(input);
        Assert.IsNotNull(label);
        Assert.AreEqual("file", input.GetAttribute("type"));
        Assert.AreEqual("button", label.GetAttribute("type"));
        Assert.AreEqual("Browse", label.TextContent.Trim());
    }

    [TestMethod]
    public void BitFileUploadShouldRenderCustomLabel()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Label, "Select files");
        });

        var label = com.Find(".bit-upl-lbl");

        Assert.AreEqual("Select files", label.TextContent.Trim());
    }

    [TestMethod]
    public void BitFileUploadShouldNotRenderLabelButtonWhenLabelIsEmpty()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Label, string.Empty);
        });

        Assert.AreEqual(0, com.FindAll(".bit-upl-lbl").Count);
    }

    [TestMethod]
    public void BitFileUploadShouldRenderLabelTemplateInsteadOfLabelButton()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, "<div class='custom-label'>Custom browse</div>");
        });

        Assert.IsNotNull(com.Find(".custom-label"));
        Assert.AreEqual(0, com.FindAll(".bit-upl-lbl").Count);
    }

    [TestMethod]
    public void BitFileUploadShouldWireUpAriaLabelledByToTheLabelButton()
    {
        var com = RenderComponent<BitFileUpload>();

        var input = com.Find(".bit-upl-fi");
        var label = com.Find(".bit-upl-lbl");

        var buttonId = label.GetAttribute("id");

        Assert.IsNotNull(buttonId);
        Assert.AreEqual(buttonId, input.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitFileUploadShouldPreferAriaLabelOverAriaLabelledBy()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Attach a document");
        });

        var input = com.Find(".bit-upl-fi");

        Assert.AreEqual("Attach a document", input.GetAttribute("aria-label"));
        Assert.IsFalse(input.HasAttribute("aria-labelledby"));
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileUploadMultipleAttributeTest(bool isMultiple)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, isMultiple);
        });

        var bitFileUpload = com.Find(".bit-upl-fi");

        Assert.AreEqual(isMultiple, bitFileUpload.HasAttribute("multiple"));
    }

    [TestMethod]
    public void BitFileUploadAcceptAttributeTest()
    {
        var allowedExtensions = new List<string> { ".mp4", ".mp3" };

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AllowedExtensions, allowedExtensions);
        });

        var bitFileUpload = com.Find(".bit-upl-fi");
        var attribute = bitFileUpload.GetAttribute("accept");

        Assert.AreEqual(".mp4,.mp3", attribute);
    }

    [TestMethod]
    public void BitFileUploadShouldNotSetAcceptAttributeByDefault()
    {
        var com = RenderComponent<BitFileUpload>();

        var input = com.Find(".bit-upl-fi");

        // the default AllowedExtensions (["*"]) must not produce an accept attribute
        Assert.IsFalse(input.HasAttribute("accept"));
    }

    [TestMethod]
    public void BitFileUploadShouldNormalizeAllowedExtensionsInTheAcceptAttribute()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            // the leading dot is optional and the entries get trimmed; MIME types pass through untouched.
            parameters.Add(p => p.AllowedExtensions, new[] { "jpg", " .png ", "image/webp" });
        });

        var input = com.Find(".bit-upl-fi");

        Assert.AreEqual(".jpg,.png,image/webp", input.GetAttribute("accept"));
    }

    [TestMethod]
    public void BitFileUploadShouldPreferAcceptOverAllowedExtensions()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Accept, "image/*");
            parameters.Add(p => p.AllowedExtensions, new[] { ".pdf" });
        });

        var input = com.Find(".bit-upl-fi");

        Assert.AreEqual("image/*", input.GetAttribute("accept"));
    }

    [TestMethod]
    public void BitFileUploadShouldApplyClassesAndStylesToRoot()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitFileUploadClassStyles { Root = "custom-root" });
            parameters.Add(p => p.Styles, new BitFileUploadClassStyles { Root = "background-color: red;" });
        });

        var root = com.Find(".bit-upl");

        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("background-color: red"));
    }

    [TestMethod]
    public void BitFileUploadShouldHideLabelButtonWhenHideLabelIsSet()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.HideLabel, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-upl-lbl").Count);
    }

    [TestMethod]
    public void BitFileUploadShouldRenderDescriptionAndWireItToTheLabelButton()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Description, "Up to 5 MB.");
        });

        var label = com.Find(".bit-upl-lbl");
        var description = com.Find(".bit-upl-dsc");

        Assert.AreEqual("Up to 5 MB.", description.TextContent.Trim());
        Assert.AreEqual(description.GetAttribute("id"), label.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileUploadShouldPreferDescriptionTemplateOverDescription()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Description, "plain text");
            parameters.Add(p => p.DescriptionTemplate, "<b class='custom-description'>rich text</b>");
        });

        var description = com.Find(".bit-upl-dsc");

        Assert.IsNotNull(com.Find(".custom-description"));
        Assert.IsFalse(description.TextContent.Contains("plain text"));
    }

    [TestMethod]
    public void BitFileUploadShouldNotRenderDescriptionContainerByDefault()
    {
        var com = RenderComponent<BitFileUpload>();

        Assert.AreEqual(0, com.FindAll(".bit-upl-dsc").Count);
    }

    [TestMethod]
    public void BitFileUploadOverallProgressShouldBeZeroWithoutFiles()
    {
        var com = RenderComponent<BitFileUpload>();

        Assert.AreEqual(0, com.Instance.TotalSize);
        Assert.AreEqual(0, com.Instance.TotalUploadedSize);
        Assert.AreEqual(0, com.Instance.OverallUploadProgress);
    }

    [TestMethod]
    public void BitFileUploadShouldRenderLiveRegion()
    {
        var com = RenderComponent<BitFileUpload>();

        var liveRegion = com.Find(".bit-upl-lvr");

        Assert.AreEqual("status", liveRegion.GetAttribute("role"));
        Assert.AreEqual("polite", liveRegion.GetAttribute("aria-live"));
    }

    [TestMethod]
    public void BitFileUploadShouldApplyClassesAndStylesToLabel()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitFileUploadClassStyles { Label = "custom-label" });
            parameters.Add(p => p.Styles, new BitFileUploadClassStyles { Label = "color: blue;" });
        });

        var label = com.Find(".bit-upl-lbl");

        Assert.IsTrue(label.ClassList.Contains("custom-label"));
        Assert.IsTrue(label.GetAttribute("style")!.Contains("color: blue"));
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileUploadIsEnabledTest(bool isEnabled)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitFileUpload = com.Find(".bit-upl");
        var bitFileUploadInput = com.Find(".bit-upl-fi");

        if (isEnabled)
        {
            Assert.IsFalse(bitFileUpload.ClassList.Contains("bit-dis"));
            Assert.IsFalse(bitFileUploadInput.HasAttribute("disabled"));
        }
        else
        {
            Assert.IsTrue(bitFileUpload.ClassList.Contains("bit-dis"));
            Assert.IsTrue(bitFileUploadInput.HasAttribute("disabled"));
        }
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptUploadIconNameParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadIconName, "Play");
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptUploadIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadIcon, BitIconInfo.Css("fa-solid fa-upload"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptFontAwesomeUploadIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadIcon, BitIconInfo.Fa("solid cloud-arrow-up"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptPauseIconNameParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.PauseIconName, "Pause");
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptPauseIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.PauseIcon, BitIconInfo.Css("fa-solid fa-pause"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptPauseIconBitInfoParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.PauseIcon, BitIconInfo.Bi("pause-circle"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptCancelIconNameParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.CancelIconName, "Cancel");
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptCancelIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.CancelIcon, BitIconInfo.Css("fa-solid fa-xmark"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptCancelIconBitInfoParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.CancelIcon, BitIconInfo.Fa("solid circle-xmark"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptRemoveIconNameParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RemoveIconName, "Delete");
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptRemoveIconParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RemoveIcon, BitIconInfo.Css("fa-solid fa-trash"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldAcceptRemoveIconBitInfoParameter()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RemoveIcon, BitIconInfo.Bi("trash"));
        });

        var root = com.Find(".bit-upl");

        Assert.IsNotNull(root);
    }

    [TestMethod]
    public void BitFileUploadShouldDisableTheLabelButtonWhenDisabled()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(com.Find(".bit-upl-lbl").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitFileUploadShouldWireTheDescriptionToTheInputToo()
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Description, "Up to 5 MB.");
        });

        var input = com.Find(".bit-upl-fi");
        var description = com.Find(".bit-upl-dsc");

        Assert.AreEqual(description.GetAttribute("id"), input.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileUploadShouldRenderTheSelectedFiles()
    {
        SetupFiles([new() { Name = "one.txt", Size = 1024, FileId = "1", Index = 0 },
                    new() { Name = "two.txt", Size = 2048, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
        });

        SelectFiles(com);

        var items = com.FindAll(".bit-upl-itm");

        Assert.HasCount(2, items);
        Assert.AreEqual("one.txt", com.FindAll(".bit-upl-fn")[0].TextContent.Trim());
        Assert.AreEqual("two.txt", com.FindAll(".bit-upl-fn")[1].TextContent.Trim());
        Assert.AreEqual(2, com.Instance.Files.Count);
    }

    [TestMethod]
    public void BitFileUploadShouldNotRenderTheFileListWhenHideFileViewIsSet()
    {
        SetupFiles([new() { Name = "one.txt", Size = 1024, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.HideFileView, true);
        });

        SelectFiles(com);

        Assert.IsEmpty(com.FindAll(".bit-upl-itm"));
        Assert.AreEqual(1, com.Instance.Files.Count);
    }

    [TestMethod]
    public void BitFileUploadShouldRejectAFileLargerThanMaxSize()
    {
        SetupFiles([new() { Name = "big.txt", Size = 2048, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.MaxSize, 1024);
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[0].Status);
        Assert.AreEqual("The file size is larger than the max size", com.Find(".bit-upl-us").TextContent.Trim());
    }

    [TestMethod]
    public void BitFileUploadShouldRejectAFileSmallerThanMinSize()
    {
        SetupFiles([new() { Name = "tiny.txt", Size = 8, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.MinSize, 1024);
            parameters.Add(p => p.MinSizeErrorMessage, "Too small");
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[0].Status);
        Assert.AreEqual("Too small", com.Find(".bit-upl-us").TextContent.Trim());
    }

    [TestMethod]
    public void BitFileUploadShouldRejectAFileWithANotAllowedExtension()
    {
        SetupFiles([new() { Name = "doc.pdf", Size = 1024, FileId = "1", Index = 0 },
                    new() { Name = "photo.JPG", Size = 1024, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            // the leading dot is optional and the matching is case-insensitive.
            parameters.Add(p => p.AllowedExtensions, new[] { "jpg" });
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[1].Status);
    }

    [TestMethod]
    public void BitFileUploadShouldMatchWildcardMimeTypesInAllowedExtensions()
    {
        SetupFiles([new() { Name = "photo", ContentType = "image/png", Size = 1024, FileId = "1", Index = 0 },
                    new() { Name = "clip", ContentType = "video/mp4", Size = 1024, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.AllowedExtensions, new[] { "image/*" });
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[1].Status);
    }

    [TestMethod]
    public void BitFileUploadShouldRejectTheFilesBeyondMaxCount()
    {
        SetupFiles([new() { Name = "a.txt", Size = 1024, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 1024, FileId = "2", Index = 1 },
                    new() { Name = "c.txt", Size = 1024, FileId = "3", Index = 2 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxCount, 2);
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[1].Status);
        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[2].Status);
        Assert.AreEqual("The maximum number of files is exceeded", com.Instance.Files[2].Message);
    }

    [TestMethod]
    public void BitFileUploadShouldNotLetARejectedFileConsumeAMaxCountSlot()
    {
        SetupFiles([new() { Name = "huge.txt", Size = 9999, FileId = "1", Index = 0 },
                    new() { Name = "a.txt", Size = 10, FileId = "2", Index = 1 },
                    new() { Name = "b.txt", Size = 10, FileId = "3", Index = 2 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxCount, 2);
            parameters.Add(p => p.MaxSize, 1024);
        });

        SelectFiles(com);

        // the oversized file is out for its size, so the two good files still both fit under the cap.
        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[1].Status);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[2].Status);
    }

    [TestMethod]
    public void BitFileUploadShouldRejectAFileThroughTheFileValidator()
    {
        SetupFiles([new() { Name = "empty.txt", Size = 0, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.FileValidator, file => file.Size == 0 ? "Empty files are not allowed" : null);
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[0].Status);
        Assert.AreEqual("Empty files are not allowed", com.Instance.Files[0].Message);
    }

    [TestMethod]
    public void BitFileUploadShouldInvalidateOnlyItsOwnFileWhenTheValidatorThrows()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 20, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.FileValidator, file => file.Size == 10 ? throw new System.InvalidOperationException("boom") : null);
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[0].Status);
        Assert.AreEqual("boom", com.Instance.Files[0].Message);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[1].Status);
    }

    [TestMethod]
    public void BitFileUploadShouldReportTheRejectedFilesThroughOnInvalid()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1", Index = 0 },
                    new() { Name = "big.txt", Size = 9999, FileId = "2", Index = 1 }]);

        BitFileInfo[]? invalidFiles = null;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxSize, 1024);
            parameters.Add(p => p.OnInvalid, files => invalidFiles = files);
        });

        SelectFiles(com);

        Assert.IsNotNull(invalidFiles);
        Assert.HasCount(1, invalidFiles);
        Assert.AreEqual("big.txt", invalidFiles[0].Name);
    }

    [TestMethod]
    public void BitFileUploadShouldNotInvokeOnInvalidWhenEveryFileIsAccepted()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1", Index = 0 }]);

        var invoked = false;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.OnInvalid, _ => invoked = true);
        });

        SelectFiles(com);

        Assert.IsFalse(invoked);
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotUploadARejectedFile()
    {
        SetupFiles([new() { Name = "big.txt", Size = 9999, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.MaxSize, 1024);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldCompleteAFileOnASuccessfulResponse()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        BitFileInfo? completed = null;
        BitFileInfo[]? allComplete = null;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.OnUploadComplete, file => completed = file);
            parameters.Add(p => p.OnAllUploadsComplete, files => allComplete = files);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);

        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, "done"));

        Assert.AreEqual(BitFileUploadStatus.Completed, com.Instance.Files[0].Status);
        Assert.AreEqual(100, com.Instance.Files[0].TotalUploadedSize);
        Assert.AreEqual(BitFileUploadStatus.Completed, com.Instance.UploadStatus);
        Assert.IsNotNull(completed);
        Assert.IsNotNull(allComplete);
        Assert.AreEqual("File upload succeeded", com.Find(".bit-upl-us").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitFileUploadShouldFailAFileOnAnErrorResponse()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        BitFileInfo? failed = null;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.OnUploadFailed, file => failed = file);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        Assert.AreEqual(BitFileUploadStatus.Failed, com.Instance.Files[0].Status);
        Assert.AreEqual("boom", com.Instance.Files[0].Message);
        Assert.AreEqual(0, com.Instance.Files[0].TotalUploadedSize);
        Assert.IsNotNull(failed);
    }

    [TestMethod]
    public async Task BitFileUploadShouldOfferARetryButtonOnAFailedFile()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        var button = com.Find(".bit-upl-usi");

        Assert.AreEqual("Retry", button.GetAttribute("title"));
        Assert.AreEqual("Retry a.txt", button.GetAttribute("aria-label"));
        Assert.IsTrue(button.QuerySelector("i")!.ClassList.Contains("bit-icon--Refresh"));
    }

    [TestMethod]
    public void BitFileUploadShouldOfferAnUploadButtonOnAPendingFile()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        var button = com.Find(".bit-upl-usi");

        Assert.AreEqual("Upload", button.GetAttribute("title"));
        Assert.IsTrue(button.QuerySelector("i")!.ClassList.Contains("bit-icon--Play"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldRespectTheCustomRetryIconAndTitle()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RetryIcon, BitIconInfo.Fa("solid rotate"));
            parameters.Add(p => p.RetryButtonTitle, "Try again");
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        var button = com.Find(".bit-upl-usi");

        Assert.AreEqual("Try again", button.GetAttribute("title"));
        Assert.IsTrue(button.QuerySelector("i")!.ClassList.Contains("fa-rotate"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldRetryAFailedFileAutomatically()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AutoRetries, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        // the first failure is absorbed by the automatic retry, which puts the file back in flight.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);

        // the budget is spent by then, so the second failure settles the file.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));
        Assert.AreEqual(BitFileUploadStatus.Failed, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotFailAFilePausedWhileItsRetryWasPending()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AutoRetries, 1);
            parameters.Add(p => p.AutoRetryDelay, System.TimeSpan.FromMilliseconds(300));
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        var failing = com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        // the pause lands while the retry is still waiting out its delay.
        await com.InvokeAsync(() => com.Instance.PauseUpload());
        await failing;

        Assert.AreEqual(BitFileUploadStatus.Paused, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldGiveAManualRetryAFreshAutomaticBudget()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AutoRetries, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        Assert.AreEqual(BitFileUploadStatus.Failed, com.Instance.Files[0].Status);

        await com.InvokeAsync(() => com.Instance.Upload(com.Instance.Files[0]));

        // the manual retry starts over with a full budget, so this failure is absorbed again.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldResumeAChunkedFileFromItsLastCompletedChunk()
    {
        SetupFiles([new() { Name = "a.txt", Size = 3 * 512 * 1024, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ChunkedUpload, true);
            parameters.Add(p => p.ChunkSize, 512L * 1024);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, string.Empty));

        Assert.AreEqual(512 * 1024, com.Instance.Files[0].TotalUploadedSize);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);

        // a failed chunk leaves the bytes of the chunks before it counted as uploaded.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        Assert.AreEqual(BitFileUploadStatus.Failed, com.Instance.Files[0].Status);
        Assert.AreEqual(512 * 1024, com.Instance.Files[0].TotalUploadedSize);
    }

    [TestMethod]
    public async Task BitFileUploadShouldPauseAndResumeAFile()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.PauseUpload());

        Assert.AreEqual(BitFileUploadStatus.Paused, com.Instance.Files[0].Status);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldCancelAPausedFile()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.PauseUpload());
        await com.InvokeAsync(() => com.Instance.CancelUpload());

        Assert.AreEqual(BitFileUploadStatus.Canceled, com.Instance.Files[0].Status);
        Assert.AreEqual("File upload canceled", com.Find(".bit-upl-us").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitFileUploadShouldIgnoreALateResponseOfACanceledFile()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.CancelUpload());

        // the abort of the in-flight request comes back as a failure, which must not undo the cancellation.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 0, string.Empty));

        Assert.AreEqual(BitFileUploadStatus.Canceled, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldStartOnlyAsManyFilesAsConcurrentUploadsAllows()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 },
                    new() { Name = "c.txt", Size = 100, FileId = "3", Index = 2 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ConcurrentUploads, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[1].Status);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[2].Status);

        // the file settling hands its slot over to the next one waiting in the queue.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, string.Empty));

        Assert.AreEqual(BitFileUploadStatus.Completed, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[1].Status);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[2].Status);

        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(1, 200, string.Empty));

        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[2].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldStartEveryFileAtOnceWithoutAConcurrencyLimit()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.IsTrue(com.Instance.Files.All(f => f.Status == BitFileUploadStatus.InProgress));
    }

    [TestMethod]
    public async Task BitFileUploadShouldFreeAConcurrencySlotWhenAFileIsPaused()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ConcurrentUploads, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.PauseUpload(com.Instance.Files[0]));

        Assert.AreEqual(BitFileUploadStatus.Paused, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[1].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldReportTheOverallProgressOfTheBatch()
    {
        SetupFiles([new() { Name = "a.txt", Size = 300, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
        });

        SelectFiles(com);

        Assert.AreEqual(400, com.Instance.TotalSize);
        Assert.AreEqual(0, com.Instance.OverallUploadProgress);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, string.Empty));

        // the bigger file weighs three quarters of the batch.
        Assert.AreEqual(300, com.Instance.TotalUploadedSize);
        Assert.AreEqual(75, com.Instance.OverallUploadProgress);
    }

    [TestMethod]
    public void BitFileUploadShouldLeaveTheRejectedFilesOutOfTheOverallProgress()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "big.txt", Size = 9999, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxSize, 1024);
        });

        SelectFiles(com);

        Assert.AreEqual(100, com.Instance.TotalSize);
    }

    [TestMethod]
    public async Task BitFileUploadShouldRenderTheProgressBarOfARunningFile()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 40));

        var progressBar = com.Find(".bit-upl-pgb");

        Assert.AreEqual("40", progressBar.GetAttribute("aria-valuenow"));
        Assert.AreEqual("a.txt", progressBar.GetAttribute("aria-label"));
        Assert.AreEqual("40%", com.Find(".bit-upl-pct").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitFileUploadShouldDropTheStaleProgressOfAnAbortedRequestOnResume()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 60));
        await com.InvokeAsync(() => com.Instance.PauseUpload());

        // a non chunked upload starts over from the first byte, so the bytes of the aborted
        // request must not be left on the file pretending to be progress.
        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.AreEqual(0, com.Instance.Files[0].LastChunkUploadedSize);
        Assert.AreEqual("0%", com.Find(".bit-upl-pct").TextContent.Trim());
    }

    [TestMethod]
    public void BitFileUploadShouldFormatTheFileSizeThroughTheFileSizeFormatter()
    {
        SetupFiles([new() { Name = "a.txt", Size = 2048, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.FileSizeFormatter, size => $"{size} B");
        });

        SelectFiles(com);

        Assert.AreEqual("0 B/2048 B", com.Find(".bit-upl-fs").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitFileUploadShouldResetTheWholeState()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.Reset());

        Assert.IsEmpty(com.Instance.Files);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.UploadStatus);
        Assert.IsEmpty(com.FindAll(".bit-upl-itm"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldRemoveAFileThatNeverReachedTheServerWithoutARequest()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        BitFileInfo? removed = null;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ShowRemoveButton, true);
            parameters.Add(p => p.OnRemoveComplete, file => removed = file);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        Assert.AreEqual(BitFileUploadStatus.Removed, com.Instance.Files[0].Status);
        Assert.IsNotNull(removed);
        Assert.IsEmpty(com.FindAll(".bit-upl-itm"));
    }

    [TestMethod]
    public void BitFileUploadShouldAppendTheNewSelectionToTheExistingFiles()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Append, true);
        });

        SelectFiles(com);

        SetupFiles([new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 }]);

        SelectFiles(com);

        Assert.AreEqual(2, com.Instance.Files.Count);
        Assert.AreEqual("a.txt", com.Instance.Files[0].Name);
        Assert.AreEqual("b.txt", com.Instance.Files[1].Name);
    }

    [TestMethod]
    public void BitFileUploadShouldReplaceTheExistingFilesWithoutAppend()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        SetupFiles([new() { Name = "b.txt", Size = 100, FileId = "2", Index = 0 }]);

        SelectFiles(com);

        Assert.AreEqual(1, com.Instance.Files.Count);
        Assert.AreEqual("b.txt", com.Instance.Files[0].Name);
    }

    [TestMethod]
    public void BitFileUploadShouldAnnounceTheSelection()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "big.txt", Size = 9999, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxSize, 1024);
        });

        SelectFiles(com);

        // the zero width space that keeps consecutive announcements distinct is not part of the message.
        var announcement = com.Find(".bit-upl-lvr").TextContent.Trim('​', ' ');

        Assert.AreEqual("2 files selected. 1 not allowed.", announcement);
    }

    [TestMethod]
    public void BitFileUploadShouldAnnounceThroughTheAnnouncementProvider()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AnnouncementProvider, files => $"{files.Count} attachment(s)");
        });

        SelectFiles(com);

        Assert.AreEqual("1 attachment(s)", com.Find(".bit-upl-lvr").TextContent.Trim('​', ' '));
    }

    [TestMethod]
    public void BitFileUploadShouldRenderTheFileViewTemplateInsteadOfTheDefaultItem()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.FileViewTemplate, file => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-item");
                builder.AddContent(2, file.Name);
                builder.CloseElement();
            });
        });

        SelectFiles(com);

        Assert.AreEqual("a.txt", com.Find(".custom-item").TextContent.Trim());
        Assert.IsEmpty(com.FindAll(".bit-upl-itm"));
        // the wrapper of a templated file keeps carrying the listitem role of the list it lives in.
        Assert.HasCount(1, com.FindAll("[role='list'] > [role='listitem']"));
    }

    [TestMethod]
    public void BitFileUploadShouldApplyTheStatusClassOfEachFile()
    {
        SetupFiles([new() { Name = "big.txt", Size = 9999, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.MaxSize, 1024);
        });

        SelectFiles(com);

        Assert.IsTrue(com.Find(".bit-upl-itm").ClassList.Contains("bit-upl-fld"));
    }

    [TestMethod]
    public void BitFileUploadShouldRejectTheFilesBeyondMaxTotalSize()
    {
        SetupFiles([new() { Name = "a.txt", Size = 600, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 300, FileId = "2", Index = 1 },
                    new() { Name = "c.txt", Size = 400, FileId = "3", Index = 2 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxTotalSize, 1000);
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[1].Status);
        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[2].Status);
        Assert.AreEqual("The total size of the files is larger than the max total size", com.Instance.Files[2].Message);
    }

    [TestMethod]
    public async Task BitFileUploadShouldTakeBackAFileRejectedByMaxCountOnceARemovalFreesUpRoom()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 10, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxCount, 1);
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[1].Status);

        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[1].Status);
        Assert.IsNull(com.Instance.Files[1].Message);
    }

    [TestMethod]
    public async Task BitFileUploadShouldKeepAFileRejectedByItsOwnSizeRejectedAfterARemoval()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "1", Index = 0 },
                    new() { Name = "big.txt", Size = 9999, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.MaxSize, 1024);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        // freeing up room says nothing about a file that is too big on its own.
        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[1].Status);
        Assert.AreEqual("The file size is larger than the max size", com.Instance.Files[1].Message);
    }

    [TestMethod]
    public void BitFileUploadShouldRejectADuplicateWhenAllowDuplicatesIsOff()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, LastModified = 5, FileId = "1", Index = 0 },
                    new() { Name = "a.txt", Size = 10, LastModified = 5, FileId = "2", Index = 1 },
                    new() { Name = "a.txt", Size = 20, LastModified = 5, FileId = "3", Index = 2 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.AllowDuplicates, false);
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[1].Status);
        Assert.AreEqual("The file is already selected", com.Instance.Files[1].Message);
        // a different size makes it a different file.
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[2].Status);
    }

    [TestMethod]
    public void BitFileUploadShouldAllowDuplicatesByDefault()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, LastModified = 5, FileId = "1", Index = 0 },
                    new() { Name = "a.txt", Size = 10, LastModified = 5, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
        });

        SelectFiles(com);

        Assert.IsTrue(com.Instance.Files.All(f => f.Status == BitFileUploadStatus.Pending));
    }

    [TestMethod]
    public async Task BitFileUploadShouldTakeBackADuplicateOnceTheOriginalIsRemoved()
    {
        SetupFiles([new() { Name = "a.txt", Size = 10, LastModified = 5, FileId = "1", Index = 0 },
                    new() { Name = "a.txt", Size = 10, LastModified = 5, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.AllowDuplicates, false);
        });

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[1].Status);

        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[1].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotTakeBackAFileThatAlreadyUploaded()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.Append, true);
            parameters.Add(p => p.MaxCount, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, string.Empty));

        Assert.AreEqual(BitFileUploadStatus.Completed, com.Instance.Files[0].Status);

        SetupFiles([new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 }]);

        SelectFiles(com);

        // the completed file holds the only slot, so it is the newcomer that is turned away.
        Assert.AreEqual(BitFileUploadStatus.Completed, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[1].Status);
    }

    [TestMethod]
    public void BitFileUploadShouldNotSetTheCaptureAttributeByDefault()
    {
        var com = RenderComponent<BitFileUpload>();

        Assert.IsFalse(com.Find(".bit-upl-fi").HasAttribute("capture"));
    }

    [TestMethod,
       DataRow("user"),
       DataRow("environment")
    ]
    public void BitFileUploadShouldRenderTheCaptureAttribute(string capture)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Capture, capture);
        });

        Assert.AreEqual(capture, com.Find(".bit-upl-fi").GetAttribute("capture"));
    }

    [TestMethod,
       DataRow(true),
       DataRow(false)
    ]
    public void BitFileUploadShouldRenderTheWebkitdirectoryAttribute(bool directory)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Directory, directory);
        });

        Assert.AreEqual(directory, com.Find(".bit-upl-fi").HasAttribute("webkitdirectory"));
    }

    [TestMethod,
       DataRow(null, "bit-upl-fil"),
       DataRow(BitVariant.Fill, "bit-upl-fil"),
       DataRow(BitVariant.Outline, "bit-upl-otl"),
       DataRow(BitVariant.Text, "bit-upl-txt")
    ]
    public void BitFileUploadShouldApplyTheVariantClass(BitVariant? variant, string expectedClass)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Variant, variant);
        });

        Assert.IsTrue(com.Find(".bit-upl").ClassList.Contains(expectedClass));
    }

    [TestMethod,
       DataRow(null, "bit-upl-pri"),
       DataRow(BitColor.Primary, "bit-upl-pri"),
       DataRow(BitColor.Success, "bit-upl-suc"),
       DataRow(BitColor.Error, "bit-upl-err"),
       DataRow(BitColor.PrimaryBorder, "bit-upl-pbr")
    ]
    public void BitFileUploadShouldApplyTheColorClass(BitColor? color, string expectedClass)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(com.Find(".bit-upl").ClassList.Contains(expectedClass));
    }

    [TestMethod,
       DataRow(null, "bit-upl-md"),
       DataRow(BitSize.Small, "bit-upl-sm"),
       DataRow(BitSize.Medium, "bit-upl-md"),
       DataRow(BitSize.Large, "bit-upl-lg")
    ]
    public void BitFileUploadShouldApplyTheSizeClass(BitSize? size, string expectedClass)
    {
        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(com.Find(".bit-upl").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitFileUploadShouldNotLetTheColorClassCollideWithTheProgressBarClass()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            // the PrimaryBorder role class is bit-upl-pbr, so the progress bar has to be named otherwise.
            parameters.Add(p => p.Color, BitColor.PrimaryBorder);
        });

        SelectFiles(com);

        Assert.IsFalse(com.Find(".bit-upl").ClassList.Contains("bit-upl-pgb"));
    }

    [TestMethod]
    public void BitFileUploadShouldRenderThePreviewOfAnImageFile()
    {
        SetupFiles([new() { Name = "photo.png", Size = 100, FileId = "1", Index = 0, PreviewUrl = "blob:preview" }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ShowPreview, true);
        });

        SelectFiles(com);

        var preview = com.Find(".bit-upl-prv");

        Assert.AreEqual("blob:preview", preview.GetAttribute("src"));
        // the thumbnail is decorative: the file name right next to it already names the file.
        Assert.AreEqual(string.Empty, preview.GetAttribute("alt"));
        Assert.AreEqual("false", preview.GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitFileUploadShouldNotRenderThePreviewWhenShowPreviewIsOff()
    {
        SetupFiles([new() { Name = "photo.png", Size = 100, FileId = "1", Index = 0, PreviewUrl = "blob:preview" }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        Assert.IsEmpty(com.FindAll(".bit-upl-prv"));
    }

    [TestMethod]
    public void BitFileUploadShouldNotRenderThePreviewOfANonImageFile()
    {
        SetupFiles([new() { Name = "notes.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ShowPreview, true);
        });

        SelectFiles(com);

        Assert.IsEmpty(com.FindAll(".bit-upl-prv"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldDropThePreviewUrlOfARemovedFile()
    {
        SetupFiles([new() { Name = "photo.png", Size = 100, FileId = "1", Index = 0, PreviewUrl = "blob:preview" }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ShowPreview, true);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        // the object URL is handed back, so the file behind it stops being held in memory.
        Assert.IsNull(com.Instance.Files[0].PreviewUrl);
    }

    [TestMethod]
    public void BitFileUploadShouldApplyClassesAndStylesToThePreview()
    {
        SetupFiles([new() { Name = "photo.png", Size = 100, FileId = "1", Index = 0, PreviewUrl = "blob:preview" }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ShowPreview, true);
            parameters.Add(p => p.Classes, new BitFileUploadClassStyles { Preview = "custom-preview" });
            parameters.Add(p => p.Styles, new BitFileUploadClassStyles { Preview = "border-radius: 0;" });
        });

        SelectFiles(com);

        var preview = com.Find(".bit-upl-prv");

        Assert.IsTrue(preview.ClassList.Contains("custom-preview"));
        Assert.IsTrue(preview.GetAttribute("style")!.Contains("border-radius: 0"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldDropTheFileListElementOnceEveryFileIsRemoved()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        Assert.HasCount(1, com.FindAll("[role='list']"));

        await com.InvokeAsync(() => com.Instance.RemoveFile());

        // an empty list element would stay behind as a landmark naming nothing.
        Assert.IsEmpty(com.FindAll("[role='list']"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldStopTheTransferOfAFileBeingRemoved()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);

        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        Assert.AreEqual(BitFileUploadStatus.Removed, com.Instance.Files[0].Status);
        // the in-flight request of the removed file is aborted rather than left running.
        Context.JSInterop.VerifyInvoke("BitBlazorUI.FileUpload.pause");
    }

    [TestMethod]
    public async Task BitFileUploadShouldFreeAConcurrencySlotWhenAFileIsRemoved()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ConcurrentUploads, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        Assert.AreEqual(BitFileUploadStatus.Removed, com.Instance.Files[0].Status);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[1].Status);
    }

    [TestMethod]
    public void BitFileUploadShouldNotBeRemovingWhenIdle()
    {
        var com = RenderComponent<BitFileUpload>();

        Assert.IsFalse(com.Instance.IsRemoving);
    }

    [TestMethod,
       DataRow(0),
       DataRow(408),
       DataRow(429),
       DataRow(500),
       DataRow(503)
    ]
    public async Task BitFileUploadShouldAutoRetryTheFailuresASecondAttemptCouldSurvive(int status)
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AutoRetries, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, status, "boom"));

        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);
    }

    [TestMethod,
       DataRow(400),
       DataRow(401),
       DataRow(404),
       DataRow(413)
    ]
    public async Task BitFileUploadShouldNotAutoRetryAFailureThatWouldComeBackIdentical(int status)
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AutoRetries, 3);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, status, "boom"));

        // repeating a request the server already refused on its own terms helps nobody.
        Assert.AreEqual(BitFileUploadStatus.Failed, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldLetShouldAutoRetryOverrideTheBuiltInRule()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var seenStatus = -1;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AutoRetries, 1);
            parameters.Add(p => p.ShouldAutoRetry, (BitFileInfo _, int status) =>
            {
                seenStatus = status;
                return true;
            });
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 404, "boom"));

        Assert.AreEqual(404, seenStatus);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldLetShouldAutoRetryTurnARetryableFailureDown()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AutoRetries, 3);
            parameters.Add(p => p.ShouldAutoRetry, (BitFileInfo _, int _) => false);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        Assert.AreEqual(BitFileUploadStatus.Failed, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldSettleTheFileWhenShouldAutoRetryThrows()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.AutoRetries, 3);
            parameters.Add(p => p.ShouldAutoRetry,
                           (BitFileInfo _, int _) => throw new System.InvalidOperationException("boom"));
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "nope"));

        // a throwing predicate must not take the whole upload down with it.
        Assert.AreEqual(BitFileUploadStatus.Failed, com.Instance.Files[0].Status);
        Assert.AreEqual("nope", com.Instance.Files[0].Message);
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotShowAnUploadedSizeLargerThanTheFile()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.FileSizeFormatter, size => $"{size}");
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        // the progress events count the multipart overhead too, so the reported bytes can exceed the file.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 140));

        Assert.AreEqual("100/100", com.Find(".bit-upl-fs").TextContent.Trim());
        Assert.AreEqual("100%", com.Find(".bit-upl-pct").TextContent.Trim());
    }

    [TestMethod]
    public void BitFileUploadShouldExposeTheDimensionsOfTheSelectedImages()
    {
        SetupFiles([new() { Name = "photo.png", ContentType = "image/png", Size = 100, FileId = "1", Index = 0, Width = 1920, Height = 1080 }]);

        BitFileInfo? validated = null;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ReadImageDimensions, true);
            // the dimensions are already there by the time the validations run, so a rule can lean on them.
            parameters.Add(p => p.FileValidator, file =>
            {
                validated = file;
                return file.Width > 1280 ? "The image is too wide" : null;
            });
        });

        SelectFiles(com);

        Assert.IsNotNull(validated);
        Assert.AreEqual(1920, com.Instance.Files[0].Width);
        Assert.AreEqual(1080, com.Instance.Files[0].Height);
        Assert.AreEqual(BitFileUploadStatus.NotAllowed, com.Instance.Files[0].Status);
        Assert.AreEqual("The image is too wide", com.Instance.Files[0].Message);
    }

    [TestMethod]
    public void BitFileUploadShouldLeaveTheDimensionsUnsetForANonImageFile()
    {
        SetupFiles([new() { Name = "notes.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ReadImageDimensions, true);
        });

        SelectFiles(com);

        Assert.IsNull(com.Instance.Files[0].Width);
        Assert.IsNull(com.Instance.Files[0].Height);
    }

    [TestMethod]
    public async Task BitFileUploadShouldCancelAFileThatHasNotStartedYet()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[0].Status);

        await com.InvokeAsync(() => com.Instance.CancelUpload());

        // a cancellation that only gets recorded as an intention would leave the file looking untouched.
        Assert.AreEqual(BitFileUploadStatus.Canceled, com.Instance.Files[0].Status);
        Assert.AreEqual("File upload canceled", com.Find(".bit-upl-us").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotCancelAFileThatAlreadyCompleted()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, "done"));

        await com.InvokeAsync(() => com.Instance.CancelUpload());

        // the bytes are on the server, and canceling on this side cannot take them back.
        Assert.AreEqual(BitFileUploadStatus.Completed, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldMarkTheFilesWaitingForAFreeSlotAsQueued()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ConcurrentUploads, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.IsFalse(com.Instance.Files[0].IsQueued);
        Assert.IsTrue(com.Instance.Files[1].IsQueued);

        // the waiting file says so instead of looking exactly like one nobody asked to upload.
        Assert.AreEqual("Waiting to upload", com.FindAll(".bit-upl-us")[0].TextContent.Trim());

        var buttons = com.FindAll(".bit-upl-itm")[1].QuerySelectorAll(".bit-upl-usi");

        // starting a file that is already on its way would do nothing, so it offers no button for it,
        // but it can still be called off before its turn ever comes.
        Assert.HasCount(1, buttons);
        Assert.AreEqual("Cancel b.txt", buttons[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldStopMarkingAFileAsQueuedOnceItStarts()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ConcurrentUploads, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, string.Empty));

        Assert.IsFalse(com.Instance.Files[1].IsQueued);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[1].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldTakeAQueuedFileOutOfTheQueueWhenItIsCanceled()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 },
                    new() { Name = "c.txt", Size = 100, FileId = "3", Index = 2 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ConcurrentUploads, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.CancelUpload(com.Instance.Files[1]));

        Assert.AreEqual(BitFileUploadStatus.Canceled, com.Instance.Files[1].Status);
        Assert.IsFalse(com.Instance.Files[1].IsQueued);

        // the slot the running file gives back goes to the next file still waiting, not to the canceled one.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, string.Empty));

        Assert.AreEqual(BitFileUploadStatus.Canceled, com.Instance.Files[1].Status);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[2].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldSettleTheBatchWhenTheLastRunningFileIsCanceled()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        BitFileInfo[]? allComplete = null;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.OnAllUploadsComplete, files => allComplete = files);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.CancelUpload());

        Assert.IsNotNull(allComplete);
        Assert.AreEqual(BitFileUploadStatus.Completed, com.Instance.UploadStatus);
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotSettleABatchThatNeverStartedUploading()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var invoked = false;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.OnAllUploadsComplete, _ => invoked = true);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.RemoveFile());

        // a selection taken back before a single byte went out is not an upload that finished.
        Assert.IsFalse(invoked);
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.UploadStatus);
    }

    [TestMethod]
    public async Task BitFileUploadShouldSettleTheBatchWhenTheLastRunningFileIsRemoved()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        BitFileInfo[]? allComplete = null;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.OnAllUploadsComplete, files => allComplete = files);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        Assert.IsNotNull(allComplete);
        Assert.AreEqual(BitFileUploadStatus.Completed, com.Instance.UploadStatus);
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotLeaveAListItemBehindForARemovedTemplatedFile()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.FileViewTemplate, file => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-item");
                builder.AddContent(2, file.Name);
                builder.CloseElement();
            });
        });

        SelectFiles(com);

        Assert.HasCount(2, com.FindAll("[role='listitem']"));

        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        // an empty listitem for a file that is not in the list anymore is noise in the accessibility tree.
        Assert.HasCount(1, com.FindAll("[role='listitem']"));
        Assert.AreEqual("b.txt", com.Find(".custom-item").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitFileUploadShouldExposeThePercentageOfTheProgressBarAsText()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 40));

        Assert.AreEqual("40%", com.Find(".bit-upl-pgb").GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldMeasureTheSpeedAndTheRemainingTimeOfARunningUpload()
    {
        SetupFiles([new() { Name = "a.txt", Size = 1000, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        // the very first reports arrive too close to the start of the request to mean anything.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 100));
        Assert.IsNull(com.Instance.Files[0].UploadSpeed);

        await Task.Delay(300);

        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 500));

        Assert.IsNotNull(com.Instance.Files[0].UploadSpeed);
        Assert.IsGreaterThan(0, com.Instance.Files[0].UploadSpeed!.Value);
        Assert.IsNotNull(com.Instance.Files[0].RemainingTime);
        Assert.IsGreaterThanOrEqualTo(TimeSpan.Zero, com.Instance.Files[0].RemainingTime!.Value);
    }

    [TestMethod]
    public async Task BitFileUploadShouldDropTheSpeedOfAFileThatIsNotRunningAnymore()
    {
        SetupFiles([new() { Name = "a.txt", Size = 1000, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        await Task.Delay(300);

        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 500));

        Assert.IsNotNull(com.Instance.Files[0].UploadSpeed);

        await com.InvokeAsync(() => com.Instance.PauseUpload());

        // a time left that keeps counting down over a transfer that is not running is a lie.
        Assert.IsNull(com.Instance.Files[0].UploadSpeed);
        Assert.IsNull(com.Instance.Files[0].RemainingTime);
    }

    [TestMethod]
    public async Task BitFileUploadShouldInvokeTheUploadUrlProviderForEveryRequest()
    {
        SetupFiles([new() { Name = "a.txt", Size = 2 * 512 * 1024, FileId = "1", Index = 0 }]);

        var calls = 0;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ChunkedUpload, true);
            parameters.Add(p => p.ChunkSize, 512L * 1024);
            parameters.Add(p => p.UploadUrlProvider, () =>
            {
                calls++;
                return Task.FromResult<string?>($"/upload/{calls}");
            });
        });

        SelectFiles(com);

        // nothing is asked of the provider before a request actually needs a URL.
        Assert.AreEqual(0, calls);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.AreEqual(1, calls);

        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, string.Empty));

        // the second chunk gets a URL of its own, which is the whole point of a provider over a fixed URL.
        Assert.AreEqual(2, calls);

        var urls = Context.JSInterop.Invocations["BitBlazorUI.FileUpload.upload"]
                                    .Select(i => i.Arguments[4] as string)
                                    .ToArray();

        Assert.AreEqual("/upload/1", urls[0]);
        Assert.AreEqual("/upload/2", urls[1]);
    }

    [TestMethod]
    public async Task BitFileUploadShouldAppendTheQueryStringsOfTheProviderToTheUrlOfEveryRequest()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadUrl, "/upload");
            parameters.Add(p => p.UploadRequestQueryStrings, new Dictionary<string, string> { { "album", "trip" } });
            parameters.Add(p => p.UploadRequestQueryStringsProvider,
                           () => Task.FromResult(new Dictionary<string, string> { { "token", "fresh" } }));
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        var url = Context.JSInterop.Invocations["BitBlazorUI.FileUpload.upload"].Single().Arguments[4] as string;

        Assert.IsNotNull(url);
        Assert.IsTrue(url.StartsWith("/upload?"));
        Assert.IsTrue(url.Contains("album=trip"));
        Assert.IsTrue(url.Contains("token=fresh"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldSendTheHeadersOfTheProviderWithEveryRequest()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var calls = 0;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadRequestHttpHeadersProvider, () =>
            {
                calls++;
                return Task.FromResult(new Dictionary<string, string> { { "Authorization", $"Bearer {calls}" } });
            });
        });

        SelectFiles(com);

        // the setup call carries no header of the provider, which would otherwise be sent twice
        // and reach the server concatenated into a single comma separated value.
        Assert.AreEqual(0, calls);

        await com.InvokeAsync(() => com.Instance.Upload());

        var headers = Context.JSInterop.Invocations["BitBlazorUI.FileUpload.upload"]
                                       .Single().Arguments[5] as Dictionary<string, string>;

        Assert.IsNotNull(headers);
        Assert.AreEqual("Bearer 1", headers["Authorization"]);
    }

    [TestMethod]
    public async Task BitFileUploadShouldLetTheHeadersOfAFileWinOverTheOnesOfTheProvider()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadRequestHttpHeadersProvider,
                           () => Task.FromResult(new Dictionary<string, string> { { "x-scope", "component" }, { "x-shared", "yes" } }));
            parameters.Add(p => p.OnUploading,
                           (BitFileInfo file) => file.HttpHeaders = new Dictionary<string, string> { { "x-scope", "file" } });
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        var headers = Context.JSInterop.Invocations["BitBlazorUI.FileUpload.upload"]
                                       .Single().Arguments[5] as Dictionary<string, string>;

        Assert.IsNotNull(headers);
        Assert.AreEqual("file", headers["x-scope"]);
        Assert.AreEqual("yes", headers["x-shared"]);
    }

    [TestMethod]
    public async Task BitFileUploadShouldSendTheFormFieldsWithTheUploadRequest()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.UploadRequestFormFields, new Dictionary<string, string> { { "folder", "invoices" }, { "note", "batch" } });
            parameters.Add(p => p.OnUploading,
                           (BitFileInfo file) => file.FormFields = new Dictionary<string, string> { { "note", "single" } });
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());

        var fields = Context.JSInterop.Invocations["BitBlazorUI.FileUpload.upload"]
                                      .Single().Arguments[6] as Dictionary<string, string>;

        Assert.IsNotNull(fields);
        Assert.AreEqual("invoices", fields["folder"]);
        // the fields of the file are laid over the ones of the whole component.
        Assert.AreEqual("single", fields["note"]);
    }

    [TestMethod]
    public async Task BitFileUploadShouldRemoveAnUploadedFileFromTheServer()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "file-1", Index = 0 }]);

        var handler = SetupHttpClient(HttpStatusCode.OK);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RemoveUrl, "https://localhost/remove");
            parameters.Add(p => p.ShowRemoveButton, true);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, "done"));
        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        Assert.IsNotNull(handler.LastRequest);
        Assert.AreEqual(HttpMethod.Delete, handler.LastRequest.Method);
        Assert.IsTrue(handler.LastRequest.RequestUri!.ToString().Contains("fileName=a.txt"));
        Assert.AreEqual("file-1", handler.LastRequest.Headers.GetValues("BIT_FILE_ID").Single());
        Assert.AreEqual(BitFileUploadStatus.Removed, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldUseTheCustomHttpMethodOfTheRemoveRequest()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "file-1", Index = 0 }]);

        var handler = SetupHttpClient(HttpStatusCode.OK);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RemoveUrl, "https://localhost/remove");
            parameters.Add(p => p.RemoveRequestHttpMethod, "POST");
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, "done"));
        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        Assert.IsNotNull(handler.LastRequest);
        Assert.AreEqual(HttpMethod.Post, handler.LastRequest.Method);
    }

    [TestMethod]
    public async Task BitFileUploadShouldReportAFailedRemoval()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "file-1", Index = 0 }]);

        SetupHttpClient(HttpStatusCode.Forbidden);

        BitFileInfo? removeFailed = null;

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.RemoveUrl, "https://localhost/remove");
            parameters.Add(p => p.OnRemoveFailed, file => removeFailed = file);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, "done"));
        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        // a removal the server refused must not pretend the file is gone.
        Assert.IsNotNull(removeFailed);
        Assert.AreEqual(BitFileUploadStatus.RemoveFailed, com.Instance.Files[0].Status);
        Assert.AreEqual("File remove failed", com.Find(".bit-upl-us").TextContent.Trim());
    }

    [TestMethod]
    public void BitFileUploadShouldNotShowAPercentageForARejectedFile()
    {
        SetupFiles([new() { Name = "big.txt", Size = 9999, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.MaxSize, 1024);
        });

        SelectFiles(com);

        // a file that was never going to be sent has no progress, and a "0%" next to the reason it
        // was turned away only reads as something that failed halfway through.
        Assert.IsEmpty(com.FindAll(".bit-upl-pct"));
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotReportAnEmptyFileAsUploadedBeforeItIsSent()
    {
        SetupFiles([new() { Name = "empty.txt", Size = 0, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        Assert.AreEqual("0%", com.Find(".bit-upl-pct").TextContent.Trim());

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, string.Empty));

        Assert.AreEqual(BitFileUploadStatus.Completed, com.Instance.Files[0].Status);
        Assert.AreEqual("100%", com.Find(".bit-upl-pct").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotRepaintTheFileListOnEveryProgressReport()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 40));

        Assert.AreEqual("40%", com.Find(".bit-upl-pct").TextContent.Trim());

        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 80));

        // a browser reports the progress of a request many times a second; repainting the whole list on
        // each of them would cost more than the upload itself, so the repaints are spaced out.
        Assert.AreEqual(80, com.Instance.Files[0].LastChunkUploadedSize);
        Assert.AreEqual("40%", com.Find(".bit-upl-pct").TextContent.Trim());

        await Task.Delay(400);

        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 80));

        Assert.AreEqual("80%", com.Find(".bit-upl-pct").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitFileUploadShouldAlwaysRepaintWhenAFileSettles()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 40));
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 90));

        // the spacing of the progress repaints must never leave the settled state unpainted.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, "done"));

        Assert.AreEqual("100%", com.Find(".bit-upl-pct").TextContent.Trim());
        Assert.AreEqual("File upload succeeded", com.Find(".bit-upl-us").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitFileUploadShouldNotStartASecondRequestForAFileAlreadyOnTheWire()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 60));

        // pressing an upload-everything button a second time would otherwise take the connection away
        // from the running request and start the file over from its very first byte.
        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.FileUpload.upload"]);
        Assert.AreEqual(60, com.Instance.Files[0].LastChunkUploadedSize);
    }

    [TestMethod]
    public async Task BitFileUploadShouldStartAFileAgainOnceItsRequestIsOver()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        Assert.AreEqual(BitFileUploadStatus.Failed, com.Instance.Files[0].Status);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.HasCount(2, Context.JSInterop.Invocations["BitBlazorUI.FileUpload.upload"]);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldStartAPausedFileAgainAfterItsRequestWasAborted()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.PauseUpload());

        // the abort leaves no response behind to free the file up, so pausing has to do it itself.
        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.HasCount(2, Context.JSInterop.Invocations["BitBlazorUI.FileUpload.upload"]);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldReleaseWhatTheBrowserHoldsForARemovedFile()
    {
        SetupFiles([new() { Name = "photo.png", Size = 100, FileId = "1", Index = 0, PreviewUrl = "blob:preview" }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.ShowPreview, true);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.RemoveFile(com.Instance.Files[0]));

        // the picked file would otherwise stay in the memory of the browser for the whole life of the page.
        Context.JSInterop.VerifyInvoke("BitBlazorUI.FileUpload.release");
        Assert.IsNull(com.Instance.Files[0].PreviewUrl);
    }

    [TestMethod]
    public async Task BitFileUploadShouldReportTheSpeedAndTheRemainingTimeOfTheWholeBatch()
    {
        SetupFiles([new() { Name = "a.txt", Size = 1000, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 1000, FileId = "2", Index = 1 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
        });

        SelectFiles(com);

        Assert.IsNull(com.Instance.TotalUploadSpeed);
        Assert.IsNull(com.Instance.OverallRemainingTime);

        await com.InvokeAsync(() => com.Instance.Upload());

        await Task.Delay(300);

        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(0, 400));
        await com.InvokeAsync(() => com.Instance.__HandleChunkUploadProgress(1, 200));

        var first = com.Instance.Files[0].UploadSpeed!.Value;
        var second = com.Instance.Files[1].UploadSpeed!.Value;

        Assert.IsNotNull(com.Instance.TotalUploadSpeed);
        Assert.AreEqual(first + second, com.Instance.TotalUploadSpeed!.Value, 0.001);
        Assert.IsNotNull(com.Instance.OverallRemainingTime);
    }

    [TestMethod]
    public async Task BitFileUploadShouldLeaveAFileThatNeverStartedAloneOnPause()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.PauseUpload());

        // there is nothing to pause about a file nobody asked to upload, and putting it in a paused
        // state would only have turned its own upload button into one that pauses it again.
        Assert.AreEqual(BitFileUploadStatus.Pending, com.Instance.Files[0].Status);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldTakeAQueuedFileOutOfTheQueueOnPause()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 },
                    new() { Name = "b.txt", Size = 100, FileId = "2", Index = 1 },
                    new() { Name = "c.txt", Size = 100, FileId = "3", Index = 2 }]);

        var com = RenderComponent<BitFileUpload>(parameters =>
        {
            parameters.Add(p => p.Multiple, true);
            parameters.Add(p => p.ConcurrentUploads, 1);
        });

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.PauseUpload(com.Instance.Files[1]));

        Assert.AreEqual(BitFileUploadStatus.Paused, com.Instance.Files[1].Status);
        Assert.IsFalse(com.Instance.Files[1].IsQueued);

        // the slot the running file gives back skips over the paused one to the file behind it.
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 200, string.Empty));

        Assert.AreEqual(BitFileUploadStatus.Paused, com.Instance.Files[1].Status);
        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[2].Status);
    }

    [TestMethod]
    public async Task BitFileUploadShouldKeepTheReasonAFailedFileFailedWhenEverythingIsCanceled()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.__HandleChunkUpload(0, 500, "boom"));

        await com.InvokeAsync(() => com.Instance.CancelUpload());

        // calling off a file that already failed would only replace its reason with a less useful one.
        Assert.AreEqual(BitFileUploadStatus.Failed, com.Instance.Files[0].Status);
        Assert.AreEqual("boom", com.Instance.Files[0].Message);
    }

    [TestMethod]
    public async Task BitFileUploadShouldCancelAPausedFileAndLetItBeStartedAgain()
    {
        SetupFiles([new() { Name = "a.txt", Size = 100, FileId = "1", Index = 0 }]);

        var com = RenderComponent<BitFileUpload>();

        SelectFiles(com);

        await com.InvokeAsync(() => com.Instance.Upload());
        await com.InvokeAsync(() => com.Instance.PauseUpload());
        await com.InvokeAsync(() => com.Instance.CancelUpload());

        Assert.AreEqual(BitFileUploadStatus.Canceled, com.Instance.Files[0].Status);

        await com.InvokeAsync(() => com.Instance.Upload());

        Assert.AreEqual(BitFileUploadStatus.InProgress, com.Instance.Files[0].Status);
    }

    private void SetupFiles(BitFileInfo[] files)
    {
        Context.JSInterop.Setup<BitFileInfo[]>("BitBlazorUI.FileUpload.setup", _ => true).SetResult(files);
    }

    private FakeHttpMessageHandler SetupHttpClient(HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(statusCode);

        Context.Services.AddSingleton(new HttpClient(handler));

        return handler;
    }

    private static void SelectFiles(IRenderedComponent<BitFileUpload> com)
    {
        com.Find(".bit-upl-fi").Change(string.Empty);
    }

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
