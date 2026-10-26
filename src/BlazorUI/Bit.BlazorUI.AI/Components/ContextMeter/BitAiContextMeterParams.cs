using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiContextMeter"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the meters of an application, or of one part of it, agree on: the two thresholds and the
/// look. The figures it reads - the conversation, the model and the counts - are left out on purpose: they are what makes
/// one meter the one it is.
/// </remarks>
public class BitAiContextMeterParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiContextMeter"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiContextMeter value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiContextMeter)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiContextMeterParams))]
    public BitAiContextMeterParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the context meter.
    /// </summary>
    public BitAiContextMeterClassStyles? Classes { get; set; }

    /// <summary>
    /// The fraction of the window at which the meter turns to its critical (error) styling.
    /// </summary>
    public double? Critical { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the context meter.
    /// </summary>
    public BitAiContextMeterClassStyles? Styles { get; set; }

    /// <summary>
    /// The fraction of the window at which the meter turns to its warning styling.
    /// </summary>
    public double? Warn { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiContextMeter"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiContextMeter"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiContextMeter"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiContextMeter"/>.
    /// </remarks>
    /// <param name="bitAiContextMeter">
    /// The <see cref="BitAiContextMeter"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiContextMeter bitAiContextMeter)
    {
        if (bitAiContextMeter is null) return;

        UpdateBaseParameters(bitAiContextMeter);

        if (Classes is not null)
        {
            bitAiContextMeter.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (Critical.HasValue)
        {
            bitAiContextMeter.TakeFromCascade(nameof(Critical), Critical.Value, static m => m.Critical, static (m, v) => m.Critical = v);
        }

        if (Styles is not null)
        {
            bitAiContextMeter.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }

        if (Warn.HasValue)
        {
            bitAiContextMeter.TakeFromCascade(nameof(Warn), Warn.Value, static m => m.Warn, static (m, v) => m.Warn = v);
        }
    }
}
