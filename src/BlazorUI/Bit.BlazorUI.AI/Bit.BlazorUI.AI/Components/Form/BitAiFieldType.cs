namespace Bit.BlazorUI;

/// <summary>
/// The input control a form field renders as.
/// </summary>
public enum BitAiFieldType
{
    /// <summary>A single-line text box.</summary>
    Text = 0,

    /// <summary>A multi-line text box.</summary>
    MultilineText = 1,

    /// <summary>A numeric input.</summary>
    Number = 2,

    /// <summary>A checkbox.</summary>
    Boolean = 3,

    /// <summary>A picker over <see cref="BitAiFormField.Options"/>.</summary>
    Select = 4,

    /// <summary>A date picker.</summary>
    Date = 5,

    /// <summary>A date and time picker.</summary>
    DateTime = 6,

    /// <summary>An email input.</summary>
    Email = 7,

    /// <summary>A URL input.</summary>
    Url = 8,

    /// <summary>A masked input.</summary>
    Password = 9,

    /// <summary>A time-of-day picker.</summary>
    Time = 10,

    /// <summary>A group of nested fields, from a schema <c>object</c> with <c>properties</c>.</summary>
    Object = 11,

    /// <summary>
    /// A list the user can add to and remove from. Scalar <see cref="BitAiFormField.Items"/> render as a
    /// list of inputs; object items render as repeatable groups.
    /// </summary>
    Array = 12,
}
