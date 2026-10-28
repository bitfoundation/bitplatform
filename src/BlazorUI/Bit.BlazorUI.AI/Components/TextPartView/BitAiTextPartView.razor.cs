namespace Bit.BlazorUI;

/// <summary>
/// BitAiTextPartView renders a text part of a message as Markdown, with its anchored citations woven into the text.
/// </summary>
/// <remarks>
/// Anchored citations carry character offsets rather than markers. Inserting a marker at each offset before parsing is
/// what lets a superscript land mid-sentence where the claim actually sits, instead of being appended as a footnote
/// nobody connects to anything. A citation whose span ends inside code is left to the source list, since a marker
/// dropped there would be rendered literally and could break the code.
/// </remarks>
public partial class BitAiTextPartView : BitAiComponentBase
{
    private string? _source;
    private BitAiTextPart? _sourcePart;
    private BitAiMessage? _sourceMessage;
    private int _sourceVersion = -1;
    private int _citationCount = -1;
    private bool _sourceWoven;



    /// <summary>
    /// Gets or sets the cascading parameters for the text part view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the classes and styles of its parts) to be
    /// applied to multiple text part view components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiTextPartViewParams.ParamName)]
    public BitAiTextPartViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the text part view.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiTextPartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The message the part belongs to, whose citations the markers of the text resolve against.
    /// </summary>
    [Parameter, EditorRequired] public BitAiMessage Message { get; set; } = default!;

    /// <summary>
    /// The callback for when the user activates an inline citation marker.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitation> OnCitationClick { get; set; }

    /// <summary>
    /// The part to render.
    /// </summary>
    [Parameter, EditorRequired] public BitAiTextPart Part { get; set; } = default!;

    /// <summary>
    /// Custom CSS styles for different parts of the text part view.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiTextPartViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aitpv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // The text is woven again only when the part or the citations of its message changed: comparing two versions is
        // what keeps a re-render of the message from rebuilding every text part in it. A part or a message of its own
        // (the view reused for another one, whose version may well be the same number) and the inline citations turned on
        // or off change it too.
        var weave = Options.InlineCitations;
        if (ReferenceEquals(_sourcePart, Part) is false
            || ReferenceEquals(_sourceMessage, Message) is false
            || _sourceVersion != Part.Version
            || _citationCount != Message.Citations.Count
            || _sourceWoven != weave)
        {
            _sourcePart = Part;
            _sourceMessage = Message;
            _sourceVersion = Part.Version;
            _citationCount = Message.Citations.Count;
            _sourceWoven = weave;
            _source = weave ? WithCitationMarkers(Part, Message) : Part.Text;
        }

        base.OnParametersSet();
    }



    /// <summary>
    /// Inserts numbered markers at the end of every citation span that falls inside this part.
    /// </summary>
    /// <remarks>
    /// Spans that reach past the text received so far are skipped: during streaming they will be
    /// valid a moment later, and inserting them early would place a marker in the wrong sentence.
    /// </remarks>
    private static string WithCitationMarkers(BitAiTextPart part, BitAiMessage message)
    {
        var text = part.Text;

        var anchored = message.Citations
            .Where(c => c.IsAnchored && (c.PartId is null || c.PartId == part.PartId))
            .Where(c => c.EndIndex is { } end && end <= text.Length)
            .Where(c => IsInsideCode(text, c.EndIndex!.Value) is false)
            // From the end of the text back, so an insertion never moves an offset still to come; and the markers of one
            // offset from the highest number down, since each is inserted in front of the one before it, which leaves
            // them reading "[1][2]".
            .OrderByDescending(c => c.EndIndex)
            .ThenByDescending(c => c.Number)
            .ToArray();

        if (anchored.Length == 0) return text;

        var builder = new System.Text.StringBuilder(text.Length + anchored.Length * 4);
        builder.Append(text);

        foreach (var citation in anchored)
        {
            builder.Insert(citation.EndIndex!.Value, $"[{citation.Number}]");
        }

        return builder.ToString();
    }

    /// <summary>
    /// A marker dropped inside a code span or fenced block would be rendered literally and could
    /// break the code, so citations whose span ends inside code are shown in the source list only.
    /// </summary>
    internal static bool IsInsideCode(string text, int index)
    {
        var fenceOpen = false;
        var inlineOpen = false;
        var i = 0;
        while (i < index && i < text.Length)
        {
            if (text[i] == '`')
            {
                var run = 0;
                while (i + run < text.Length && text[i + run] == '`') run++;
                var atLineStart = i == 0 || text[i - 1] == '\n';
                if (run >= 3 && atLineStart) fenceOpen = fenceOpen is false;
                else if (fenceOpen is false) inlineOpen = inlineOpen is false;
                i += run;
                continue;
            }

            // A code span cannot cross a blank line; an unmatched backtick closes at the paragraph end.
            if (text[i] == '\n' && inlineOpen && i + 1 < text.Length && text[i + 1] == '\n') inlineOpen = false;
            i++;
        }

        return fenceOpen || inlineOpen;
    }
}
