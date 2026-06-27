namespace Bit.Bmotion;
/// <summary>
/// Options that control how a <see cref="Bmotion"/> element is tracked within the viewport
/// for <c>WhileInView</c> and <c>OnViewportEnter</c>/<c>OnViewportLeave</c> animations.
/// </summary>
public class BmotionViewportOptions
{
    /// <summary>
    /// If <c>true</c>, once the element enters the viewport the animation will not
    /// reverse when the element leaves. Default: <c>false</c>.
    /// </summary>
    public bool Once { get; set; }

    /// <summary>
    /// A CSS margin string added to the viewport detection area, e.g. <c>"0px -20px 0px 100px"</c>.
    /// Supports the same format as <c>IntersectionObserver.rootMargin</c>.
    /// Default: <c>"0px"</c>.
    /// </summary>
    public string Margin { get; set; } = "0px";

    /// <summary>
    /// How much of the element must be visible to be considered "in view".
    /// <list type="bullet">
    ///   <item><description><c>"some"</c> (default) - any part visible.</description></item>
    ///   <item><description><c>"all"</c> - fully visible.</description></item>
    ///   <item><description>A number between <c>0</c> and <c>1</c> for exact threshold.</description></item>
    /// </list>
    /// </summary>
    public string Amount { get; set; } = "some";

    internal object ToJsObject()
    {
        var amount = Amount?.Trim().ToLowerInvariant();
        double threshold = amount switch
        {
            "some" => 0.0,
            "all"  => 1.0,
            _      => double.TryParse(amount, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out var v) && double.IsFinite(v)
                           ? Math.Clamp(v, 0, 1) : 0.0,
        };

        return new Dictionary<string, object?>
        {
            ["once"]      = Once,
            // Fall back to "0px" for null/whitespace so the JS side always receives a valid
            // IntersectionObserver rootMargin string instead of an empty/invalid value.
            ["margin"]    = string.IsNullOrWhiteSpace(Margin) ? "0px" : Margin,
            ["threshold"] = threshold,
        };
    }
}
