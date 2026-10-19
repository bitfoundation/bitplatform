namespace Bit.Bdnd.Tests.Core;

[TestClass]
public class BdndValueTests
{
    // ---- BdndXYCoord ----

    [TestMethod]
    public void CoordinatesSubtractAndAddComponentWise()
    {
        var a = new BdndXYCoord(10, 20);
        var b = new BdndXYCoord(2.5, -4);

        Assert.AreEqual(new BdndXYCoord(7.5, 24), a - b);
        Assert.AreEqual(new BdndXYCoord(12.5, 16), a + b);
        Assert.AreEqual(a, a - b + b);
    }

    [TestMethod]
    public void CoordinatesCompareByValue()
    {
        var a = new BdndXYCoord(1, 2);

        Assert.AreEqual(new BdndXYCoord(1, 2), a);
        Assert.IsTrue(a == new BdndXYCoord(1, 2));
        Assert.IsTrue(a != new BdndXYCoord(2, 1));
        Assert.AreEqual(new BdndXYCoord(1, 5), a with { Y = 5 });

        var (x, y) = a;
        Assert.AreEqual(1, x);
        Assert.AreEqual(2, y);
    }

    // ---- BdndNativeTypes ----

    [TestMethod]
    [DataRow(BdndNativeTypes.File, "__NATIVE_FILE__", "Files")]
    [DataRow(BdndNativeTypes.Url, "__NATIVE_URL__", "text/uri-list")]
    [DataRow(BdndNativeTypes.Text, "__NATIVE_TEXT__", "text/plain")]
    [DataRow(BdndNativeTypes.Html, "__NATIVE_HTML__", "text/html")]
    public void NativeTypesKeepReactDndValuesAndMapToTheirDataTransferType(string nativeType, string value, string dataTransferType)
    {
        Assert.AreEqual(value, nativeType);
        Assert.IsTrue(BdndNativeTypes.IsNative(nativeType));
        Assert.AreEqual(dataTransferType, BdndNativeTypes.ToDataTransferType(nativeType));
    }

    [TestMethod]
    [DataRow("box")]
    [DataRow("Files")]
    [DataRow("text/plain")]
    [DataRow("__native_file__")]
    [DataRow("")]
    public void InAppTypesAreNotNative(string type)
    {
        Assert.IsFalse(BdndNativeTypes.IsNative(type));
    }

    [TestMethod]
    public void AnUnknownNativeTypeIsUsedAsItsOwnDataTransferType()
    {
        Assert.AreEqual("__NATIVE_CUSTOM__", BdndNativeTypes.ToDataTransferType("__NATIVE_CUSTOM__"));
    }

    // ---- BdndNativeDropEventArgs ----

    [TestMethod]
    public void NativeDropArgsDefaultToNoFilesAndNoData()
    {
        var args = new BdndNativeDropEventArgs { ItemType = BdndNativeTypes.File };

        Assert.IsEmpty(args.Files);
        Assert.IsEmpty(args.Data);
        Assert.IsNull(args.Text);
        Assert.IsNull(args.Html);
        Assert.IsNull(args.Url);
        Assert.IsEmpty(args.Urls);
    }

    [TestMethod]
    public void NativeDropArgsReadTextHtmlAndUrlFromTheData()
    {
        var args = new BdndNativeDropEventArgs
        {
            ItemType = BdndNativeTypes.Html,
            Data = new Dictionary<string, string>
            {
                ["text/plain"] = "hello",
                ["text/html"] = "<b>hello</b>",
                ["text/uri-list"] = "https://bitplatform.dev",
            },
        };

        Assert.AreEqual("hello", args.Text);
        Assert.AreEqual("<b>hello</b>", args.Html);
        Assert.AreEqual("https://bitplatform.dev", args.Url);
        Assert.AreSequenceEqual(new[] { "https://bitplatform.dev" }, args.Urls);
    }

    [TestMethod]
    public void UrlsSplitTheUriListAndSkipCommentsAndBlankLines()
    {
        // RFC 2483: CRLF separated, '#' starts a comment line
        var args = new BdndNativeDropEventArgs
        {
            ItemType = BdndNativeTypes.Url,
            Data = new Dictionary<string, string>
            {
                ["text/uri-list"] = "# dragged from a browser\r\nhttps://a.example/\r\n\r\n   \r\n  https://b.example/x  \nhttps://c.example/#fragment\n#another comment\n",
            },
        };

        Assert.AreSequenceEqual(new[] { "https://a.example/", "https://b.example/x", "https://c.example/#fragment" }, args.Urls);
    }

    [TestMethod]
    public void AnEmptyUriListHasNoUrls()
    {
        var args = new BdndNativeDropEventArgs
        {
            ItemType = BdndNativeTypes.Url,
            Data = new Dictionary<string, string> { ["text/uri-list"] = "" },
        };

        Assert.AreEqual("", args.Url);
        Assert.IsEmpty(args.Urls);
    }

    // ---- payload DTOs ----

    [TestMethod]
    public void InteropPayloadsStartEmpty()
    {
        var payload = new BdndNativeDropPayload();
        var file = new BdndNativeFile();
        var rect = new BdndDomRect();

        Assert.IsEmpty(payload.Types);
        Assert.IsEmpty(payload.Data);
        Assert.IsEmpty(payload.Files);
        Assert.AreEqual("", file.Name);
        Assert.AreEqual("", file.Type);
        Assert.AreEqual(0, file.Size);
        Assert.AreEqual(0, rect.Left + rect.Top + rect.Width + rect.Height);
    }

    [TestMethod]
    public void InteropPayloadsDeserializeFromTheScriptsCamelCaseShape()
    {
        // the shape bit-bdnd.js sends to HandleNativeDrop, read with the JS interop serializer options
        var json = """{"types":["Files","text/plain"],"data":{"text/plain":"a.txt"},"files":[{"name":"a.txt","size":12,"type":"text/plain"}]}""";

        var payload = System.Text.Json.JsonSerializer.Deserialize<BdndNativeDropPayload>(json,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;

        Assert.AreSequenceEqual(new[] { "Files", "text/plain" }, payload.Types);
        Assert.AreEqual("a.txt", payload.Data["text/plain"]);
        var file = Assert.ContainsSingle(payload.Files);
        Assert.AreEqual("a.txt", file.Name);
        Assert.AreEqual(12, file.Size);
        Assert.AreEqual("text/plain", file.Type);
    }

    // ---- BdndDropTargetScope ----

    [TestMethod]
    public void ATargetScopeFindsTheNearestTargetOfTheSameManager()
    {
        var outerManager = new BdndDragDropManager();
        var innerManager = new BdndDragDropManager();
        var outer = new BdndDropTargetScope(outerManager, "outer", null);
        var nestedProviderTarget = new BdndDropTargetScope(innerManager, "nested", outer);
        var inner = new BdndDropTargetScope(outerManager, "inner", nestedProviderTarget);

        Assert.AreEqual("inner", inner.FindTargetId(outerManager));
        Assert.AreEqual("outer", nestedProviderTarget.FindTargetId(outerManager), "targets of a nested provider are skipped");
        Assert.AreEqual("nested", inner.FindTargetId(innerManager));
        Assert.IsNull(outer.FindTargetId(innerManager));
    }
}
