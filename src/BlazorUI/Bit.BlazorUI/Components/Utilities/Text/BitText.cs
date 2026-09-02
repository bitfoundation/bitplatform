using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// Use text to present your design and content as clearly and efficiently as possible.
/// </summary>
/// <remarks>
/// The component is the typography ramp of the library made reachable from the markup: <see cref="Typography"/> picks
/// one of the theme's named steps - the six heading levels, the two subtitles, the two body sizes, the button, the two
/// captions and the overline - and the size, the weight, the line height and the tracking of that step all come from
/// the theme, so every preset re-skins the text of a whole application from one place.
/// <br />
/// The looks and the semantics are two separate decisions, and the component keeps them apart. The variant decides how
/// the text is drawn and, on its own, which tag it is drawn in; <see cref="Element"/> overrides that tag, so a run of
/// text can be an "h2" that reads like an "h4", or a "span" that reads like a heading, without a page having to choose
/// between the outline a screen reader navigates and the size a design asks for. <see cref="AriaLevel"/> is the third
/// of those axes, for the places where neither the tag nor the look is free to move.
/// <br />
/// Everything else the component offers is a property of a run of text rather than of a step of the ramp, so each one
/// is a parameter of its own that composes with any variant: the weight, the case, the emphasis of the
/// <see cref="Italic"/>, <see cref="Underline"/> and <see cref="Strikethrough"/>, the figure spacing of
/// <see cref="Numeric"/>, and the whole of the wrapping - <see cref="NoWrap"/> and <see cref="LineClamp"/> for the two
/// truncations, <see cref="Wrap"/> for how the remaining lines are broken, and <see cref="BreakWord"/>,
/// <see cref="ForceBreak"/> and <see cref="Hyphenate"/> for what may be broken in the middle of itself.
/// <br />
/// Anything that is not a parameter is splatted onto the rendered tag, and the attributes the component builds itself
/// are merged with the splatted ones rather than replacing them: a "class" or a "style" arriving through the
/// "@attributes" directive is kept alongside the class list and the style the component builds, and every attribute
/// the component would otherwise write as null - the id, the direction, the language, the label, the tab index - is
/// resolved against the splatted spelling of the same name instead of removing it.
/// </remarks>
public partial class BitText : BitComponentBase
{
    // The HTML void elements: they are defined to have no content at all, so a closing tag and any child content
    // are invalid markup in them. The static HTML renderer writes them self-closed and drops whatever follows.
    private static readonly HashSet<string> _voidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "basefont", "bgsound", "br", "col", "embed", "frame", "hr",
        "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr"
    };

    // The tags that are headings of their own. They carry a level the accessibility tree reads off the tag name, so
    // neither the heading role nor an aria-level has anything to add to them beyond overriding that level.
    private static readonly HashSet<string> _headingElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "h1", "h2", "h3", "h4", "h5", "h6"
    };



    /// <summary>
    /// Gets or sets the cascading parameters for the text component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple text components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitTextParams.ParamName)]
    public BitTextParams? CascadingParameters { get; set; }



    /// <summary>
    /// Sets the horizontal alignment of the text content.
    /// </summary>
    /// <remarks>
    /// <see cref="BitTextAlign.Start"/> and <see cref="BitTextAlign.End"/> are the logical values: they follow the
    /// direction of the text, so the same markup reads from the leading edge of both a left-to-right and a
    /// right-to-left page, which <see cref="BitTextAlign.Left"/> and <see cref="BitTextAlign.Right"/> deliberately do
    /// not. Prefer them unless the alignment is meant to be a physical one - a column of figures that stays on the
    /// same side whichever way the text around it runs.
    /// <br />
    /// <see cref="BitTextAlign.Justify"/> spaces the words of every line but the last so that both edges line up.
    /// WCAG advises against justifying a block of text, since the uneven word spacing it creates is harder to read
    /// for people with dyslexia and other cognitive concerns.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitTextAlign? Align { get; set; }

    /// <summary>
    /// Sets the level of the heading the text is announced as, without changing the rendered tag.
    /// </summary>
    /// <remarks>
    /// The heading level is the outline a screen reader navigates by, and it belongs to where the text sits in the
    /// document rather than to how large it is drawn. This is what keeps the two apart where the tag itself cannot
    /// move: a component reused at several depths, or a caption that has to stay a "div" for the layout around it.
    /// <br />
    /// On a tag that is not already a heading the value also writes a heading role, since a level on its own names
    /// nothing; on an "h1" to "h6" only the level is written, and it overrides the one the tag carries. A native
    /// heading tag is always the better answer where the markup is free to use one.
    /// <br />
    /// Heading levels count from one, so a value below that is left out rather than written as an attribute the
    /// accessibility tree cannot read as a level at all.
    /// </remarks>
    [Parameter] public int? AriaLevel { get; set; }

    /// <summary>
    /// Renders the text as a block level element.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A variant left inline - the captions, and whichever of the span variants the theme gives no display of its
    /// own - takes only as much width as its content, and text can only overflow a box that has a width. So this is
    /// what an inline variant needs before <see cref="NoWrap"/> has anything to truncate, and what lets
    /// <see cref="Align"/> move the text inside its container rather than leaving it where the content ends.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Block { get; set; }

    /// <summary>
    /// Breaks a word that is too long for its line rather than letting it overflow.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the gentle half of <see cref="ForceBreak"/>: a word is only broken where there is no other way to fit
    /// the line, so ordinary prose keeps breaking between its words and only a URL, a hash or a file path wider than
    /// the column is broken in the middle of itself.
    /// <br />
    /// It also lets the box become narrower than its longest word, which is what a text inside a flex or a grid item
    /// needs before that item can shrink to the width its container gives it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool BreakWord { get; set; }

    /// <summary>
    /// The content of the text.
    /// </summary>
    /// <remarks>
    /// A void element - an "img", an "hr", a "br" - is defined to hold no content, so nothing is rendered into one
    /// where <see cref="Element"/> names it.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The general color of the text.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    /// <remarks>
    /// The tag decides the semantics of the text while <see cref="Typography"/> decides its looks, and the two are
    /// set apart on purpose: an "h2" drawn at the size of an "h4" keeps the outline a screen reader navigates intact,
    /// which picking the smaller heading for its size would break. It is also how the text reaches the tags that mean
    /// something of their own and that no variant maps to - a "strong", an "em", a "blockquote", a "label", a "code",
    /// an "abbr", a "time".
    /// <br />
    /// Any tag name is accepted, and the value is used as written, since SVG tag names are case sensitive. An empty
    /// or whitespace value falls back to the tag of the variant, and so does a value that is not a name a tag can
    /// have: one that does not begin with an ASCII letter, or that carries anything but letters, digits and the "-",
    /// "_", "." and ":" that join them - a whitespace or a "&lt;" would end the tag and write markup of its own.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Forces the text to always break at the end.
    /// </summary>
    /// <remarks>
    /// The break happens wherever the line runs out rather than only where a word cannot be fitted, which is what a
    /// column of identifiers or of hexadecimal needs and what ordinary prose does not - <see cref="BreakWord"/> is
    /// the one to reach for there, since it leaves the words that do fit alone.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ForceBreak { get; set; }

    /// <summary>
    /// The kind of the foreground color of the text.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Foreground { get; set; }

    /// <summary>
    /// If true, the text will have a bottom margin.
    /// </summary>
    /// <remarks>
    /// The margin is set in "em", so it follows the size of the variant: the gap under a heading comes out larger
    /// than the one under a caption without either of them naming a length.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Gutter { get; set; }

    /// <summary>
    /// Hyphenates the words that are broken across two lines.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The browser needs to know which language the text is in before it can hyphenate it, so this only has an effect
    /// where a <see cref="Lang"/> is set here or on an ancestor and the browser carries a dictionary for that
    /// language; where it does not, the text is broken between its words as it would have been anyway. A soft hyphen
    /// written into the content itself is honoured whether or not this is set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Hyphenate { get; set; }

    /// <summary>
    /// Renders the text in italics.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is a typographic slant and nothing else. Where the slant is meant to carry a stress that changes the
    /// meaning of the sentence, an "em" through <see cref="Element"/> is what says so to a screen reader.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Italic { get; set; }

    /// <summary>
    /// The language of the text, written as the "lang" attribute of the rendered element.
    /// </summary>
    /// <remarks>
    /// A passage in another language than the page around it has to say so for a screen reader to pronounce it with
    /// the right voice rather than reading it as if it were the page's own language. The same attribute is what a
    /// browser picks a hyphenation dictionary by (see <see cref="Hyphenate"/>), what decides the case mapping of an
    /// upper cased <see cref="Transform"/> - the dotted and the dotless "i" of Turkish, the "ß" of German - and what
    /// the font fallback consults for the scripts that share code points.
    /// </remarks>
    [Parameter] public string? Lang { get; set; }

    /// <summary>
    /// Truncates the text after the given number of lines with an ellipsis.
    /// </summary>
    /// <remarks>
    /// This is the multi-line half of <see cref="NoWrap"/>: the text wraps normally up to the given line, and what is
    /// left is clipped away with the last visible line ending in an ellipsis. Only whole lines can be clamped, so a
    /// value below one leaves the text alone.
    /// <br />
    /// The clip is a paint time effect and nothing is taken out of the document, so the whole text is still copied,
    /// found by a find-in-page and read out by a screen reader. Two things follow from how a browser clamps: a bottom
    /// padding on the element shows slivers of the lines underneath the clamp, so that padding belongs on a wrapper
    /// around it, and a word wider than the box still overflows sideways unless <see cref="BreakWord"/> is set beside
    /// it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? LineClamp { get; set; }

    /// <summary>
    /// Prevents the text from being selected.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This belongs to the text that is part of the chrome rather than part of the content - the caption of a tab, a
    /// drag handle, a label a double click is meant to reach the control under. Content itself stays selectable,
    /// since a reader who cannot select a passage cannot copy it into a translator, a dictionary or a note either.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoSelect { get; set; }

    /// <summary>
    /// If true, the text will not wrap, but instead will truncate with a text overflow ellipsis.
    /// Note that text overflow can only happen with block or inline-block level elements(the element needs to have a width in order to overflow).
    /// </summary>
    /// <remarks>
    /// A variant left inline - a caption, say - has no width of its own to overflow, so <see cref="Block"/> is what
    /// gives it one; the same applies to a text inside a flex or a grid item, whose automatic minimum width is the
    /// width of its longest word until <see cref="BreakWord"/> or an "overflow" of its own says otherwise.
    /// <br />
    /// Nothing is taken out of the document, so the whole text is still copied, found and read out - which is also
    /// why a truncation is never the place for a page title, a label or an error message.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoWrap { get; set; }

    /// <summary>
    /// Renders the digits of the text at a single width, so that they line up across the lines.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// In an ordinary font a "1" is narrower than an "8", so a column of figures comes out ragged and a number that
    /// counts up or changes in place visibly shifts under the eye. This asks the font for its tabular figures, which
    /// share one width, and for its lining ones, which all sit on the baseline. The letters beside them stay
    /// proportional, and a font carrying no tabular figures is left alone.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Numeric { get; set; }

    /// <summary>
    /// Draws a line through the text.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is a decoration and carries no meaning of its own. Where the line stands for something that has been
    /// removed or is no longer accurate, a "del" or an "s" through <see cref="Element"/> is what says so. It combines
    /// with <see cref="Underline"/>, and the text is then both underlined and struck through.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Strikethrough { get; set; }

    /// <summary>
    /// The capitalization of the text.
    /// </summary>
    /// <remarks>
    /// The transform is visual only: the characters in the document are the ones that were written, so a copy and a
    /// screen reader both get the original text. The case mapping follows the <see cref="Lang"/> of the text where
    /// one is set, which is what the languages whose upper case is not a character by character mapping need.
    /// <br />
    /// A long run of upper case is harder to read, since it takes away the ascenders and the descenders that give a
    /// word its shape, so this belongs to short labels rather than to body copy.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitTextTransform? Transform { get; set; }

    /// <summary>
    /// The typography of the text.
    /// </summary>
    /// <remarks>
    /// The variant decides the size, the weight, the line height and the tracking of the text, all of them read from
    /// the theme, and - unless <see cref="Element"/> says otherwise - the tag it is rendered in: the six heading
    /// variants render their own heading tag, the two subtitles an "h6", the two body variants and the inherit one a
    /// "p", and the button, the two captions and the overline a "span".
    /// <br />
    /// <see cref="BitTypography.Inherit"/> takes every one of those from the element around it instead, which is what
    /// a run of text inside an already styled block needs to keep the look of its surroundings while still taking the
    /// colors, the wrapping and the rest of the parameters of the component.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitTypography? Typography { get; set; }

    /// <summary>
    /// Underlines the text.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// An underline is what readers have been taught to read as a link, so underlining a run of text that is not one
    /// is worth avoiding outside the places where the convention says otherwise. It combines with
    /// <see cref="Strikethrough"/>, and the text is then both underlined and struck through.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Underline { get; set; }

    /// <summary>
    /// Removes the text from the page while keeping it available to assistive technologies.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The text is still in the document and still announced, which neither a <see cref="BitVisibility.Hidden"/> nor
    /// a <see cref="BitVisibility.Collapsed"/> text is. It belongs to the context the design around it already gives
    /// a sighted reader and that a screen reader would otherwise miss - the heading of a section whose meaning its
    /// layout carries, the unit of a figure, an "opens in a new window" after a link.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool VisuallyHidden { get; set; }

    /// <summary>
    /// How the lines of the text are broken.
    /// </summary>
    /// <remarks>
    /// <see cref="BitTextWrap.Balance"/> is for a heading of a few lines and <see cref="BitTextWrap.Pretty"/> for
    /// body copy; both only ask for better break points, so an engine that does not implement one lays the text out
    /// the way it would have anyway.
    /// <br />
    /// <see cref="NoWrap"/> and <see cref="LineClamp"/> have the last word over this: a text truncated on a single
    /// line does not wrap whatever is asked for here, and a clamped one always wraps.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitTextWrap? Wrap { get; set; }

    /// <summary>
    /// The font weight of the text.
    /// </summary>
    /// <remarks>
    /// The steps come from the theme's weight scale rather than being numbers of their own, so a preset that draws
    /// its semibold somewhere else moves this with it. Left unset, the weight is the one the
    /// <see cref="Typography"/> variant carries.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitFontWeight? Weight { get; set; }



    protected override string RootElementClass => "bit-txt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => $"bit-txt-{(Typography ?? BitTypography.Subtitle1).ToString().ToLower(CultureInfo.InvariantCulture)}")
                    .Register(() => NoWrap ? "bit-txt-nowrap" : string.Empty)
                    .Register(() => Gutter ? "bit-txt-gutter" : string.Empty);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-txt-pri",
            BitColor.Secondary => "bit-txt-sec",
            BitColor.Tertiary => "bit-txt-ter",
            BitColor.Info => "bit-txt-inf",
            BitColor.Success => "bit-txt-suc",
            BitColor.Warning => "bit-txt-wrn",
            BitColor.SevereWarning => "bit-txt-swr",
            BitColor.Error => "bit-txt-err",
            BitColor.PrimaryBackground => "bit-txt-pbg",
            BitColor.SecondaryBackground => "bit-txt-sbg",
            BitColor.TertiaryBackground => "bit-txt-tbg",
            BitColor.PrimaryForeground => "bit-txt-pfg",
            BitColor.SecondaryForeground => "bit-txt-sfg",
            BitColor.TertiaryForeground => "bit-txt-tfg",
            BitColor.PrimaryBorder => "bit-txt-pbr",
            BitColor.SecondaryBorder => "bit-txt-sbr",
            BitColor.TertiaryBorder => "bit-txt-tbr",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Foreground switch
        {
            BitColorKind.Primary => "bit-txt-pfg",
            BitColorKind.Secondary => "bit-txt-sfg",
            BitColorKind.Tertiary => "bit-txt-tfg",
            BitColorKind.Transparent => "bit-txt-rfg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => ForceBreak ? "bit-txt-fbr" : string.Empty);

        ClassBuilder.Register(() => Weight switch
        {
            BitFontWeight.Light => "bit-txt-fwl",
            BitFontWeight.Regular => "bit-txt-fwr",
            BitFontWeight.Medium => "bit-txt-fwm",
            BitFontWeight.Semibold => "bit-txt-fws",
            BitFontWeight.Bold => "bit-txt-fwb",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Transform switch
        {
            BitTextTransform.None => "bit-txt-trn",
            BitTextTransform.Uppercase => "bit-txt-tru",
            BitTextTransform.Lowercase => "bit-txt-trl",
            BitTextTransform.Capitalize => "bit-txt-trc",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Wrap switch
        {
            BitTextWrap.Wrap => "bit-txt-wrp",
            BitTextWrap.NoWrap => "bit-txt-wnw",
            BitTextWrap.Balance => "bit-txt-wbl",
            BitTextWrap.Pretty => "bit-txt-wpr",
            BitTextWrap.Stable => "bit-txt-wst",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Italic ? "bit-txt-itl" : string.Empty)
                    .Register(() => Underline ? "bit-txt-und" : string.Empty)
                    .Register(() => Strikethrough ? "bit-txt-stk" : string.Empty)
                    .Register(() => Numeric ? "bit-txt-num" : string.Empty)
                    .Register(() => Hyphenate ? "bit-txt-hyp" : string.Empty)
                    .Register(() => BreakWord ? "bit-txt-brw" : string.Empty)
                    .Register(() => NoSelect ? "bit-txt-nsl" : string.Empty)
                    .Register(() => Block ? "bit-txt-blk" : string.Empty)
                    .Register(() => LineClamp > 0 ? "bit-txt-clp" : string.Empty)
                    .Register(() => VisuallyHidden ? "bit-txt-vhd" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Align.HasValue is false ? null :
            $"text-align:{Align switch
            {
                BitTextAlign.Start => "start",
                BitTextAlign.End => "end",
                BitTextAlign.Left => "left",
                BitTextAlign.Right => "right",
                BitTextAlign.Center => "center",
                BitTextAlign.Justify => "justify",
                BitTextAlign.JustifyAll => "justify-all",
                BitTextAlign.MatchParent => "match-parent",
                BitTextAlign.Inherit => "inherit",
                BitTextAlign.Initial => "initial",
                BitTextAlign.Revert => "revert",
                BitTextAlign.RevertLayer => "revert-layer",
                BitTextAlign.Unset => "unset",
                _ => "start"
            }}");

        // The number of lines is the one thing about a clamp that is not a fixed declaration, so it is the one thing
        // written inline while the class carries the rest of it. Both the prefixed property every engine ships today
        // and the standard one they are implementing are written, so the standard one takes over of its own accord.
        StyleBuilder.Register(() => LineClamp > 0
                                    ? $"-webkit-line-clamp:{LineClamp!.Value.ToString(CultureInfo.InvariantCulture)};line-clamp:{LineClamp!.Value.ToString(CultureInfo.InvariantCulture)}"
                                    : null);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitTextParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);
        base.OnParametersSet();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var element = Element?.Trim();
        if (element.HasNoValue() || IsValidElement(element!) is false)
        {
            element = GetVariantElement(Typography ?? BitTypography.Subtitle1);
        }

        var isHeading = _headingElements.Contains(element!);
        // A heading level counts from one, and a level of zero or below is one the accessibility tree cannot read
        // as anything: it is left out rather than written as an attribute a screen reader has to recover from.
        var ariaLevel = AriaLevel >= 1 ? AriaLevel : null;

        builder.OpenElement(0, element!);
        // The splatted attributes come first so everything the component builds itself is written over them. The
        // values the component would otherwise write as null are resolved against them below, since a null written
        // over a splatted attribute does not leave that attribute alone - it removes it.
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", Id.HasValue() ? Id : (GetSplattedAttribute("id") ?? _Id));
        builder.AddAttribute(3, "style", JoinStyles(GetSplattedAttribute("style"), StyleBuilder.Value));
        builder.AddAttribute(4, "class", JoinClasses(ClassBuilder.Value, GetSplattedAttribute("class")));
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLower(CultureInfo.InvariantCulture) ?? GetSplattedAttribute("dir"));
        builder.AddAttribute(6, "lang", Lang.HasValue() ? Lang : GetSplattedAttribute("lang"));
        builder.AddAttribute(7, "aria-label", AriaLabel ?? GetSplattedAttribute("aria-label"));
        // A run of text is not focusable of itself, so the tab index is only ever written by a page that means to
        // reach it - a "-1" for a message the validation of a form moves the focus to, for instance.
        builder.AddAttribute(8, "tabindex", TabIndex ?? GetSplattedAttribute("tabindex"));
        // A level on a tag that is not a heading names nothing on its own, so the role is what makes it a heading;
        // on a heading tag the level alone is written, and it overrides the one the tag itself carries.
        builder.AddAttribute(9, "role", ariaLevel.HasValue && isHeading is false ? "heading" : GetSplattedAttribute("role"));
        builder.AddAttribute(10, "aria-level", ariaLevel.HasValue
                                               ? ariaLevel.Value.ToString(CultureInfo.InvariantCulture)
                                               : GetSplattedAttribute("aria-level"));
        builder.AddElementReferenceCapture(11, v => RootElement = v);
        // A void element is defined to hold no content: the static renderer writes it self-closed, so anything put
        // inside it would either be dropped or end up as a sibling of the element in the rendered markup.
        if (_voidElements.Contains(element!) is false)
        {
            builder.AddContent(12, ChildContent);
        }
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }



    // The tag a variant renders of its own. The dictionary below stays the source of the mapping, so a variant that
    // is not in it - a value cast in from outside the enum - falls back to the tag of the default variant rather
    // than throwing where the element is built.
    private static string GetVariantElement(BitTypography typography)
    {
        return _VariantMapping.TryGetValue(typography, out var element) ? element : "h6";
    }

    // The same reading of a tag name as BitElement's: what a name is made of rather than what it must not contain,
    // since the engines disagree over which symbols name an element and one they refuse throws where it is built.
    private static bool IsValidElement(string element)
    {
        if (char.IsAsciiLetter(element[0]) is false) return false;

        foreach (var @char in element)
        {
            if (char.IsAsciiLetterOrDigit(@char)) continue;

            if (@char is '-' or '_' or '.' or ':') continue;

            // Everything outside ASCII that is a letter or a digit is a name of some alphabet; the rest of it - the
            // separators, the punctuation, the C1 controls - is refused along with the ASCII symbols and whitespace.
            if (char.IsAscii(@char) is false && char.IsLetterOrDigit(@char)) continue;

            return false;
        }

        return true;
    }



    protected static readonly Dictionary<BitTypography, string> _VariantMapping = new()
    {
        { BitTypography.H1, "h1" },
        { BitTypography.H2, "h2" },
        { BitTypography.H3, "h3" },
        { BitTypography.H4, "h4" },
        { BitTypography.H5, "h5" },
        { BitTypography.H6, "h6" },
        { BitTypography.Subtitle1, "h6" },
        { BitTypography.Subtitle2, "h6" },
        { BitTypography.Body1, "p" },
        { BitTypography.Body2, "p" },
        { BitTypography.Button, "span" },
        { BitTypography.Caption1, "span" },
        { BitTypography.Caption2, "span" },
        { BitTypography.Overline, "span" },
        { BitTypography.Inherit, "p" },
    };
}
