namespace Bit.BlazorUI;

// The sanitization policy payload sent to the JS bridge. Property names are serialized to
// camelCase by the JS interop serializer to match what the bridge reads (allowedTags, ...).
internal class BitRichTextEditorPolicyPayload
{
    public string[] AllowedTags { get; set; } = [];
    public Dictionary<string, string[]> AllowedAttributes { get; set; } = [];
    public string[] AllowedUriSchemes { get; set; } = [];
    public bool AllowDataImageUris { get; set; }
}
