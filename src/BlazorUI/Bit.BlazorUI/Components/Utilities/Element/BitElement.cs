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
/// everywhere else. <see cref="NoWrapper"/> removes the tag altogether and leaves only the content behind, which is what turns the
/// component into a conditional wrapper. <see cref="StopPropagation"/> and <see cref="PreventDefault"/> reach the click modifiers
/// that Razor only offers on plain elements.
/// </remarks>
public partial class BitElement : BitComponentBase
{
    // The HTML void elements: they are defined to have no content at all, so a closing tag and any child content are
    // invalid markup in them. The static HTML renderer writes them self-closed and silently drops whatever follows.
    private static readonly HashSet<string> _voidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
    };

    // The elements HTML defines a "disabled" content attribute on. On anything else the attribute is meaningless
    // markup, so a disabled element of another tag is only reported to assistive technologies through aria-disabled.
    private static readonly HashSet<string> _disableableElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "button", "fieldset", "input", "optgroup", "option", "select", "textarea"
    };



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
    /// to the default tag.
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
    /// the style, the id, the direction, the visibility, the splatted attributes and the event handlers - has nowhere to
    /// go and is ignored, and <see cref="BitComponentBase.RootElement"/> is never captured.
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
    /// Stops the click event of the element from bubbling up to its ancestors.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the "@onclick:stopPropagation" directive of a plain HTML element, which Razor does not accept on a component.
    /// It is what keeps a click on a control inside a clickable card from also triggering the card.
    /// </remarks>
    [Parameter] public bool StopPropagation { get; set; }



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

            builder.AddContent(0, ChildContent);

            base.BuildRenderTree(builder);

            return;
        }

        var element = Element?.Trim();
        if (element.HasNoValue())
        {
            element = "div";
        }

        var disabled = IsEnabled is false;

        builder.OpenElement(0, element!);
        // The splatted attributes come first so everything the component builds itself is written over them. The values
        // the component would otherwise write as null are resolved against them below, since a null written over a
        // splatted attribute does not leave that attribute alone - it removes it.
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", Id.HasValue() ? Id : (GetSplattedAttribute("id") ?? _Id));
        builder.AddAttribute(3, "style", JoinStyles(GetSplattedAttribute("style"), StyleBuilder.Value));
        builder.AddAttribute(4, "class", JoinClasses(ClassBuilder.Value, GetSplattedAttribute("class")));
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLower() ?? GetSplattedAttribute("dir"));
        builder.AddAttribute(6, "tabindex", TabIndex ?? GetSplattedAttribute("tabindex"));
        builder.AddAttribute(7, "aria-label", AriaLabel ?? GetSplattedAttribute("aria-label"));
        // HTML only defines the disabled attribute on the form elements, so everywhere else the state is carried by
        // aria-disabled alone, which is also what keeps a disabled element announced as disabled rather than missing.
        // The attribute is only written while the element is disabled, since writing it as false over a splatted
        // disabled attribute would remove that one rather than leave it alone.
        if (disabled && _disableableElements.Contains(element!))
        {
            builder.AddAttribute(8, "disabled", true);
        }
        builder.AddAttribute(9, "aria-disabled", disabled ? "true" : GetSplattedAttribute("aria-disabled"));
        // The two click modifiers of a plain element, which the razor compiler refuses on a component. They are written
        // unconditionally so that turning either of them off again is a value change the renderer can see and undo.
        builder.AddEventStopPropagationAttribute(10, "onclick", StopPropagation);
        builder.AddEventPreventDefaultAttribute(11, "onclick", PreventDefault);
        builder.AddElementReferenceCapture(12, v => RootElement = v);
        // A void element is defined to hold no content: the static renderer writes it self-closed, so anything put
        // inside it would either be dropped or end up as a sibling of the element in the rendered markup.
        if (_voidElements.Contains(element!) is false)
        {
            builder.AddContent(13, ChildContent);
        }
        builder.CloseElement();

        base.BuildRenderTree(builder);
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
