namespace Bit.BlazorUI;

/// <summary>The kind of editor/filter rendered for a column based on its data type.</summary>
public enum BitDataGridColumnDataType
{
    /// <summary>Picks the type from the bound property: bool is Boolean, an enum is Enum, DateOnly is Date,
    /// DateTime and DateTimeOffset are themselves, the numeric types are Number, and anything else is Text.</summary>
    Auto = 0,

    /// <summary>A text box, filtered with the text operators (contains, starts with, ...).</summary>
    Text,

    /// <summary>A text box with the decimal keypad, filtered with equality and the comparison operators.</summary>
    Number,

    /// <summary>A true/false dropdown.</summary>
    Boolean,

    /// <summary>A date picker, for a value without a time of day.</summary>
    Date,

    /// <summary>A date picker; an equality filter matches the whole calendar day, whatever the time of day.</summary>
    DateTime,

    /// <summary>A date picker; an equality filter matches the whole calendar day, whatever the time of day.</summary>
    DateTimeOffset,

    /// <summary>A dropdown of the enum's member names.</summary>
    Enum
}
