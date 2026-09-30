namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitMediaQuery"/> component.
/// </summary>
public class BitMediaQueryParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitMediaQuery"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitMediaQuery value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitMediaQuery)}";



    public string Name => ParamName;



    /// <summary>
    /// The initial matched state to render with until the actual result of the query arrives from the browser.
    /// </summary>
    public bool? DefaultMatched { get; set; }

    /// <summary>
    /// Renders the active content directly, without the wrapping root element.
    /// </summary>
    public bool? NoWrapper { get; set; }

    /// <summary>
    /// The custom media query to be matched.
    /// </summary>
    /// <remarks>
    /// A query is one decision with <see cref="ScreenQuery"/>: neither is cascaded to a media query that sets
    /// either of them itself, so a cascaded <see cref="Query"/> never overrides a <see cref="BitMediaQuery.ScreenQuery"/>
    /// written on the component.
    /// </remarks>
    public string? Query { get; set; }

    /// <summary>
    /// The predefined screen query to be matched.
    /// </summary>
    /// <remarks>
    /// A query is one decision with <see cref="Query"/>: neither is cascaded to a media query that sets either of
    /// them itself.
    /// </remarks>
    public BitScreenQuery? ScreenQuery { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitMediaQuery"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitMediaQuery"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitMediaQuery"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitMediaQuery"/>.
    /// </remarks>
    /// <param name="bitMediaQuery">
    /// The <see cref="BitMediaQuery"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitMediaQuery bitMediaQuery)
    {
        if (bitMediaQuery is null) return;

        UpdateBaseParameters(bitMediaQuery);

        if (DefaultMatched.HasValue && bitMediaQuery.HasNotBeenSet(nameof(DefaultMatched)))
        {
            bitMediaQuery.DefaultMatched = DefaultMatched.Value;
        }

        if (NoWrapper.HasValue && bitMediaQuery.HasNotBeenSet(nameof(NoWrapper)))
        {
            bitMediaQuery.NoWrapper = NoWrapper.Value;
        }

        if (AppliesQuery(bitMediaQuery))
        {
            bitMediaQuery.Query = Query;
            bitMediaQuery.ScreenQuery = ScreenQuery;
        }
    }

    // What to match is one decision, made on the component or here as a whole: a cascaded Query would otherwise take
    // precedence over a ScreenQuery written on the component, and a cascaded ScreenQuery would sit unused beside a Query.
    internal bool AppliesQuery(BitMediaQuery bitMediaQuery)
    {
        return (Query.HasValue() || ScreenQuery.HasValue)
            && bitMediaQuery.HasNotBeenSet(nameof(BitMediaQuery.Query))
            && bitMediaQuery.HasNotBeenSet(nameof(BitMediaQuery.ScreenQuery));
    }
}
