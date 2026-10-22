namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiDataTable"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what every data table of an app or a section of it agrees on - the classes and styles of its
/// parts - rather than the rows a single table shows.
/// </remarks>
public class BitAiDataTableParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiDataTable"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiDataTable value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiDataTable)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the data table.
    /// </summary>
    public BitAiDataTableClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the data table.
    /// </summary>
    public BitAiDataTableClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiDataTable"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiDataTable"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiDataTable"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiDataTable"/>.
    /// </remarks>
    /// <param name="bitAiDataTable">
    /// The <see cref="BitAiDataTable"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiDataTable bitAiDataTable)
    {
        if (bitAiDataTable is null) return;

        UpdateBaseParameters(bitAiDataTable);

        if (Classes is not null)
        {
            bitAiDataTable.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiDataTable.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
