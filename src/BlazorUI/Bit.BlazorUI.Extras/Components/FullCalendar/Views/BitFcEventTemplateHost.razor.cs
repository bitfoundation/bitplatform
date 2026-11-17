namespace Bit.BlazorUI;

/// <summary>
/// The content of an event surface (a month badge, a day or week block, a timeline block) that an EventTemplate draws,
/// and the one place the surface's name is put together for it.
/// </summary>
/// <remarks>
/// A templated surface shows whatever the template shows, which the details of the event alone would not hold, so it
/// is named by the text it shows first and the details after it: the first is the text a speech-input user reads off
/// it (Label in Name, WCAG 2.5.3), the second what the template may well leave out. The surface points its
/// aria-labelledby at itself and then at the hidden details (<see cref="LabelledBy"/>): a reference to itself is
/// computed from its own content, so the template's markup needs no box of its own to be named by - one with
/// display: contents is one that some engines leave out of the accessibility tree, and its text with it.
/// <para>
/// The hidden details are rendered by the surface right after itself, as its sibling, never inside it: walking the
/// surface's own content for the first reference passes the hidden element and is skipped there, and a name
/// computation that visits each element once (Playwright's, the one its accessible-name assertions read in every
/// engine) then gives the second reference nothing, so the name would lose its details.
/// </para>
/// </remarks>
public partial class BitFcEventTemplateHost
{
    /// <summary>The event drawn.</summary>
    [Parameter, EditorRequired] public BitFullCalendarEvent Event { get; set; } = default!;

    /// <summary>The template that draws the event.</summary>
    [Parameter, EditorRequired] public RenderFragment<BitFullCalendarEvent> Template { get; set; } = default!;



    /// <summary>The id the surface itself carries, so its name can start with its own content.</summary>
    public static string SurfaceId(string instanceId) => $"bit-bfc-evt-{instanceId}";

    /// <summary>The id of the hidden details the name ends with, which the surface renders right after itself.</summary>
    public static string DetailsId(string instanceId) => $"bit-bfc-evt-dtl-{instanceId}";

    /// <summary>The aria-labelledby of a templated surface: its own content, then the details.</summary>
    public static string LabelledBy(string instanceId) => $"{SurfaceId(instanceId)} {DetailsId(instanceId)}";
}
