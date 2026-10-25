namespace Bit.Borm;

/// <summary>
/// The logical kind of a field. A renderer maps each <see cref="BormFieldType"/> to a concrete UI control.
/// The type is intentionally UI-agnostic: it describes intent, not a specific widget.
/// </summary>
public enum BormFieldType
{
    /// <summary>Single-line free text.</summary>
    Text,

    /// <summary>Multi-line free text.</summary>
    TextArea,

    /// <summary>Email address (text with email semantics/validation).</summary>
    Email,

    /// <summary>Masked secret input.</summary>
    Password,

    /// <summary>URL input.</summary>
    Url,

    /// <summary>Telephone number input.</summary>
    Tel,

    /// <summary>Whole number input.</summary>
    Integer,

    /// <summary>Decimal/floating point number input.</summary>
    Number,

    /// <summary>Boolean toggle (checkbox/switch).</summary>
    Checkbox,

    /// <summary>Single choice from a fixed list (dropdown).</summary>
    Select,

    /// <summary>Multiple choice from a fixed list.</summary>
    MultiSelect,

    /// <summary>Single choice rendered as a radio group.</summary>
    Radio,

    /// <summary>Date only.</summary>
    Date,

    /// <summary>Date and time.</summary>
    DateTime,

    /// <summary>Time only.</summary>
    Time,

    /// <summary>Color picker.</summary>
    Color,

    /// <summary>Slider/range input.</summary>
    Range,

    /// <summary>File upload.</summary>
    File,

    /// <summary>Hidden value, not displayed.</summary>
    Hidden,

    /// <summary>
    /// Presentational content — a section heading, an explanatory paragraph, a divider. It binds to no
    /// value and is never validated; it exists because a real form is not only inputs, and splitting a
    /// long one into titled sections otherwise means giving up on describing it as a schema.
    /// </summary>
    Static,

    /// <summary>A nested object: a group of child fields bound to a sub-object.</summary>
    Object,

    /// <summary>A repeating list of items (array/repeater) described by an item schema.</summary>
    Array,

    /// <summary>A field rendered by a custom renderer resolved via <see cref="BormFieldDefinition.CustomRenderer"/>.</summary>
    Custom,

    /// <summary>
    /// A search box. The same value as <see cref="Text"/>, but browsers give it a clear affordance, an
    /// on-screen keyboard with a search key, and history from previous searches — none of which a plain
    /// text box gets. Appended rather than slotted in beside <see cref="Text"/> so no existing member's
    /// numeric value moves.
    /// </summary>
    Search,

    /// <summary>
    /// A text box that filters a list of choices as the user types — the control every form framework
    /// ends up adding once a dropdown has more than a screenful of options in it. It stores an option's
    /// value like a <see cref="Select"/> does, and by default only accepts one of them; set
    /// <c>AllowCustom()</c> to let the user answer with something that is not on the list.
    /// </summary>
    Combobox,

    /// <summary>
    /// A list of short free-text values entered one at a time and shown as removable chips — labels on
    /// an issue, skills on a profile, recipients on a message. It binds to a collection exactly as an
    /// <see cref="Array"/> of strings does; the difference is the affordance, because a repeater gives
    /// each tag its own row with add, remove and reorder buttons, which is the wrong shape entirely for
    /// a word.
    /// </summary>
    Tags,

    /// <summary>
    /// A month of a year — a card's expiry, a billing period, a start date given to the month. Renders
    /// the browser's month picker. A <c>DateOnly</c> or <c>DateTime</c> holds the month's first day;
    /// with no CLR type the value is the text the picker speaks, <c>2024-05</c>.
    /// </summary>
    Month,

    /// <summary>
    /// An ISO 8601 week — a timesheet, a delivery slot, a rota. Renders the browser's week picker. A
    /// <c>DateOnly</c> or <c>DateTime</c> holds the week's Monday; with no CLR type the value is the
    /// text the picker speaks, <c>2024-W19</c>.
    /// </summary>
    Week
}
