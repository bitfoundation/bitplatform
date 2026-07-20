namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Defines that a callback parameter will be set to its default value instead of the deserialized
/// Chart.js value. Can be applied to parameters of methods passed to a <see cref="BitChartLegacyDelegateHandler{T}"/>.
/// Use this attribute to improve performance by applying it to parameters that are expected to have
/// a large size like the 'chartData' parameter of <see cref="BitChartLegendLabelFilter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class BitChartLegacyIgnoreCallbackValueAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance of the <see cref="BitChartLegacyIgnoreCallbackValueAttribute" /> class.
    /// </summary>
    public BitChartLegacyIgnoreCallbackValueAttribute()
    {
    }
}
