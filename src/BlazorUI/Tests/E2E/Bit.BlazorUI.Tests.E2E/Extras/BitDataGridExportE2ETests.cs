using System.IO;
using System.IO.Compression;
using System.Text;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's exports on /e2e/data-grid/export, read back from the downloads the browser receives: the toolbar's
/// Export CSV (ExportFileName, the UTF-8 BOM, the header line, Exportable off, ExportValue, a column with no value left
/// out, quoting, formula-injection neutralised, every matching row rather than the page, the active filter honoured),
/// Export Excel (a real .xlsx: the frozen header and Frozen column as a pane, the AutoFilter, numbers as numbers,
/// ExcelExportStyled's fills), the selected-rows exports, ToCsv / ToCsvAsync / ToExcelAsync, the default "export" file
/// name, and an infinite-scrolling grid whose export asks its provider once for every row.
/// </summary>
[TestClass]
public class BitDataGridExportE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/export";
    private const string Id = "grid-export";

    private const string FullCsv =
        "Id,Name,Price,Released,Value\r\n" +
        "1,Laptop,1200,2023-05-01,6000\r\n" +
        "2,\"Desk, oak\",300,2022-01-15,600\r\n" +
        "3,'=SUM(A1),10,2021-11-20,10\r\n" +
        "4,Lamp,45,2023-02-10,1350\r\n" +
        "5,\"Say \"\"hi\"\"\",5,2024-03-05,15\r\n" +
        "6,Mug,10,2022-09-09,2000\r\n";

    private async Task<(string Name, byte[] Bytes)> DownloadAsync(ILocator button)
    {
        var download = await Page.RunAndWaitForDownloadAsync(() => button.ClickAsync());
        var path = await download.PathAsync();
        return (download.SuggestedFilename, await File.ReadAllBytesAsync(path));
    }

    private static string Utf8WithoutBom(byte[] bytes)
    {
        Assert.IsTrue(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "The CSV does not start with a UTF-8 BOM.");
        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }

    private static string ReadZipEntry(byte[] bytes, string entry)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var part = zip.GetEntry(entry);
        Assert.IsNotNull(part, $"The workbook has no {entry}.");
        using var reader = new StreamReader(part.Open());
        return reader.ReadToEnd();
    }

    [TestMethod]
    public async Task Csv_TheToolbarButton_DownloadsEveryRowWithTheExportedColumns()
    {
        await OpenAsync(Url);

        await Expect(DataRows(Id)).ToHaveCountAsync(3);

        var (name, bytes) = await DownloadAsync(Toolbar(Id).GetByRole(AriaRole.Button, new() { Name = "Export CSV" }));

        Assert.AreEqual("products.csv", name);
        // Every row, not the page of three; Supplier opts out and Note has no value to write; Value comes from ExportValue.
        Assert.AreEqual(FullCsv, Utf8WithoutBom(bytes));
    }

    [TestMethod]
    public async Task Csv_TheActiveFilter_NarrowsTheExport()
    {
        await OpenAsync(Url);

        await FilterRow(Id).Locator("[aria-label='Filter by Name']").FillAsync("la");
        await ExpectColumnAsync(Id, 3, "Laptop", "Lamp");

        var (_, bytes) = await DownloadAsync(Toolbar(Id).GetByRole(AriaRole.Button, new() { Name = "Export CSV" }));

        Assert.AreEqual("Id,Name,Price,Released,Value\r\n1,Laptop,1200,2023-05-01,6000\r\n4,Lamp,45,2023-02-10,1350\r\n", Utf8WithoutBom(bytes));
    }

    [TestMethod]
    public async Task Excel_TheToolbarButton_DownloadsARealWorkbook()
    {
        await OpenAsync(Url);

        var (name, bytes) = await DownloadAsync(Toolbar(Id).GetByRole(AriaRole.Button, new() { Name = "Export Excel" }));

        Assert.AreEqual("products.xlsx", name);
        Assert.IsTrue(bytes.Length > 1000 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K', "The download is not a zip package.");

        var sheet = ReadZipEntry(bytes, "xl/worksheets/sheet1.xml");
        StringAssert.Contains(sheet, ">Laptop<");
        StringAssert.Contains(sheet, ">Desk, oak<");
        StringAssert.Contains(sheet, ">Released<");
        Assert.IsFalse(sheet.Contains("Acme"), "A column that is not exportable reached the workbook.");
        Assert.IsFalse(sheet.Contains("no value to export"), "A template-only column reached the workbook.");
        // The header row and the frozen Id column are a frozen pane, under Excel's own AutoFilter.
        StringAssert.Contains(sheet, "xSplit=\"1\"");
        StringAssert.Contains(sheet, "ySplit=\"1\"");
        StringAssert.Contains(sheet, "<autoFilter ref=\"A1:E7\"/>");
        // Numbers land as numbers.
        StringAssert.Contains(sheet, "<v>6000</v>");

        // ExcelExportStyled carries the grid's colors: solid fills beyond the two the format requires.
        var styles = ReadZipEntry(bytes, "xl/styles.xml");
        StringAssert.Contains(styles, "patternType=\"solid\"");

        StringAssert.Contains(ReadZipEntry(bytes, "[Content_Types].xml"), "spreadsheetml");
    }

    [TestMethod]
    public async Task Selected_TheSelectedOnlyExports_CarryTheSelectedRowsAlone()
    {
        await OpenAsync(Url);

        await Cell(Id, 0, 1).Locator("input").CheckAsync();
        await Expect(Cell(Id, 0, 1).Locator("input")).ToBeCheckedAsync();
        await Pagers(Id).Locator("button[aria-label='Next page']").ClickAsync();
        await Expect(Cell(Id, 0, 3)).ToHaveTextAsync("Lamp");
        await Cell(Id, 0, 1).Locator("input").CheckAsync();
        await Expect(DataRow(Id, 0)).ToHaveAttributeAsync("aria-selected", "true");

        var (name, bytes) = await DownloadAsync(Page.Locator("#btn-export-selected-csv"));
        Assert.AreEqual("products.csv", name);
        Assert.AreEqual("Id,Name,Price,Released,Value\r\n1,Laptop,1200,2023-05-01,6000\r\n4,Lamp,45,2023-02-10,1350\r\n", Utf8WithoutBom(bytes));

        var (excelName, excel) = await DownloadAsync(Page.Locator("#btn-export-selected-excel"));
        Assert.AreEqual("products.xlsx", excelName);
        var sheet = ReadZipEntry(excel, "xl/worksheets/sheet1.xml");
        StringAssert.Contains(sheet, ">Lamp<");
        Assert.IsFalse(sheet.Contains(">Mug<"), "A row that is not selected reached the selected-rows workbook.");

        await Page.Locator("#btn-to-csv-selected").ClickAsync();
        await Expect(Text("csv-text")).ToContainTextAsync("Lamp");
        Assert.AreEqual("Id,Name,Price,Released,Value\r\n1,Laptop,1200,2023-05-01,6000\r\n4,Lamp,45,2023-02-10,1350\r\n",
            await Text("csv-text").EvaluateAsync<string>("el => el.textContent"));
    }

    [TestMethod]
    public async Task Api_ToCsvAndToExcelAsync_BuildTheExportsInCode()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-to-csv").ClickAsync();
        await Expect(Text("csv-text")).ToContainTextAsync("Mug");
        var csv = await Text("csv-text").EvaluateAsync<string>("el => el.textContent");
        Assert.AreEqual(FullCsv, csv);

        await Page.Locator("#btn-to-excel").ClickAsync();
        await Expect(Text("excel-size")).ToHaveTextAsync("zip");
    }

    [TestMethod]
    public async Task Csv_WithoutExportFileName_IsNamedExport()
    {
        await OpenAsync(Url);

        var (name, bytes) = await DownloadAsync(Toolbar("grid-export-default").GetByRole(AriaRole.Button, new() { Name = "Export CSV" }));

        Assert.AreEqual("export.csv", name);
        StringAssert.StartsWith(Utf8WithoutBom(bytes), "Id,Name\r\n1,Laptop\r\n");
    }

    [TestMethod]
    public async Task Csv_AnInfiniteGrid_AsksItsProviderForEveryRow()
    {
        await OpenAsync(Url);

        const string id = "grid-export-infinite";
        await Expect(DataRows(id).First).ToBeVisibleAsync();
        var loaded = await DataRows(id).CountAsync();
        Assert.IsTrue(loaded < 55, "Every row was loaded before the export, which then proves nothing.");

        var (name, bytes) = await DownloadAsync(Toolbar(id).GetByRole(AriaRole.Button, new() { Name = "Export CSV" }));

        Assert.AreEqual("all-rows.csv", name);
        var lines = Utf8WithoutBom(bytes).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(56, lines.Length, "The export does not hold every row the provider has.");
        Assert.AreEqual("55,Row 55", lines[^1]);
        await Expect(Text("infinite-requests")).ToContainTextAsync("[0,all]");
        // The export's rows are not the grid's: it still holds the batches it had loaded.
        Assert.IsTrue(await DataRows(id).CountAsync() < 55, "The export's rows were added to the grid.");
    }
}
