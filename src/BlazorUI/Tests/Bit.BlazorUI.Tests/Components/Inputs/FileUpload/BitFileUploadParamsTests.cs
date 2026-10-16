using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileUpload;

/// <summary>
/// Covers the shape of the BitParams cascade of the FileUpload: that a BitFileUploadParams carries every parameter
/// that is not per component and nothing the component does not have, that the parameters the main tests leave
/// out - the texts, the icons, the titles, the request settings and their providers - reach the instance, and what
/// an empty or a dropped params object leaves behind. What the cascade does to the markup is pinned in BitFileUploadTests.
/// </summary>
[TestClass]
public class BitFileUploadParamsTests : BunitTestContext
{
    // What belongs to one file upload alone - its templates, its callbacks and the record its preloaded files belong
    // to - is written per component, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitFileUpload.CascadingParameters),
        nameof(BitFileUpload.DescriptionTemplate),
        nameof(BitFileUpload.FileViewTemplate),
        nameof(BitFileUpload.LabelTemplate),
        nameof(BitFileUpload.PreloadedFiles),
        nameof(BitFileUpload.OnAllUploadsComplete),
        nameof(BitFileUpload.OnFilesChange),
        nameof(BitFileUpload.OnInvalid),
        nameof(BitFileUpload.OnProgress),
        nameof(BitFileUpload.OnRemoveComplete),
        nameof(BitFileUpload.OnRemoveFailed),
        nameof(BitFileUpload.OnUploading),
        nameof(BitFileUpload.OnUploadComplete),
        nameof(BitFileUpload.OnUploadFailed),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitFileUploadParams uploadParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { uploadParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFileUpload>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitFileUploadParamsShouldBeAnInputParamsObject()
    {
        var @params = new BitFileUploadParams();

        Assert.IsInstanceOfType<BitInputBaseParams<string?>>(@params);
        Assert.AreEqual("BitParams.BitFileUpload", @params.Name);
    }

    [TestMethod]
    public void BitFileUploadParamsShouldCarryEveryParameterThatIsNotPerComponent()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name)
                                                     .Concat(typeof(BitInputBase<string?>).GetProperties().Select(p => p.Name))
                                                     .Where(n => n is not (nameof(BitFileUpload.ReadOnly) or nameof(BitFileUpload.Required)))
                                                     .ToHashSet();

        var parameters = typeof(BitFileUpload).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                              .Select(p => p.Name)
                                              .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitFileUploadParams).GetProperty(name), $"BitFileUploadParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitFileUploadParamsShouldNameNothingTheComponentDoesNotHave()
    {
        var componentParameters = typeof(BitFileUpload).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                       .Select(p => p.Name)
                                                       .ToHashSet();

        var paramsProperties = typeof(BitFileUploadParams).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                          .Where(p => p.CanWrite)
                                                          .Select(p => p.Name);

        foreach (var name in paramsProperties)
        {
            Assert.IsTrue(componentParameters.Contains(name), $"BitFileUploadParams.{name} is no parameter of the component.");
        }
    }

    [TestMethod]
    public void BitFileUploadParamsUpdateParametersShouldSetTheTextsTheIconsAndTheRequestSettings()
    {
        static string? announce(IReadOnlyList<BitFileInfo> files) => "announced";
        static TimeSpan? delay(BitFileInfo file, int attempt) => TimeSpan.Zero;
        static string format(long size) => size.ToString();
        static string? validate(BitFileInfo file) => null;
        static bool retry(BitFileInfo file, int status) => true;
        static Task<Dictionary<string, string>> entries() => Task.FromResult(new Dictionary<string, string>());
        static Task<string?> url() => Task.FromResult<string?>("/u");

        var @params = new BitFileUploadParams
        {
            AnnouncementProvider = announce,
            AutoRetryDelayProvider = delay,
            CancelButtonTitle = "Call off",
            CancelAllText = "Call all off",
            CancelIcon = BitIconInfo.Bi("x"),
            CancelIconName = "Clear",
            CanceledUploadMessage = "Called off",
            ChunkSize = 1024L * 1024,
            Classes = new() { Root = "cascaded-root" },
            ClearText = "Wipe",
            DropZoneSelector = "#zone",
            DuplicateErrorMessage = "Twice",
            FailedRemoveMessage = "Still there",
            FailedUploadMessage = "Broke",
            FileSizeFormatter = format,
            FileValidator = validate,
            LabelIcon = BitIconInfo.Bi("cloud"),
            LabelIconName = "CloudUpload",
            LabelIconPlacement = BitPlacement.End,
            MaxCountErrorMessage = "Too many",
            MaxSizeErrorMessage = "Too big",
            MaxTotalSizeErrorMessage = "Too much",
            MinSizeErrorMessage = "Too small",
            NotAllowedExtensionErrorMessage = "Wrong kind",
            PauseButtonTitle = "Hold",
            PauseIcon = BitIconInfo.Bi("pause"),
            PauseIconName = "Stop",
            PreloadedFileMessage = "On file",
            QueuedUploadMessage = "In line",
            RemoveButtonTitle = "Drop",
            RemoveIcon = BitIconInfo.Bi("trash"),
            RemoveIconName = "Delete",
            RemoveRequestHttpHeaders = new Dictionary<string, string> { ["x-remove"] = "1" },
            RemoveRequestHttpHeadersProvider = entries,
            RemoveRequestHttpMethod = "POST",
            RemoveRequestQueryStrings = new Dictionary<string, string> { ["album"] = "trip" },
            RemoveRequestQueryStringsProvider = entries,
            RetryButtonTitle = "Again",
            RetryIcon = BitIconInfo.Bi("arrow-repeat"),
            RetryIconName = "Refresh",
            ShouldAutoRetry = retry,
            ShowBatchActions = true,
            ShowDropArea = true,
            Styles = new() { Root = "--s: root;" },
            SuccessfulUploadMessage = "Landed",
            UploadAllText = "Send all",
            UploadButtonTitle = "Send",
            UploadFormFieldName = "payload",
            UploadIcon = BitIconInfo.Bi("upload"),
            UploadIconName = "Play",
            UploadRequestFormFields = new Dictionary<string, string> { ["folder"] = "invoices" },
            UploadRequestHttpHeaders = new Dictionary<string, string> { ["x-upload"] = "1" },
            UploadRequestHttpHeadersProvider = entries,
            UploadRequestHttpMethod = "PUT",
            UploadRequestQueryStrings = new Dictionary<string, string> { ["token"] = "t" },
            UploadRequestQueryStringsProvider = entries,
            UploadUrlProvider = url,
            ReadOnly = true,
            Required = true,
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitFileUpload>().Instance;

        Assert.AreEqual((Func<IReadOnlyList<BitFileInfo>, string?>)announce, instance.AnnouncementProvider);
        Assert.AreEqual((Func<BitFileInfo, int, TimeSpan?>)delay, instance.AutoRetryDelayProvider);
        Assert.AreEqual("Call off", instance.CancelButtonTitle);
        Assert.AreEqual("Call all off", instance.CancelAllText);
        Assert.AreEqual(@params.CancelIcon, instance.CancelIcon);
        Assert.AreEqual("Clear", instance.CancelIconName);
        Assert.AreEqual("Called off", instance.CanceledUploadMessage);
        Assert.AreEqual(1024L * 1024, instance.ChunkSize);
        Assert.AreSame(@params.Classes, instance.Classes);
        Assert.AreEqual("Wipe", instance.ClearText);
        Assert.AreEqual("#zone", instance.DropZoneSelector);
        Assert.AreEqual("Twice", instance.DuplicateErrorMessage);
        Assert.AreEqual("Still there", instance.FailedRemoveMessage);
        Assert.AreEqual("Broke", instance.FailedUploadMessage);
        Assert.AreEqual((Func<long, string>)format, instance.FileSizeFormatter);
        Assert.AreEqual((Func<BitFileInfo, string?>)validate, instance.FileValidator);
        Assert.AreEqual(@params.LabelIcon, instance.LabelIcon);
        Assert.AreEqual("CloudUpload", instance.LabelIconName);
        Assert.AreEqual(BitPlacement.End, instance.LabelIconPlacement);
        Assert.AreEqual("Too many", instance.MaxCountErrorMessage);
        Assert.AreEqual("Too big", instance.MaxSizeErrorMessage);
        Assert.AreEqual("Too much", instance.MaxTotalSizeErrorMessage);
        Assert.AreEqual("Too small", instance.MinSizeErrorMessage);
        Assert.AreEqual("Wrong kind", instance.NotAllowedExtensionErrorMessage);
        Assert.AreEqual("Hold", instance.PauseButtonTitle);
        Assert.AreEqual(@params.PauseIcon, instance.PauseIcon);
        Assert.AreEqual("Stop", instance.PauseIconName);
        Assert.AreEqual("On file", instance.PreloadedFileMessage);
        Assert.AreEqual("In line", instance.QueuedUploadMessage);
        Assert.AreEqual("Drop", instance.RemoveButtonTitle);
        Assert.AreEqual(@params.RemoveIcon, instance.RemoveIcon);
        Assert.AreEqual("Delete", instance.RemoveIconName);
        Assert.AreSame(@params.RemoveRequestHttpHeaders, instance.RemoveRequestHttpHeaders);
        Assert.AreEqual((Func<Task<Dictionary<string, string>>>)entries, instance.RemoveRequestHttpHeadersProvider);
        Assert.AreEqual("POST", instance.RemoveRequestHttpMethod);
        Assert.AreSame(@params.RemoveRequestQueryStrings, instance.RemoveRequestQueryStrings);
        Assert.AreEqual((Func<Task<Dictionary<string, string>>>)entries, instance.RemoveRequestQueryStringsProvider);
        Assert.AreEqual("Again", instance.RetryButtonTitle);
        Assert.AreEqual(@params.RetryIcon, instance.RetryIcon);
        Assert.AreEqual("Refresh", instance.RetryIconName);
        Assert.AreEqual((Func<BitFileInfo, int, bool>)retry, instance.ShouldAutoRetry);
        Assert.IsTrue(instance.ShowBatchActions);
        Assert.IsTrue(instance.ShowDropArea);
        Assert.AreSame(@params.Styles, instance.Styles);
        Assert.AreEqual("Landed", instance.SuccessfulUploadMessage);
        Assert.AreEqual("Send all", instance.UploadAllText);
        Assert.AreEqual("Send", instance.UploadButtonTitle);
        Assert.AreEqual("payload", instance.UploadFormFieldName);
        Assert.AreEqual(@params.UploadIcon, instance.UploadIcon);
        Assert.AreEqual("Play", instance.UploadIconName);
        Assert.AreSame(@params.UploadRequestFormFields, instance.UploadRequestFormFields);
        Assert.AreSame(@params.UploadRequestHttpHeaders, instance.UploadRequestHttpHeaders);
        Assert.AreEqual((Func<Task<Dictionary<string, string>>>)entries, instance.UploadRequestHttpHeadersProvider);
        Assert.AreEqual("PUT", instance.UploadRequestHttpMethod);
        Assert.AreSame(@params.UploadRequestQueryStrings, instance.UploadRequestQueryStrings);
        Assert.AreEqual((Func<Task<Dictionary<string, string>>>)entries, instance.UploadRequestQueryStringsProvider);
        Assert.AreEqual((Func<Task<string?>>)url, instance.UploadUrlProvider);
        Assert.IsTrue(instance.ReadOnly);
        Assert.IsTrue(instance.Required);
    }

    [TestMethod]
    public void BitFileUploadParamsUpdateParametersShouldIgnoreANullComponent()
    {
        var @params = new BitFileUploadParams { Color = BitColor.Success };

        @params.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitFileUploadShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitFileUploadParams
        {
            Dir = BitDir.Rtl,
            Class = "cascaded",
            Style = "margin: 1px;",
            Id = "cascaded-id",
            HtmlAttributes = new Dictionary<string, object> { ["data-cascaded"] = "yes", ["data-both"] = "from-cascade" },
        }, builder =>
        {
            builder.AddAttribute(1, "data-both", "from-instance");
        });

        var root = component.Find(".bit-upl");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        Assert.AreEqual("cascaded-id", root.Id);
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
        // What the instance wrote for itself is never replaced by the cascade.
        Assert.AreEqual("from-instance", root.GetAttribute("data-both"));
    }

    [TestMethod]
    public void BitFileUploadShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitFileUploadParams());

        var root = component.Find(".bit-upl");

        // Nothing cascades, so Color and Size stay unset and publish nothing, and the Variant keeps its default.
        foreach (var cssClass in new[] { "bit-upl-pri", "bit-upl-md", "bit-upl-dra", "bit-upl-eni", "bit-upl-rdl" })
        {
            Assert.IsFalse(root.ClassList.Contains(cssClass), $"An empty params object published {cssClass}.");
        }

        Assert.IsTrue(root.ClassList.Contains("bit-upl-fil"));
        Assert.AreEqual("Browse", component.Find(".bit-upl-lbl").TextContent.Trim());
    }

    [TestMethod]
    public void TheValuesOfADroppedParamsObjectShouldGo()
    {
        var component = RenderWithParams(new BitFileUploadParams { ShowDropArea = true, Label = "Cascaded label", ReadOnly = true });

        Assert.IsTrue(component.Find(".bit-upl").ClassList.Contains("bit-upl-dra"));
        Assert.IsTrue(component.Find(".bit-upl").ClassList.Contains("bit-upl-rdl"));
        Assert.AreEqual("Cascaded label", component.Find(".bit-upl-lbl").TextContent.Trim());

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitFileUploadParams { Size = BitSize.Large } }));

        var root = component.Find(".bit-upl");

        Assert.IsFalse(root.ClassList.Contains("bit-upl-dra"));
        Assert.IsFalse(root.ClassList.Contains("bit-upl-rdl"));
        Assert.IsTrue(root.ClassList.Contains("bit-upl-lg"));
        Assert.AreEqual("Browse", component.Find(".bit-upl-lbl").TextContent.Trim());
    }
}
