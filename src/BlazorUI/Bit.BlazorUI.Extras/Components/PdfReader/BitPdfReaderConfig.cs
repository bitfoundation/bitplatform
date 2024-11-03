namespace Bit.BlazorUI;

public class BitPdfReaderConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Url { get; set; }

    public bool RenderAllPages { get; set; }

    public decimal Scale { get; set; } = 1;

    public int InitialPageNumber { get; set; } = 1;

}
