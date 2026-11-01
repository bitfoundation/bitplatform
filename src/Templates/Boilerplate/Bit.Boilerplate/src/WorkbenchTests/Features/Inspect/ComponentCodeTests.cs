namespace Boilerplate.WorkbenchTests.Features.Inspect;

/// <summary>
/// Inspect mode's panel writes parameter changes into a component's tag as text, so the tags have to be found and read
/// exactly, plain values told from code, and every value written back the way a page's author would write it.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ComponentCodeTests
{
    private const string Markup = """
        @* <BitButton Color="BitColor.Error" /> in a comment is no tag *@
        <BitButtonGroup Items="items" />
        <BitButton IconOnly Color="BitColor.Error" IconName="@BitIconName.Delete"
                   Title="@Localizer[nameof(AppStrings.Delete)]"
                   Styles="@(new() { Root = "color: red", Icon = "margin: 0 \"x\"" })"
                   OnClick="() => Remove(item)" />
        <BitButton>@("a > b")</BitButton>
        @code { private List<BitButton> buttons = []; }
        """;

    [TestMethod]
    public void TheTagsOfAComponent_Should_BeFoundOutsideCommentsAndCode()
    {
        var starts = WorkbenchRazorTags.Find(Markup, "BitButton");

        Assert.HasCount(2, starts);
        Assert.StartsWith("<BitButton IconOnly", Markup[starts[0]..]);
        Assert.StartsWith("<BitButton>", Markup[starts[1]..]);
    }

    [TestMethod]
    public void ATagsAttributes_Should_BeReadWithTheirQuotesAndCodeStepped()
    {
        var start = WorkbenchRazorTags.Find(Markup, "BitButton")[0];
        var (attributes, _) = WorkbenchRazorTags.Parse(Markup, start, "BitButton")!.Value;

        Assert.AreSequenceEqual(new[] { "IconOnly", "Color", "IconName", "Title", "Styles", "OnClick" }, attributes.Select(a => a.Name).ToArray());
        Assert.IsNull(attributes[0].Value);
        Assert.AreEqual("@(new() { Root = \"color: red\", Icon = \"margin: 0 \\\"x\\\"\" })", attributes[4].Value);
        Assert.AreEqual("() => Remove(item)", attributes[5].Value);
    }

    [TestMethod]
    [DataRow(typeof(bool), null, "true")]
    [DataRow(typeof(bool), "false", "false")]
    [DataRow(typeof(BitColor?), "BitColor.Error", "Error")]
    [DataRow(typeof(BitColor?), "@BitColor.Warning", "Warning")]
    [DataRow(typeof(int), "20", "20")]
    [DataRow(typeof(string), "search", "search")]
    [DataRow(typeof(string), "a@@b.com", "a@b.com")]
    [DataRow(typeof(string), "@(\"He said \\\"hi\\\"\")", "He said \"hi\"")]
    [DataRow(typeof(string), "@BitIconName.Delete", "Delete")]
    public void APlainValue_Should_BeReadAsThePanelShowsIt(Type type, string? raw, string expected)
    {
        Assert.IsTrue(WorkbenchRazorValues.TryRead(type, raw, out var value));
        Assert.AreEqual(expected, value);
    }

    [TestMethod]
    [DataRow(typeof(string), "@Localizer[nameof(AppStrings.Delete)]")]
    [DataRow(typeof(bool), "isNarrow is false")]
    [DataRow(typeof(BitColor?), "@(isOn ? BitColor.Error : BitColor.Info)")]
    [DataRow(typeof(BitColor?), "BitSize.Small")]
    [DataRow(typeof(int), "pageSize")]
    public void Code_Should_NotBeReadAsAPlainValue(Type type, string raw)
    {
        Assert.IsFalse(WorkbenchRazorValues.TryRead(type, raw, out _));
    }

    [TestMethod]
    public void TheParts_Should_BeReadOnlyWhenEveryOneIsAPlainString()
    {
        var parts = WorkbenchRazorValues.TryReadParts("@(new() { Root = \"color: red\", Icon = \"margin: 0\" })");

        Assert.IsNotNull(parts);
        Assert.AreEqual("color: red", parts["Root"]);
        Assert.AreEqual("margin: 0", parts["Icon"]);
        Assert.IsNull(WorkbenchRazorValues.TryReadParts("@(new() { Root = rootStyle })"));
        Assert.IsNull(WorkbenchRazorValues.TryReadParts("@styles"));
    }

    [TestMethod]
    public void AValue_Should_BeWrittenTheWayAPageWritesIt()
    {
        var button = typeof(BitButton);
        string? Attribute(string parameter, string value) => WorkbenchRazorValues.Attribute(WorkbenchComponentParameters.ParameterOf(button, parameter)!, value);

        Assert.AreEqual("IconOnly", Attribute("IconOnly", "true"));
        Assert.AreEqual("IconOnly=\"false\"", Attribute("IconOnly", "false"));
        Assert.AreEqual("Color=\"BitColor.Warning\"", Attribute("Color", "Warning"));
        Assert.AreEqual("IconName=\"@BitIconName.RecycleBin\"", Attribute("IconName", "RecycleBin"));
        Assert.AreEqual("Title=\"Remove it\"", Attribute("Title", "Remove it"));
        Assert.AreEqual("Title=\"@(\"Say \\\"hi\\\"\")\"", Attribute("Title", "Say \"hi\""));
        Assert.IsNull(Attribute("Color", "Purple"));
        Assert.IsNull(Attribute("LoadingDelay", "soon"));
    }

    [TestMethod]
    public void TheIconSearch_Should_RankLikeTheDemosIconPage()
    {
        Assert.AreEqual("Delete", WorkbenchIconSearch.Search("delete").First());
        Assert.Contains("Delete", WorkbenchIconSearch.Search("trash").Take(5).ToArray());
        Assert.AreEqual("Settings", WorkbenchIconSearch.Search("gear").First());
        Assert.HasCount(WorkbenchIconSearch.Count, WorkbenchIconSearch.Search(null));
        Assert.AreEqual("_12PointStar", WorkbenchIconSearch.FieldOf("12PointStar"));
    }

    [TestMethod]
    [DataRow("--bit-btn-icn-margintop", "margin")]
    [DataRow("--bit-btn-padding", "spacing")]
    [DataRow("--bit-btn-lbl-fontsize", "font-size")]
    [DataRow("--bit-btn-min-height", "size")]
    [DataRow("--bit-txt-letter-spacing", "letter-spacing")]
    [DataRow("--bit-txt-line-height", "line-height")]
    [DataRow("--bit-txt-text-transform", "text-transform")]
    [DataRow("Height", "size")]
    [DataRow("Placeholder", null)]
    public void AName_Should_PickItsSuggestions(string name, string? kind)
    {
        Assert.AreEqual(kind, WorkbenchValueSuggestions.KindOf(name));
    }
}
