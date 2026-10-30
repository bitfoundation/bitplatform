using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiMessageActions"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the action rows of an application agree on: whether they are always shown, whether they
/// offer feedback, and their classes and styles. The message, the session, whether it can be regenerated, the extra
/// actions and the callback are left out on purpose: they are what makes one row the one it is.
/// </remarks>
public class BitAiMessageActionsParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiMessageActions"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiMessageActions value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiMessageActions)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiMessageActionsParams))]
    public BitAiMessageActionsParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the message actions.
    /// </summary>
    public BitAiMessageActionsClassStyles? Classes { get; set; }

    /// <summary>
    /// Keeps the actions visible rather than revealing them on hover.
    /// </summary>
    public bool? Pinned { get; set; }

    /// <summary>
    /// Shows the thumbs up and down buttons.
    /// </summary>
    public bool? ShowFeedback { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the message actions.
    /// </summary>
    public BitAiMessageActionsClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiMessageActions"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiMessageActions"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiMessageActions"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiMessageActions"/>.
    /// </remarks>
    /// <param name="bitAiMessageActions">
    /// The <see cref="BitAiMessageActions"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiMessageActions bitAiMessageActions)
    {
        if (bitAiMessageActions is null) return;

        UpdateBaseParameters(bitAiMessageActions);

        if (Classes is not null)
        {
            bitAiMessageActions.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (Pinned.HasValue)
        {
            bitAiMessageActions.TakeFromCascade(nameof(Pinned), Pinned.Value, static m => m.Pinned, static (m, v) => m.Pinned = v);
        }

        if (ShowFeedback.HasValue)
        {
            bitAiMessageActions.TakeFromCascade(nameof(ShowFeedback), ShowFeedback.Value, static m => m.ShowFeedback, static (m, v) => m.ShowFeedback = v);
        }

        if (Styles is not null)
        {
            bitAiMessageActions.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }
    }
}
