namespace Bit.BlazorUI;

/// <summary>The kind of editor/filter rendered for a column based on its data type.</summary>
public enum BitDataGridColumnDataType
{
    Auto = 0,
    Text,
    Number,
    Boolean,
    Date,
    DateTime,
    DateTimeOffset,
    Enum
}
