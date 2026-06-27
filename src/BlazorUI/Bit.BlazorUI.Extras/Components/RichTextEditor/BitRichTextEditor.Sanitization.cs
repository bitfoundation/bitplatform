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
    private object? BuildPolicyPayload()
    {
        if (SanitizationPolicy is null) return null;
        return new
        {
            allowedTags = SanitizationPolicy.AllowedTags.Select(t => t.ToLowerInvariant()).ToArray(),
            allowedAttributes = SanitizationPolicy.AllowedAttributes
                .GroupBy(kv => kv.Key.ToLowerInvariant())
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(kv => kv.Value)
                          .Select(a => a.ToLowerInvariant())
                          .Distinct()
                          .ToArray()),
            allowedUriSchemes = SanitizationPolicy.AllowedUriSchemes.Select(s => s.ToLowerInvariant()).ToArray(),
            allowDataImageUris = SanitizationPolicy.AllowDataImageUris
        };
    }
}
