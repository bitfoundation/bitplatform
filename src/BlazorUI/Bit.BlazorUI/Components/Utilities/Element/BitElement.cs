using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// A simple component with a customizable HTML tag, offering full control over styling, attributes, and directional flow, designed to integrate seamlessly into the bit BlazorUI.
/// </summary>
/// <remarks>
/// The element is the escape hatch of the library: it renders whatever tag <see cref="Element"/> names - an "a", a "button", an
/// "input", a list item, an SVG shape or a custom element - while still going through the same parameters every other component
/// of the library offers, so the markup a page needs stays reachable without dropping out of the bit BlazorUI parameter set.
/// <br />
/// Anything that is not a parameter of the component is splatted onto that tag, which is what makes it useful as a dynamic tag:
/// the attributes and the event handlers a page writes on it (href, placeholder, data-*, @onclick, ...) land on whichever element
/// is rendered. Attributes the component builds itself are merged with the splatted ones rather than replacing them, so a "class"
/// or a "style" written as a plain HTML attribute is kept alongside <see cref="BitComponentBase.Class"/> and <see cref="BitComponentBase.Style"/>.
/// <br />
/// The tag decides the rest of the behavior: a void element such as "input" or "img" holds no content, so <see cref="ChildContent"/>
/// is not rendered into it, and a disabled element gets the "disabled" attribute only where HTML defines one, plus "aria-disabled"
/// and a tabindex of -1 everywhere else, so that a tag HTML cannot disable is at least taken out of the tab order. A disabled
/// "a" also loses its "href", which is what keeps the link itself from being focused and followed.
/// <see cref="NoWrapper"/> removes the tag altogether and leaves only the content behind, which is what turns the component into a
/// conditional wrapper. <see cref="StopPropagation"/> and <see cref="PreventDefault"/>, and <see cref="StopPropagationEvents"/> and
/// <see cref="PreventDefaultEvents"/> for the events other than the click, reach the event modifiers that Razor only offers on plain
/// elements.
/// <br />
/// The tag name is used as written, but it is only used at all while it is a name a tag can have: a value carrying whitespace or
/// any of the characters that end a tag falls back to the default one, since a tag name that reached the rendered markup as it is
/// would be a way to write markup rather than to name an element. That is a last line of defense and not a license - a tag name
/// taken from untrusted input still decides what the browser runs, since "script" and "iframe" are names a tag can have, so the
/// value belongs to the page rather than to its data.
/// </remarks>
public partial class BitElement : BitComponentBase
{
    // The HTML void elements: they are defined to have no content at all, so a closing tag and any child content are
    // invalid markup in them. The static HTML renderer writes them self-closed and silently drops whatever follows.
    // The obsolete four (basefont, bgsound, frame and keygen) are in the list the HTML parser itself treats as void,
    // so a browser drops their content just the same and they belong here with the rest.
    private static readonly HashSet<string> _voidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "basefont", "bgsound", "br", "col", "embed", "frame", "hr",
        "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr"
    };

    // The elements HTML defines a "disabled" content attribute on. On anything else the attribute is meaningless
    // markup, so a disabled element of another tag is only reported to assistive technologies through aria-disabled.
    private static readonly HashSet<string> _disableableElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "button", "fieldset", "input", "optgroup", "option", "select", "textarea"
    };

    // The characters that would end the tag or begin an attribute inside it if they reached the rendered markup as
    // part of the tag name. Whitespace and the control characters are refused along with them, and the name has to
    // begin with an ASCII letter, which is what the HTML parser requires before it reads a tag name at all.
    private static readonly char[] _invalidElementChars = ['<', '>', '/', '=', '&', '"', '\'', '`'];



    /// <summary>
    /// Gets or sets the cascading parameters for the element component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple element components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitElementParams.ParamName)]
    public BitElementParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content of the element.
    /// </summary>
    /// <remarks>
    /// A void element (such as "input", "img", "br" or "hr") is defined to hold no content, so the content is not
    /// rendered into it. The same applies while <see cref="NoWrapper"/> is set, except that there the content is all
    /// that is rendered.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    /// <remarks>
    /// Any tag name is accepted, including SVG elements and custom elements, and the value is used as written, since
    /// SVG tag names ("linearGradient", "clipPath", ...) are case sensitive. An empty or whitespace value falls back
    /// to the default tag, and so does a value that is not a name a tag can have: one that does not begin with a
    /// letter, or that carries whitespace or any of the characters that would end the tag in the rendered markup.
    /// <br />
    /// The tag can be changed between renders, which replaces the rendered element with a new one of the new tag.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Renders only the content of the element, without the wrapping HTML tag.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the conditional wrapper: the same markup wraps its content in a tag or leaves it untouched depending on
    /// a condition, without repeating the content in both branches of an if statement.
    /// <br />
    /// Nothing is rendered but the content, so everything that would land on an element - <see cref="Element"/>, the class,
    /// the style, the id, the direction, the splatted attributes and the event handlers - has nowhere to go and is ignored,
    /// and <see cref="BitComponentBase.RootElement"/> is never captured. The one exception is a
    /// <see cref="BitVisibility.Collapsed"/> <see cref="BitComponentBase.Visibility"/>, which asks for the component to be
    /// out of the DOM and needs no element of its own to say so: nothing is rendered at all, not even the content.
    /// </remarks>
    [Parameter] public bool NoWrapper { get; set; }

    /// <summary>
    /// Prevents the default browser action of the click event of the element.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the "@onclick:preventDefault" directive of a plain HTML element, which Razor does not accept on a component.
    /// It is what keeps an "a" from navigating or a "button" of type submit from submitting the form around it, while the
    /// click handler still runs.
    /// </remarks>
    [Parameter] public bool PreventDefault { get; set; }

    /// <summary>
    /// The names of the events whose default browser action is prevented on the element.
    /// </summary>
    /// <remarks>
    /// This is <see cref="PreventDefault"/> for every other event: the ":preventDefault" directive Razor only accepts on a
    /// plain HTML element, for the events that have no parameter of their own here - the "contextmenu" of a custom menu, the
    /// "dragover" of a drop target, the "submit" of a form, the "keydown" of a key the page handles itself.
    /// <br />
    /// The names are the DOM event names, with or without the "on" prefix ("contextmenu" and "oncontextmenu" name the same
    /// event), and a name is a modifier of the element rather than a handler of the event: the default action is prevented
    /// whether or not a handler of that event is written beside it.
    /// </remarks>
    [Parameter] public IEnumerable<string>? PreventDefaultEvents { get; set; }

    /// <summary>
    /// Stops the click event of the element from bubbling up to its ancestors.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the "@onclick:stopPropagation" directive of a plain HTML element, which Razor does not accept on a component.
    /// It is what keeps a click on a control inside a clickable card from also triggering the card.
    /// </remarks>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// The names of the events that are stopped from bubbling up from the element to its ancestors.
    /// </summary>
    /// <remarks>
    /// This is <see cref="StopPropagation"/> for every other event: the ":stopPropagation" directive Razor only accepts on a
    /// plain HTML element, for the events that have no parameter of their own here - the "dblclick", the "pointerdown" or the
    /// "keydown" that a control inside a listening container handles on its own.
    /// <br />
    /// The names are the DOM event names, with or without the "on" prefix ("dblclick" and "ondblclick" name the same event),
    /// and a name is a modifier of the element rather than a handler of the event: the bubbling stops at this element
    /// whether or not a handler of that event is written beside it.
    /// </remarks>
    [Parameter] public IEnumerable<string>? StopPropagationEvents { get; set; }



    /// <summary>
    /// Gives the browser focus to the rendered element.
    /// </summary>
    /// <remarks>
    /// The element has to be one the browser can focus for the call to have an effect: a tag that is focusable of itself
    /// (an "a" with an "href", a "button", an "input", ...) or any other tag carrying a <see cref="BitComponentBase.TabIndex"/>.
    /// <br />
    /// Nothing is rendered while <see cref="NoWrapper"/> is set and nothing is captured before the first render, so there the
    /// call does nothing rather than fail on an element that is not there.
    /// </remarks>
    public ValueTask FocusAsync()
    {
        return RootElement.Context is null ? ValueTask.CompletedTask : RootElement.FocusAsync();
    }



    protected override string RootElementClass => "bit-elm";

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitElementParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (NoWrapper)
        {
            // No element is rendered any more, so the reference to the one a previous render captured is stale.
            RootElement = default;

            // A collapsed component is one that is asked to be out of the DOM, which is the one thing about an element
            // that still means something without an element: the content it would have wrapped goes with it.
            if (Visibility != BitVisibility.Collapsed)
            {
                builder.AddContent(0, ChildContent);
            }

            base.BuildRenderTree(builder);

            return;
        }

        var element = Element?.Trim();
        if (element.HasNoValue() || IsValidElement(element!) is false)
        {
            element = "div";
        }

        var disabled = IsEnabled is false;
        // HTML only defines the disabled attribute on the form elements, so everywhere else the state has to be carried
        // by the aria attribute and by the tab order rather than by the browser's own handling of the attribute.
        var nativelyDisabled = disabled && _disableableElements.Contains(element!);
        // An anchor is the one tag that is reachable and activatable through its own href whatever the tab order says:
        // it stays focusable programmatically, and the enter key on a focused link follows the href without a click the
        // pointer-events of the disabled class could stop. Dropping the href is what disables the link itself.
        var disabledLink = disabled && string.Equals(element, "a", StringComparison.OrdinalIgnoreCase);

        builder.OpenElement(0, element!);
        // The splatted attributes come first so everything the component builds itself is written over them. The values
        // the component would otherwise write as null are resolved against them below, since a null written over a
        // splatted attribute does not leave that attribute alone - it removes it.
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", Id.HasValue() ? Id : (GetSplattedAttribute("id") ?? _Id));
        builder.AddAttribute(3, "style", JoinStyles(GetSplattedAttribute("style"), StyleBuilder.Value));
        builder.AddAttribute(4, "class", JoinClasses(ClassBuilder.Value, GetSplattedAttribute("class")));
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLower() ?? GetSplattedAttribute("dir"));
        // A tag HTML has no disabled attribute for keeps its keyboard tab stop while it is disabled, and the pointer
        // events the disabled class turns off are only one of the two ways to reach it, so the tab stop goes as well.
        builder.AddAttribute(6, "tabindex", disabled && nativelyDisabled is false
                                            ? "-1"
                                            : (TabIndex ?? GetSplattedAttribute("tabindex")));
        builder.AddAttribute(7, "aria-label", AriaLabel ?? GetSplattedAttribute("aria-label"));
        // The attribute is only written while the element is disabled, since writing it as false over a splatted
        // disabled attribute would remove that one rather than leave it alone.
        if (nativelyDisabled)
        {
            builder.AddAttribute(8, "disabled", true);
        }
        // What keeps a disabled element announced as disabled rather than as missing, whichever tag it renders.
        builder.AddAttribute(9, "aria-disabled", disabled ? "true" : GetSplattedAttribute("aria-disabled"));
        // Written over the splatted href of a disabled anchor, which is what takes the link out of the tab order the
        // browser builds of itself and leaves nothing for the enter key to follow; every other tag has no href to lose.
        builder.AddAttribute(10, "href", disabledLink ? null : GetSplattedAttribute("href"));
        // The event modifiers of a plain element, which the razor compiler refuses on a component. The two of the click
        // are written unconditionally so that turning either of them off again is a value change the renderer can see
        // and undo; the ones of the other events are only there while they are named.
        builder.AddEventStopPropagationAttribute(11, "onclick", StopPropagation);
        builder.AddEventPreventDefaultAttribute(12, "onclick", PreventDefault);
        var seq = 13;
        foreach (var name in StopPropagationEvents ?? [])
        {
            var @event = NormalizeEventName(name);
            if (@event is null) continue;

            builder.AddEventStopPropagationAttribute(seq++, @event, true);
        }
        foreach (var name in PreventDefaultEvents ?? [])
        {
            var @event = NormalizeEventName(name);
            if (@event is null) continue;

            builder.AddEventPreventDefaultAttribute(seq++, @event, true);
        }
        // The two frames below follow a block of frames as long as the two lists above, so their sequence numbers are
        // pinned past the end of it: a sequence number that moved with the length of a list would look to the renderer
        // like a different frame, and the content of the element would be thrown away and built again.
        builder.AddElementReferenceCapture(1000, v => RootElement = v);
        // A void element is defined to hold no content: the static renderer writes it self-closed, so anything put
        // inside it would either be dropped or end up as a sibling of the element in the rendered markup.
        if (_voidElements.Contains(element!) is false)
        {
            builder.AddContent(1001, ChildContent);
        }
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }



    // A tag name is only a tag name while the markup it is written into still reads it as one: the HTML parser only
    // begins a tag at all when a letter follows the "<", and it ends the name at the first whitespace and the tag at
    // the first ">", so a name carrying either of them would write markup of its own rather than name an element.
    private static bool IsValidElement(string element)
    {
        if (char.IsAsciiLetter(element[0]) is false) return false;

        foreach (var @char in element)
        {
            if (char.IsWhiteSpace(@char) || char.IsControl(@char)) return false;

            if (_invalidElementChars.Contains(@char)) return false;
        }

        return true;
    }

    // The event names the renderer matches a modifier against are the "on" prefixed ones, which is also how a handler
    // is written in a razor file, so a name given without the prefix is the same event written the other way.
    private static string? NormalizeEventName(string? name)
    {
        var @event = name?.Trim().ToLowerInvariant();

        if (@event.HasNoValue()) return null;

        return @event!.StartsWith("on") ? @event : $"on{@event}";
    }

    // The value of an attribute the page wrote as plain HTML rather than as a parameter of the component. HTML
    // attribute names are case insensitive, and so is the deduplication the render tree does between a splatted
    // attribute and one the component writes itself, so a differently cased spelling has to be found here too.
    private string? GetSplattedAttribute(string name)
    {
        if (HtmlAttributes.Count == 0) return null;

        if (HtmlAttributes.TryGetValue(name, out var value)) return value?.ToString();

        foreach (var attribute in HtmlAttributes)
        {
            if (string.Equals(attribute.Key, name, StringComparison.OrdinalIgnoreCase)) return attribute.Value?.ToString();
        }

        return null;
    }

    // Two class lists landing in the same class attribute are only two lists while a space stands between them.
    private static string? JoinClasses(string? @class, string? extraClass)
    {
        if (@class.HasNoValue()) return extraClass;

        if (extraClass.HasNoValue()) return @class;

        return $"{@class} {extraClass}";
    }
}
