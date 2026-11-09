using System.Globalization;

namespace Bit.Bheadless;

/// <summary>
/// A headless segmented date input (React Aria's DateField): the month, the day and the year are each a
/// <c>role="spinbutton"</c> segment, in the order and with the separators of the culture's short date pattern
/// (<c>M/d/yyyy</c> in <c>en-US</c>, <c>dd.MM.yyyy</c> in <c>de-DE</c>, <c>yyyy/M/d</c> with Persian months in <c>fa-IR</c>).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Typing</b> — digits fill the focused segment, which moves on once no other digit could follow (a "4" in
///     the month, two digits in the day, four in the year); typing the separator moves on too. Backspace deletes a digit,
///     then moves back. Empty segments show a placeholder (<c>mm</c>, <c>dd</c>, <c>yyyy</c>) with <c>data-placeholder</c>.</item>
///   <item><b>Keyboard</b> — ↑ / ↓ step the segment (wrapping), PageUp / PageDown step further, Home / End go to its
///     bounds, ← / → move between segments (mirrored right-to-left). Every segment is a tab stop.</item>
///   <item><b>Value</b> — <c>@bind-Value</c> a <c>DateOnly?</c>: it is set once every segment is filled (a day past the
///     month's end becomes its last day) and null while one is empty. A value outside <c>Min</c> / <c>Max</c> is kept
///     and marks the field invalid.</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BitBhField"/> wiring (the root is a <c>role="group"</c>
///     labelled by the field's label; clicking the label focuses the first segment) and a hidden input
///     (<c>Name</c>) carrying <c>yyyy-MM-dd</c>.</item>
/// </list>
///
/// <code>
/// &lt;BitBhField&gt;
///     &lt;BitBhLabel&gt;Birth date&lt;/BitBhLabel&gt;
///     &lt;BitBhDateField @bind-Value="birthDate" class="date-field" /&gt;
/// &lt;/BitBhField&gt;
/// </code>
///
/// <para>The segments are focusable spans, not text inputs: a phone's on-screen keyboard is not offered for them (use a
/// <see cref="BitBhDatePicker"/>, whose calendar works by touch).</para>
///
/// <para><b>Styling hooks:</b> <c>data-disabled</c>, <c>data-readonly</c>, <c>data-invalid</c>, <c>data-required</c>,
/// <c>data-focused</c> (focus within), <c>data-empty</c> on the root; see <see cref="BitBhDateFieldSegment"/> for the segments.</para>
/// </summary>
public class BitBhDateField : BitBhDateFieldBase<DateOnly>
{
    private protected override List<BitBhDateSegmentState.Spec> BuildLayout(BitBhCalendarSystem system, out bool is12Hour)
    {
        is12Hour = false;
        return DateLayout(system);
    }

    /// <summary>The year, month and day segments of the culture's short date pattern (<c>MM/dd/yyyy</c> when it lacks one).</summary>
    internal static List<BitBhDateSegmentState.Spec> DateLayout(BitBhCalendarSystem system)
    {
        var layout = BitBhDateSegmentState.Parse(system.Format.ShortDatePattern);
        var types = layout.Where(s => s.Type != BitBhDateSegmentType.Literal).Select(s => s.Type).ToList();
        var complete = types.Count == 3 && types.Contains(BitBhDateSegmentType.Year) && types.Contains(BitBhDateSegmentType.Month) && types.Contains(BitBhDateSegmentType.Day);
        return complete ? layout : BitBhDateSegmentState.Parse("MM/dd/yyyy");
    }

    private protected override void Load(DateOnly value) => State.SetDate(value);

    private protected override DateOnly? Compose() => State.ComposeDate();

    private protected override string ToIso(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private DateOnly PlaceholderDate => PlaceholderValue ?? DateOnly.FromDateTime(DateTime.Today);

    private protected override int PlaceholderYear => System.Year(PlaceholderDate);

    private protected override int PlaceholderPart(BitBhDateSegmentType type) => type switch
    {
        BitBhDateSegmentType.Year => System.Year(PlaceholderDate),
        BitBhDateSegmentType.Month => System.Month(PlaceholderDate),
        BitBhDateSegmentType.Day => System.Day(PlaceholderDate),
        _ => 0,
    };
}
