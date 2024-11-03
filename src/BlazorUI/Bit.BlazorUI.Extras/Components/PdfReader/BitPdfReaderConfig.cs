namespace Bit.BlazorUI;

public class BitPdfReaderConfig
{
    /// <summary>
    /// The id of the pdf reader instance and its canvas element(s).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The URL of the pdf file.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// The scale in which the pdf document gets rendered on the page.
    /// </summary>
    public decimal Scale { get; set; } = 1;

}
