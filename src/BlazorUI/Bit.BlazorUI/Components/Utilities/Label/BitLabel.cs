using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// Labels give a name or a title to a control or to a group of controls, including text fields, check boxes,
/// combo boxes, radio buttons and drop-down menus.
/// </summary>
/// <remarks>
/// The component renders a native "label" element, so the browser does the binding of itself: clicking the label
/// moves the focus to the control it names, and a screen reader announces that control by the label's text. The
/// control is named either by <see cref="For"/>, which points at the id of a control anywhere on the page, or by
/// putting the control inside the label's own content.
/// <br />
/// A native label may only name a single form control, which is why <see cref="Element"/> is there: the caption of a
/// group of controls - a set of radio buttons, a pair of range inputs - is not a label element but a plain element
/// referenced by the group's aria-labelledby, and naming another tag here is what renders it as one while keeping
/// the same look.
/// <br />
/// <see cref="Required"/> and <see cref="Optional"/> render the necessity of the field beside the text. Only one of
/// the two is ever rendered - a required field is not an optional one - and the default required asterisk is a
/// decoration that assistive technologies are meant to skip, since what makes a field announced as required is the
/// "required" (or "aria-required") attribute of the control itself, not a star in the text beside it.
/// </remarks>
public partial class BitLabel : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the label component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple label components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitLabelParams.ParamName)]
    public BitLabelParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content of the label, which can be a text or any custom markup.
    /// </summary>
    /// <remarks>
    /// A form control put inside this content is named by the label without needing <see cref="For"/>, which is the
    /// implicit association of HTML and the one way to label a control that has no id of its own.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the label.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLabelClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the label.
    /// </summary>
    /// <remarks>
    /// The label inherits the color of its container while this is not set, which is what keeps it in step with the
    /// text around it. Setting it is how a caption is given a meaning of its own - an <see cref="BitColor.Error"/> one
    /// beside a field that failed its validation, a <see cref="BitColor.SecondaryForeground"/> one for a caption that
    /// should sit back from the content it names.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "label".
    /// </summary>
    /// <remarks>
    /// A native label may only name one form control, so the caption of a group of controls - a set of radio buttons,
    /// a pair of inputs making up a range - is rendered as a plain element ("div", "span", "legend", ...) that the
    /// group points at through its own aria-labelledby, rather than as a label that would name only the first control
    /// in it.
    /// <br />
    /// The value is used as written and only while it is a name a tag can have: a letter followed by letters, digits
    /// and the "-", "_", "." and ":" that join them. Anything else falls back to the default "label", since a name
    /// carrying a whitespace or a "&lt;" would write markup of its own rather than name an element.
    /// <br />
    /// <see cref="For"/> is only rendered while the element is a label, since the "for" attribute belongs to the label
    /// element alone and means nothing on any other tag.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// The id of the form control this label is bound to, rendered as the "for" attribute of the label element.
    /// </summary>
    /// <remarks>
    /// This is the explicit association of HTML: the value is the id of the control, not its name, and the control may
    /// sit anywhere on the page. It is ignored while <see cref="Element"/> renders a tag other than a label.
    /// </remarks>
    [Parameter] public string? For { get; set; }

    /// <summary>
    /// Prevents the text of the label from being selected.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A click on a label is forwarded to the control it names, so a double click on one - whose second click lands on
    /// the label rather than on the control - selects the label's text instead of doing anything to the control. This
    /// turns that selection off for the labels where it is only ever an accident, such as the one over a checkbox or a
    /// toggle that a user is expected to click.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoSelect { get; set; }

    /// <summary>
    /// Keeps the label on a single line and truncates the overflow with an ellipsis.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The label wraps by default and breaks a word too long for its line rather than overflowing it. Truncation needs
    /// a width to overflow: the label is a block element whichever tag <see cref="Element"/> renders, so it takes the
    /// width of its container, and one given a display of its own through the style needs a width of its own for the
    /// ellipsis to appear.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoWrap { get; set; }

    /// <summary>
    /// Whether the associated field is optional, which renders an indicator after the content of the label.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the other half of <see cref="Required"/>, for the forms where most of the fields are required and it is
    /// the exceptions that are worth marking. It is ignored while <see cref="Required"/> is set, and unlike the
    /// required asterisk the indicator is a word rather than a symbol, so it is announced by assistive technologies
    /// along with the rest of the label.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Optional { get; set; }

    /// <summary>
    /// The custom template of the optional indicator of the label.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="OptionalText"/>, and is only rendered while <see cref="Optional"/> is set.
    /// </remarks>
    [Parameter] public RenderFragment? OptionalTemplate { get; set; }

    /// <summary>
    /// The text of the optional indicator of the label. The default is "(optional)".
    /// </summary>
    /// <remarks>
    /// This is what a localized form replaces the English default with. It is ignored while
    /// <see cref="OptionalTemplate"/> is set, and is only rendered while <see cref="Optional"/> is set.
    /// </remarks>
    [Parameter] public string? OptionalText { get; set; }

    /// <summary>
    /// Whether the associated field is required, which renders an indicator after the content of the label.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The indicator is an asterisk by default, and a default asterisk is hidden from assistive technologies: a star
    /// read out in the middle of a caption says nothing about the field, and what makes a field announced as required
    /// is the "required" or "aria-required" attribute of the control itself. An indicator given its own
    /// <see cref="RequiredText"/> or <see cref="RequiredTemplate"/> is announced with the rest of the label instead,
    /// since a word put there deliberately is meant to be read.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Required { get; set; }

    /// <summary>
    /// The custom template of the required indicator of the label.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="RequiredText"/>, and is only rendered while <see cref="Required"/> is set. The
    /// indicator it renders is announced by assistive technologies, unlike the default asterisk.
    /// </remarks>
    [Parameter] public RenderFragment? RequiredTemplate { get; set; }

    /// <summary>
    /// The text of the required indicator of the label. The default is "*".
    /// </summary>
    /// <remarks>
    /// This is what turns the asterisk into another mark or into a word ("(required)"). It is ignored while
    /// <see cref="RequiredTemplate"/> is set, and is only rendered while <see cref="Required"/> is set. The indicator
    /// it renders is announced by assistive technologies, unlike the default asterisk.
    /// </remarks>
    [Parameter] public string? RequiredText { get; set; }

    /// <summary>
    /// The size of the label.
    /// </summary>
    /// <remarks>
    /// The default is the medium size, which is the one matching the labels the input components of the library render
    /// of their own.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the label.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitLabelClassStyles? Styles { get; set; }

    /// <summary>
    /// Removes the label from the page while keeping it available to assistive technologies.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the accessible way to leave a control without a visible caption: the label is still in the accessibility
    /// tree and still names its control, which neither a <see cref="BitVisibility.Hidden"/> nor a
    /// <see cref="BitVisibility.Collapsed"/> label is. A visible caption is what most users need, so this belongs to
    /// the places where the surrounding design already says what the control is - a search box with a magnifier in it,
    /// a cell of a table whose column header names the field.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool VisuallyHidden { get; set; }



    protected override string RootElementClass => "bit-lbl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-lbl-sm",
            BitSize.Medium => "bit-lbl-md",
            BitSize.Large => "bit-lbl-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-lbl-pri",
            BitColor.Secondary => "bit-lbl-sec",
            BitColor.Tertiary => "bit-lbl-ter",
            BitColor.Info => "bit-lbl-inf",
            BitColor.Success => "bit-lbl-suc",
            BitColor.Warning => "bit-lbl-wrn",
            BitColor.SevereWarning => "bit-lbl-swr",
            BitColor.Error => "bit-lbl-err",
            BitColor.PrimaryBackground => "bit-lbl-pbg",
            BitColor.SecondaryBackground => "bit-lbl-sbg",
            BitColor.TertiaryBackground => "bit-lbl-tbg",
            BitColor.PrimaryForeground => "bit-lbl-pfg",
            BitColor.SecondaryForeground => "bit-lbl-sfg",
            BitColor.TertiaryForeground => "bit-lbl-tfg",
            BitColor.PrimaryBorder => "bit-lbl-pbr",
            BitColor.SecondaryBorder => "bit-lbl-sbr",
            BitColor.TertiaryBorder => "bit-lbl-tbr",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Required ? "bit-lbl-req" : (Optional ? "bit-lbl-opt" : string.Empty));

        ClassBuilder.Register(() => NoWrap ? "bit-lbl-nwr" : string.Empty);

        ClassBuilder.Register(() => NoSelect ? "bit-lbl-nsl" : string.Empty);

        ClassBuilder.Register(() => VisuallyHidden ? "bit-lbl-vhd" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitLabelParams))]
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
            element = "label";
        }

        var isLabelElement = string.Equals(element, "label", StringComparison.OrdinalIgnoreCase);

        builder.OpenElement(0, element!);
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "style", StyleBuilder.Value);
        builder.AddAttribute(4, "class", ClassBuilder.Value);
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLower(CultureInfo.InvariantCulture));
        builder.AddAttribute(6, "aria-label", AriaLabel);
        // A label is not focusable of itself, so the tab index is only ever written by a page that means to reach it -
        // a "-1" for a caption the validation of a form moves the focus to, for instance.
        builder.AddAttribute(7, "tabindex", TabIndex);
        // The "for" attribute is defined on the label element alone: on any other tag it is markup that names nothing,
        // and a group caption rendered through Element is pointed at by its group rather than pointing at a control.
        builder.AddAttribute(8, "for", isLabelElement ? For : null);
        builder.AddElementReferenceCapture(9, v => RootElement = v);
        builder.AddContent(10, ChildContent);

        // A field is either required or optional, so the two indicators are the branches of one decision rather than
        // two independent ones: a label asked for both renders the required one, which is the stronger statement.
        if (Required)
        {
            builder.OpenElement(11, "span");
            builder.AddAttribute(12, "style", Styles?.RequiredIndicator);
            builder.AddAttribute(13, "class", Classes?.RequiredIndicator.HasValue() is true
                                              ? $"bit-lbl-rqi {Classes!.RequiredIndicator}"
                                              : "bit-lbl-rqi");
            // The default asterisk is a decoration - a screen reader that announces it at all announces a "star" in the
            // middle of the caption, which says nothing about the field, while the "required" attribute of the control
            // is what does. An indicator written by the page is a word put there to be read, so it is left announced.
            builder.AddAttribute(14, "aria-hidden", RequiredTemplate is null && RequiredText.HasNoValue() ? "true" : null);
            if (RequiredTemplate is not null)
            {
                builder.AddContent(15, RequiredTemplate);
            }
            else
            {
                builder.AddContent(16, RequiredText ?? "*");
            }
            builder.CloseElement();
        }
        else if (Optional)
        {
            builder.OpenElement(17, "span");
            builder.AddAttribute(18, "style", Styles?.OptionalIndicator);
            builder.AddAttribute(19, "class", Classes?.OptionalIndicator.HasValue() is true
                                              ? $"bit-lbl-opi {Classes!.OptionalIndicator}"
                                              : "bit-lbl-opi");
            if (OptionalTemplate is not null)
            {
                builder.AddContent(20, OptionalTemplate);
            }
            else
            {
                builder.AddContent(21, OptionalText ?? "(optional)");
            }
            builder.CloseElement();
        }

        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
