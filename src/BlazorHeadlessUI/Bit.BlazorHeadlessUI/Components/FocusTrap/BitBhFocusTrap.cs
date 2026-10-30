namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Traps keyboard focus inside its content while <see cref="BitBhFocusScope.Enabled"/>:
/// a <see cref="BitBhFocusScope"/> with <see cref="BitBhFocusScope.Trapped"/> on by default.
///
/// <code>
/// &lt;BitBhFocusTrap Enabled="isActive"&gt;
///     &lt;input placeholder="First" /&gt;
///     &lt;button&gt;Action&lt;/button&gt;
/// &lt;/BitBhFocusTrap&gt;
/// </code>
/// </summary>
public class BitBhFocusTrap : BitBhFocusScope
{
    /// <summary>Creates a trap (Trapped = true).</summary>
    public BitBhFocusTrap() => Trapped = true;
}
