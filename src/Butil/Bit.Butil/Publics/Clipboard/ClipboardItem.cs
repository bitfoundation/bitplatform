namespace Bit.Butil;

public class ClipboardItem
{
    public string MimeType { get; set; } = default!;

    public byte[] Data { get; set; } = default!;
}
