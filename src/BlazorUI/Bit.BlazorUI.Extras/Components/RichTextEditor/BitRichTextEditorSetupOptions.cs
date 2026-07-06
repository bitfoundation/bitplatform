namespace Bit.BlazorUI;

internal class BitRichTextEditorSetupOptions
{
    public int Debounce { get; set; }
    public BitRichTextEditorPolicyPayload? Policy { get; set; }
    public bool HasUpload { get; set; }
    public bool PlainTextPaste { get; set; }
    public int? MaxLength { get; set; }
    public string[]? ShortcutKeys { get; set; }
}
