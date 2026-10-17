namespace Bit.BbScanner.Tests;

/// <summary>Round trips generated Data Matrix symbols through the decoding path.</summary>
[TestClass]
public class DataMatrixDecoderTests
{
    /// <summary>Every one of the thirty shapes at every module size from two to five pixels.</summary>
    public static IEnumerable<object[]> EveryShapeAndScale
    {
        get
        {
            var data = new List<object[]>();
            foreach (var version in BbDataMatrixVersion.All)
            {
                for (var scale = 2; scale <= 5; scale++)
                {
                    data.Add([version.VersionNumber, scale]);
                }
            }

            return data;
        }
    }

    /// <summary>
    /// Detection measures the symbol from its timing patterns, so every shape at every small
    /// module size has to come through the whole rendered path, not just the parser.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(EveryShapeAndScale))]
    public void DecodesEveryShapeFromRenderedImage(int versionNumber, int scale)
    {
        var version = BbDataMatrixVersion.All.Single(v => v.VersionNumber == versionNumber);

        // Fill about half of the symbol with distinct letters so the data region is busy.
        var content = new string(
            Enumerable.Range(0, Math.Max(1, version.TotalDataCodewords / 2))
                .Select(i => (char)('A' + (i * 7 % 26)))
                .ToArray());

        using var matrix = DataMatrixEncoder.Encode(content, version);
        Assert.AreEqual(version.SymbolSizeRows, matrix.Height);
        Assert.AreEqual(version.SymbolSizeColumns, matrix.Width);

        using var image = SyntheticImage.FromMatrix(matrix, scale, quietZoneModules: 4);
        using var binary = new BbHybridBinarizer().GetBlackMatrix(image.View);
        Assert.IsNotNull(binary);

        var reader = new BbDataMatrixReader();
        foreach (var options in new[] { BbMatrixDecodeOptions.Live, BbMatrixDecodeOptions.Thorough })
        {
            var result = reader.Decode(binary, options);

            Assert.IsTrue(result is not null, $"{version} at {scale} px/module did not decode (TryHarder: {options.TryHarder}).");
            Assert.AreEqual(content, result.Payload.Text);
            Assert.AreEqual(version.ToString(), result.Payload.SymbolVersion);
        }
    }

    [TestMethod]
    [DataRow("A")]
    [DataRow("HELLO")]
    [DataRow("Data Matrix 2026!")]
    [DataRow("1234567890123456")]
    [DataRow("https://example.com/item/42")]
    public void DecodesGrid(string content)
    {
        using var matrix = DataMatrixEncoder.Encode(content);
        var result = new BbDataMatrixReader().DecodeMatrix(matrix);

        Assert.IsNotNull(result);
        Assert.AreEqual(content, result.Text);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(9)]
    [DataRow(10)]
    [DataRow(15)]
    [DataRow(17)]
    public void DecodesEverySymbolShapeBand(int minimumVersion)
    {
        const string Content = "AB";
        using var matrix = DataMatrixEncoder.Encode(Content, minimumVersion);

        var expected = BbDataMatrixVersion.All.First(v => v.VersionNumber == minimumVersion);
        Assert.AreEqual(expected.SymbolSizeRows, matrix.Height);

        var result = new BbDataMatrixReader().DecodeMatrix(matrix);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Text);
        Assert.AreEqual(expected.ToString(), result.SymbolVersion);
    }

    [TestMethod]
    public void DecodesFromRenderedImage()
    {
        const string Content = "SCANNER";
        using var matrix = DataMatrixEncoder.Encode(Content);
        using var image = SyntheticImage.FromMatrix(matrix, scale: 6, quietZoneModules: 4);

        using var binary = new BbHybridBinarizer().GetBlackMatrix(image.View);
        Assert.IsNotNull(binary);

        var result = new BbDataMatrixReader().Decode(binary, BbMatrixDecodeOptions.Thorough);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Payload.Text);
        Assert.AreEqual(BbBarcodeFormat.DataMatrix, result.Format);
        Assert.AreEqual(4, result.Points.Length);
    }

    [TestMethod]
    public void RecoversFromDamagedModules()
    {
        const string Content = "RESILIENCE";
        using var matrix = DataMatrixEncoder.Encode(Content, minimumVersion: 6);

        // Flip a couple of data modules well inside the symbol.
        matrix[8, 8] = !matrix[8, 8];
        matrix[9, 9] = !matrix[9, 9];

        var result = new BbDataMatrixReader().DecodeMatrix(matrix);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Text);
        Assert.IsTrue(result.ErrorsCorrected > 0);
    }

    [TestMethod]
    public void SymbolShapeTableIsInternallyConsistent()
    {
        foreach (var version in BbDataMatrixVersion.All)
        {
            var regionsDown = version.SymbolSizeRows / version.DataRegionSizeRows;
            var regionsAcross = version.SymbolSizeColumns / version.DataRegionSizeColumns;

            // Each data region is surrounded by a one module border on every side.
            Assert.AreEqual(version.SymbolSizeRows, regionsDown * (version.DataRegionSizeRows + 2));
            Assert.AreEqual(version.SymbolSizeColumns, regionsAcross * (version.DataRegionSizeColumns + 2));

            // Every module of every data region holds exactly one bit of one codeword.
            var dataModules = regionsDown * regionsAcross * version.DataRegionSizeRows * version.DataRegionSizeColumns;
            var placed = version.TotalCodewords * 8;

            // The 144 by 144 symbol leaves four modules to the fixed corner pattern.
            Assert.IsTrue(dataModules - placed is 0 or 4, $"Version {version.VersionNumber}: {dataModules} modules but {placed} placed bits.");
        }
    }

    [TestMethod]
    public void ReturnsNullForAnEmptyImage()
    {
        using var blank = new BbBitMatrix(64, 64);
        Assert.IsNull(new BbDataMatrixReader().Decode(blank, BbMatrixDecodeOptions.Live));
    }
}
