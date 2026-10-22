using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiDataTable renders a JSON array of objects as a table, one row per object and one column per key.
/// </summary>
/// <remarks>
/// The column set is the union of the keys seen, in the order they first appear. The union matters while streaming:
/// early rows often have fewer fields than later ones, and a table built from the first row alone would drop columns
/// as they appear. A nested object or array is shown as its JSON text, and a missing or null field as an empty cell.
/// </remarks>
public partial class BitAiDataTable : BitAiComponentBase
{
    private readonly List<string> _columns = [];
    private JsonElement[] _rows = [];



    /// <summary>
    /// Gets or sets the cascading parameters for the data table component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple data table components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiDataTableParams.ParamName)]
    public BitAiDataTableParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the data table.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiDataTableClassStyles? Classes { get; set; }

    /// <summary>
    /// The JSON array of objects to render. Anything but an array renders an empty table, and an item of the array that
    /// is not an object renders an empty row.
    /// </summary>
    [Parameter, EditorRequired] public JsonElement Rows { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the data table.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiDataTableClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aidtb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiDataTableParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        _columns.Clear();

        if (Rows.ValueKind is not JsonValueKind.Array)
        {
            _rows = [];
        }
        else
        {
            _rows = Rows.EnumerateArray().ToArray();

            foreach (var row in _rows)
            {
                if (row.ValueKind is not JsonValueKind.Object) continue;

                foreach (var property in row.EnumerateObject())
                {
                    if (_columns.Contains(property.Name, StringComparer.Ordinal) is false) _columns.Add(property.Name);
                }
            }
        }

        base.OnParametersSet();
    }



    private static string Cell(JsonElement row, string column)
    {
        if (row.ValueKind is not JsonValueKind.Object) return string.Empty;
        if (row.TryGetProperty(column, out var value) is false) return string.Empty;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            JsonValueKind.Object or JsonValueKind.Array => value.GetRawText(),
            // A number or a boolean as the JSON it was written as: JsonElement.ToString() spells true as "True".
            _ => value.GetRawText(),
        };
    }
}
