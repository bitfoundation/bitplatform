using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bmotion;

/// <summary>
/// Staggered text animation - the equivalent of motion.dev's <c>splitText</c> and GSAP's
/// <c>SplitText</c>, but with no DOM surgery: the text is split in C# and each unit is rendered as
/// its own <see cref="Bmotion"/> element, so there is nothing to re-split on re-render and nothing
/// for a script to undo.
/// <code>
/// &lt;BmotionSplitText Text="Animate every character"
///                   By="BmSplitBy.Chars"
///                   Initial="Bm.To(opacity: 0, y: 24)"
///                   Animate="Bm.To(opacity: 1, y: 0)"
///                   Stagger="Bm.Stagger(0.03)"
///                   Transition="Bm.Spring(bounce: 0.3, duration: 0.6)" /&gt;
/// </code>
/// <para>
/// Whitespace between words is rendered verbatim rather than as animated units, so the text still
/// wraps, collapses and copies exactly like the unsplit original. In <see cref="BmSplitBy.Chars"/>
/// mode each word is wrapped in an inline-block of its own, so a word never breaks across lines.
/// </para>
/// <para>
/// <b>Accessibility:</b> by default the container carries the full text as an <c>aria-label</c> and
/// every split unit is <c>aria-hidden</c>, so assistive technology reads the sentence once instead
/// of spelling it out. Set <see cref="Accessible"/> to <c>false</c> to opt out (e.g. when the text
/// is already labelled by an ancestor).
/// </para>
/// <para>
/// Every unit is a real animated element, so <see cref="BmSplitBy.Chars"/> on a long paragraph
/// means hundreds of them. Split headlines by character; split body copy by
/// <see cref="BmSplitBy.Words"/> or <see cref="BmSplitBy.Lines"/>.
/// </para>
/// </summary>
public sealed class BmotionSplitText : ComponentBase
{
    /// <summary>The text to split and animate.</summary>
    [Parameter, EditorRequired] public string? Text { get; set; }

    /// <summary>Whether to animate per character (default), per word or per authored line.</summary>
    [Parameter] public BmSplitBy By { get; set; } = BmSplitBy.Chars;

    // ── Animation targets (forwarded to every unit) ────────────────────────────
    /// <summary>Starting state of every unit.</summary>
    [Parameter] public BmTarget? Initial { get; set; }

    /// <summary>Target state of every unit; each starts at its staggered offset.</summary>
    [Parameter] public BmTarget? Animate { get; set; }

    /// <summary>Exit state of every unit (requires an enclosing presence component).</summary>
    [Parameter] public BmTarget? Exit { get; set; }

    /// <summary>Hover overlay applied per unit - hovering one character animates only that one.</summary>
    [Parameter] public BmTarget? WhileHover { get; set; }

    /// <summary>Tap overlay applied per unit.</summary>
    [Parameter] public BmTarget? WhileTap { get; set; }

    /// <summary>
    /// In-view overlay: the classic "text animates in when it scrolls into view". Combine with
    /// <see cref="Once"/> so it plays a single time.
    /// </summary>
    [Parameter] public BmTarget? WhileInView { get; set; }

    /// <summary>Plays <see cref="WhileInView"/> only the first time the text enters the viewport.</summary>
    [Parameter] public bool Once { get; set; }

    /// <summary>Advanced viewport options for <see cref="WhileInView"/>.</summary>
    [Parameter] public BmViewport? Viewport { get; set; }

    /// <summary>Timing/physics for every unit. Each unit's stagger offset is added to its delay.</summary>
    [Parameter] public BmTransition? Transition { get; set; }

    /// <summary>
    /// The delay generator across units. Defaults to <c>Bm.Stagger(0.03)</c> - the cascade is the
    /// whole point of splitting the text. Pass <c>Bm.Stagger(0)</c> to animate every unit at once.
    /// </summary>
    [Parameter] public BmStagger? Stagger { get; set; }

    // ── Markup ─────────────────────────────────────────────────────────────────
    /// <summary>The element rendered around the whole run. Default <c>span</c>.</summary>
    [Parameter] public string As { get; set; } = "span";

    /// <summary>CSS class for the container element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline style for the container element.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>CSS class applied to every animated unit, for styling individual characters/words.</summary>
    [Parameter] public string? UnitClass { get; set; }

    /// <summary>
    /// Adds <c>aria-label</c> to the container and <c>aria-hidden</c> to the split units so the
    /// text is announced once as a sentence. Default <c>true</c>.
    /// </summary>
    [Parameter] public bool Accessible { get; set; } = true;

    /// <summary>Fires once when the last unit's animation completes.</summary>
    [Parameter] public EventCallback OnComplete { get; set; }

    // ── Cached split ───────────────────────────────────────────────────────────
    private string? _splitText;
    private BmSplitBy _splitBy;
    private List<BmTextChunk> _chunks = new();
    private int _unitCount;

    protected override void OnParametersSet()
    {
        // Splitting is pure string work but runs over every character; redo it only when the text
        // or the split mode actually changes, not on every unrelated re-render.
        if (_splitText == Text && _splitBy == By && _chunks.Count > 0) return;
        _splitText = Text;
        _splitBy = By;
        _chunks = BmotionTextSplitter.Split(Text, By);
        _unitCount = BmotionTextSplitter.CountUnits(_chunks);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (string.IsNullOrEmpty(Text)) return;

        var stagger = Stagger ?? _defaultStagger;
        // Lines are blocks; characters and words are inline-blocks that must not break mid-unit.
        bool lines = By == BmSplitBy.Lines;
        string unitDisplay = lines ? "display:block;" : "display:inline-block;";

        builder.OpenElement(0, As);
        if (!string.IsNullOrEmpty(Class)) builder.AddAttribute(1, "class", Class);
        // pre-wrap keeps runs of spaces and authored newlines that HTML would otherwise collapse,
        // so the split text lays out exactly like the original string.
        builder.AddAttribute(2, "style", string.IsNullOrEmpty(Style)
            ? "white-space:pre-wrap;"
            : "white-space:pre-wrap;" + Style);
        if (Accessible)
        {
            builder.AddAttribute(3, "aria-label", Text);
            // A bare span with aria-label is not reliably announced; role="text" makes the
            // container a single accessible text node instead of a bag of hidden fragments.
            builder.AddAttribute(4, "role", "text");
        }

        // In Chars mode the characters of one word live inside a nowrap inline-block, so the word
        // wraps as a unit instead of breaking between two of its letters.
        bool groupWord = By == BmSplitBy.Chars;

        // Sequence numbers are emitted from a running counter with a fixed stride per node, so the
        // numbering depends only on the split shape - which is itself determined by Text and By.
        // Any change to either rebuilds this subtree anyway, so the numbering stays stable for
        // every render that Blazor actually diffs.
        int unitIndex = 0;
        int seq = 5;
        foreach (var chunk in _chunks)
        {
            if (chunk.IsGap)
            {
                // Whitespace (or a blank authored line) - rendered as-is so wrapping still works.
                if (lines)
                {
                    builder.OpenElement(seq++, "br");
                    builder.CloseElement();
                }
                else
                {
                    builder.OpenElement(seq++, "span");
                    builder.AddContent(seq++, chunk.Text);
                    builder.CloseElement();
                }
                continue;
            }

            if (groupWord)
            {
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "style", "display:inline-block;white-space:nowrap;");
                builder.AddAttribute(seq++, "aria-hidden", Accessible ? "true" : null);
            }

            foreach (var unit in chunk.Units)
            {
                int index = unitIndex++;
                bool last = index == _unitCount - 1;
                string unitText = unit;

                builder.OpenComponent<Bmotion>(seq++);
                builder.SetKey($"{index}:{unitText}");
                // Every parameter is passed unconditionally (a null target is identical to an
                // omitted one) so each unit occupies the same fixed number of sequence slots.
                builder.AddComponentParameter(seq++, nameof(Bmotion.Initial), Initial);
                builder.AddComponentParameter(seq++, nameof(Bmotion.Animate), Animate);
                builder.AddComponentParameter(seq++, nameof(Bmotion.Exit), Exit);
                builder.AddComponentParameter(seq++, nameof(Bmotion.WhileHover), WhileHover);
                builder.AddComponentParameter(seq++, nameof(Bmotion.WhileTap), WhileTap);
                builder.AddComponentParameter(seq++, nameof(Bmotion.WhileInView), WhileInView);
                builder.AddComponentParameter(seq++, nameof(Bmotion.Once), Once);
                builder.AddComponentParameter(seq++, nameof(Bmotion.Viewport), Viewport);
                builder.AddComponentParameter(seq++, nameof(Bmotion.Transition), StaggeredTransition(stagger, index));
                builder.AddComponentParameter(seq++, nameof(Bmotion.OnAnimationComplete),
                    last && OnComplete.HasDelegate
                        ? EventCallback.Factory.Create<BmProps?>(this, _ => OnComplete.InvokeAsync())
                        : default(EventCallback<BmProps?>));

                builder.AddComponentParameter(seq++, nameof(Bmotion.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenElement(0, "span");
                    b.AddAttribute(1, "class", UnitClass);
                    b.AddAttribute(2, "style", unitDisplay);
                    // A word grouper already hides the whole word; only tag the unit when it is
                    // the outermost split element, so the tree carries one aria-hidden per branch.
                    b.AddAttribute(3, "aria-hidden", Accessible && !groupWord ? "true" : null);
                    b.AddContent(4, unitText);
                    b.CloseElement();
                }));
                builder.CloseComponent();
            }

            if (groupWord) builder.CloseElement();
        }

        builder.CloseElement();
    }

    private static readonly BmStagger _defaultStagger = new(0.03);

    /// <summary>
    /// The transition for the unit at <paramref name="index"/>: the configured transition with the
    /// stagger offset added to its delay. Copies rather than mutating, so one shared
    /// <see cref="Transition"/> instance drives every unit without them fighting over its delay.
    /// </summary>
    private BmTransition StaggeredTransition(BmStagger stagger, int index)
    {
        double offset = stagger.DelayFor(index, _unitCount);
        var basis = Transition;
        if (basis is null) return offset > 0 ? new BmTween { Delay = offset } : new BmTween();
        return offset > 0 ? basis.WithDelay(basis.Delay + offset) : basis;
    }
}
