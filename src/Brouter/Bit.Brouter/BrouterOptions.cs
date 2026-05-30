namespace Bit.Brouter;

/// <summary>
/// Global options for Bit.Brouter. Register via <c>builder.Services.AddBitBrouterServices(o =&gt; ...)</c>.
/// </summary>
public sealed class BrouterOptions
{
    /// <summary>
    /// Whether literal segment matching is case sensitive. Defaults to <c>false</c>
    /// to match React Router and Vue Router conventions (URLs are case-insensitive).
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Whether <c>/users</c> and <c>/users/</c> are treated as the same path.
    /// Defaults to <c>true</c>; trailing slashes are ignored.
    /// </summary>
    public bool IgnoreTrailingSlash { get; set; } = true;

    /// <summary>
    /// Whether to scroll to the top of the page after a successful navigation.
    /// Defaults to <see cref="BrouterScrollMode.None"/>.
    /// </summary>
    public BrouterScrollMode ScrollBehavior { get; set; } = BrouterScrollMode.None;
}
