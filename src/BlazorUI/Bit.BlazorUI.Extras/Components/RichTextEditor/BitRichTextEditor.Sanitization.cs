namespace Bit.BlazorUI;

// Sanitization plumbing. When SanitizationPolicy is null the bridge applies a secure default
// allowlist; otherwise the provided allowlist payload is sent to the bridge.
public partial class BitRichTextEditor
{
    /// <summary>
    /// Allowlist policy applied to all content. When null the bridge applies a secure
    /// default allowlist.
    /// </summary>
    [Parameter] public BitRichTextEditorSanitizationPolicy? SanitizationPolicy { get; set; }

    /// <summary>Builds the policy object sent to the JS bridge, or null for the default.</summary>
    private BitRichTextEditorPolicyPayload? BuildPolicyPayload()
    {
        if (SanitizationPolicy is null) return null;
        // A concrete class is required here (not an anonymous type): the trimmer strips anonymous
        // type constructor parameter names in release builds, which breaks System.Text.Json
        // serialization during the JS interop call.
        return new BitRichTextEditorPolicyPayload
        {
            AllowedTags = SanitizationPolicy.AllowedTags.Select(t => t.ToLowerInvariant()).ToArray(),
            AllowedAttributes = SanitizationPolicy.AllowedAttributes
                .GroupBy(kv => kv.Key.ToLowerInvariant())
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(kv => kv.Value)
                          .Select(a => a.ToLowerInvariant())
                          .Distinct()
                          .ToArray()),
            AllowedUriSchemes = SanitizationPolicy.AllowedUriSchemes.Select(s => s.ToLowerInvariant()).ToArray(),
            AllowDataImageUris = SanitizationPolicy.AllowDataImageUris,
            // Null is passed through as null: the bridge then applies its built-in approved embed
            // hosts, so a policy that lists the iframe tag without saying where from is still held
            // to them rather than accepting any source its scheme allowlist happens to permit.
            AllowedIframeHosts = SanitizationPolicy.AllowedIframeHosts?.Select(h => h.ToLowerInvariant()).ToArray()
        };
    }
}
