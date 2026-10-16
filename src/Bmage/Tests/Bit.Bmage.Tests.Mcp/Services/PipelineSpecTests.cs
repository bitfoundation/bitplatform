using System.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>
/// The pipeline reader: a fluent chain written the way it is written in C# becomes the real
/// ImagePipeline - every builder method, with its real parameters - and anything else comes back as
/// an explanation an agent can correct, never as an exception.
/// </summary>
[TestClass]
public class PipelineSpecTests
{
    [TestMethod]
    [DataRow("ImagePipeline.Create().AutoOrient().MaxSize(1920, 1920).Sharpen(0.3f)")]
    [DataRow("AutoOrient().MaxSize(1920, 1920).Sharpen(0.3f)")]
    [DataRow(".AutoOrient()\n.MaxSize(1920, 1920)\n.Sharpen(0.3f);")]
    [DataRow("var pipeline = ImagePipeline.Create()\n    .AutoOrient()            // upright first\n    .MaxSize(1920, 1920)\n    .Sharpen(0.3f);")]
    [DataRow("ImagePipeline.Empty.AutoOrient().MaxSize(1920, 1920).Sharpen(amount: 0.3f)")]
    public void Parse_TheWaysAPipelineIsWritten_AllBuildTheSamePipeline(string spec)
    {
        var result = BmagePipelineSpec.Parse(spec);

        Assert.IsNull(result.Error, result.Error);
        Assert.IsNotNull(result.Pipeline);
        Assert.AreEqual("Auto orient → Resize(1920x1920, Max) → Sharpen", result.Pipeline.ToString());
    }

    [TestMethod]
    public void Parse_EnumsNamedArgumentsAndDefaults_BindToTheRealParameters()
    {
        var result = BmagePipelineSpec.Parse("Cover(512, 512, Anchor.Top).Resize(400, null, mode: ResizeMode.Max, filter: ResamplingFilter.Lanczos3)");

        Assert.IsNull(result.Error, result.Error);

        var cover = (ResizeOperation)result.Pipeline![0];
        Assert.AreEqual(ResizeMode.Cover, cover.Mode);
        Assert.AreEqual(Anchor.Top, cover.Anchor);

        var resize = (ResizeOperation)result.Pipeline[1];
        Assert.AreEqual(ResizeMode.Max, resize.Mode);
        Assert.AreEqual(ResamplingFilter.Lanczos3, resize.Filter);
        Assert.IsNull(resize.Height);
    }

    [TestMethod]
    public void Parse_TheValueTypesAPipelineTakes_AreAllReadable()
    {
        var result = BmagePipelineSpec.Parse(
            "SmartCrop(AspectRatio.Square)" +
            ".CropToAspect(\"16:9\")" +
            ".Pixelate(14, new Rectangle(40, 120, 300, 40))" +
            ".Rotate(12.5, Rgba32.Transparent)" +
            ".Resize(400, 260, ResizeMode.Contain, background: \"#18181f\")" +
            ".Skew(10, 0, new Rgba32(24, 26, 31))" +
            ".Crop(0, 0, 100, 100)" +
            ".Orient(Orientation.Rotate90)");

        Assert.IsNull(result.Error, result.Error);
        Assert.AreEqual(8, result.Pipeline!.Count);

        Assert.AreEqual(new Rectangle(40, 120, 300, 40), ((PixelateFilter)result.Pipeline[2]).Region);
        Assert.AreEqual(Rgba32.ParseHex("#18181f"), ((ResizeOperation)result.Pipeline[4]).Background);
    }

    [TestMethod]
    public void Parse_OverloadsAreChosenByTheArguments()
    {
        var byInts = BmagePipelineSpec.Parse("Crop(1, 2, 30, 40)");
        var byRectangle = BmagePipelineSpec.Parse("Crop(new System.Drawing.Rectangle(1, 2, 30, 40))");

        Assert.IsNull(byInts.Error, byInts.Error);
        Assert.IsNull(byRectangle.Error, byRectangle.Error);
        Assert.AreEqual(byInts.Pipeline!.GetOutputSize(new Size(100, 100)), byRectangle.Pipeline!.GetOutputSize(new Size(100, 100)));
    }

    [TestMethod]
    public void Parse_TheCanonicalForm_IsTheCodeThatBuildsIt()
    {
        var result = BmagePipelineSpec.Parse("autoorient().maxsize(1920,1920)");

        Assert.AreEqual("ImagePipeline.Create()\n    .AutoOrient()\n    .MaxSize(1920, 1920)", result.Canonical);
    }

    [TestMethod]
    public void Parse_TheCallsThatRunThePipeline_AreIgnored_AndSaidToBe()
    {
        var result = BmagePipelineSpec.Parse("ImagePipeline.Create().MaxSize(100, 100).ExecuteAsync(image)");

        Assert.IsNull(result.Error, result.Error);
        Assert.AreEqual(1, result.Pipeline!.Count);
        StringAssert.Contains(result.Warnings.Single(), "ExecuteAsync");
    }

    [TestMethod]
    [DataRow("", "empty")]
    [DataRow("Teleport(3)", "no method called 'Teleport'")]
    [DataRow("MaxSize(1920)", "'maxHeight' is required")]
    [DataRow("MaxSize(1920, 1920, 3, 4)", "at most")]
    [DataRow("MaxSize(19.5, 1920)", "not a whole number")]
    [DataRow("Cover(10, 10, Anchor.Sideways)", "Anchor has no member 'Sideways'")]
    [DataRow("Cover(10, 10, ResizeMode.Max)", "is not a Anchor")]
    [DataRow("Resize(10, 10, colour: Rgba32.White)", "no parameter called 'colour'")]
    [DataRow("Apply(new Duotone())", "cannot construct")]
    [DataRow("ColorMatrix(matrix)", "cannot construct")]
    [DataRow("MaxSize(10, 10", "not closed")]
    [DataRow("MaxSize(10, 10) junk", "Expected '('")]
    [DataRow("Posterize(1)", "rejected its arguments")]
    [DataRow("SmartCrop(AspectRatio.Golden)", "not one of AspectRatio's presets")]
    public void Parse_WhatCannotBeRead_IsExplained_NeverThrown(string spec, string expected)
    {
        var result = BmagePipelineSpec.Parse(spec);

        Assert.IsNull(result.Pipeline);
        Assert.IsNotNull(result.Error);
        StringAssert.Contains(result.Error, expected);
    }

    [TestMethod]
    public void Parse_AnUnknownMethod_ListsTheOnesThatExist()
    {
        var error = BmagePipelineSpec.Parse("Teleport(3)").Error!;

        foreach (var name in new[] { "AutoOrient", "MaxSize", "Cover", "SmartCrop", "Sharpen", "Pixelate" })
        {
            StringAssert.Contains(error, name);
        }

        Assert.IsFalse(error.Contains("AddRange", StringComparison.Ordinal), "A builder the spec cannot call is offered as one it can.");
    }

    [TestMethod]
    public void Parse_AnOverlongSpec_IsRefused()
    {
        var result = BmagePipelineSpec.Parse(string.Concat(Enumerable.Repeat(".Brightness(0)", 400)));

        StringAssert.Contains(result.Error, "at most");
    }
}
