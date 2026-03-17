using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Tag component provides a visual representation of an attribute, person, or asset.
/// </summary>
public partial class BitTag : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the tag component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple tag components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitTagParams.ParamName)]
    public BitTagParams? CascadingParameters { get; set; }



    /// <summary>
    /// Child content of component, the content that the tag will apply to.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the tag.
    /// </summary>
    [Parameter] public BitTagClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the dismiss button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// Defaults to the built-in Cancel icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom dismiss icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="DismissIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the dismiss button from the built-in Fluent UI icons.
    /// Defaults to <c>Cancel</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.ChromeClose</c>).
    /// <br />
    /// For external icon libraries, use <see cref="DismissIcon"/> instead.
    /// </remarks>
    [Parameter] public string? DismissIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.AddFriend</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery: 
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// The value is case-sensitive and must match a valid icon identifier. 
    /// If not set or set to <c>null</c>, no icon will be rendered.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Click event handler of the tag.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Dismiss button click event, if set the dismiss icon will show up.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// Reverses the direction flow of the content of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// The size of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tag.
    /// </summary>
    [Parameter] public BitTagClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the tag.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The visual variant of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-tag";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-tag-pri",
            BitColor.Secondary => "bit-tag-sec",
            BitColor.Tertiary => "bit-tag-ter",
            BitColor.Info => "bit-tag-inf",
            BitColor.Success => "bit-tag-suc",
            BitColor.Warning => "bit-tag-wrn",
            BitColor.SevereWarning => "bit-tag-swr",
            BitColor.Error => "bit-tag-err",
            BitColor.PrimaryBackground => "bit-tag-pbg",
            BitColor.SecondaryBackground => "bit-tag-sbg",
            BitColor.TertiaryBackground => "bit-tag-tbg",
            BitColor.PrimaryForeground => "bit-tag-pfg",
            BitColor.SecondaryForeground => "bit-tag-sfg",
            BitColor.TertiaryForeground => "bit-tag-tfg",
            BitColor.PrimaryBorder => "bit-tag-pbr",
            BitColor.SecondaryBorder => "bit-tag-sbr",
            BitColor.TertiaryBorder => "bit-tag-tbr",
            _ => "bit-tag-pri"
        });

        ClassBuilder.Register(() => Reversed ? "bit-tag-rvs" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tag-sm",
            BitSize.Medium => "bit-tag-md",
            BitSize.Large => "bit-tag-lg",
            _ => "bit-tag-md"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-tag-fil",
            BitVariant.Outline => "bit-tag-otl",
            BitVariant.Text => "bit-tag-txt",
            _ => "bit-tag-fil"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitTagParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    private async Task HandleOnDismissClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnDismiss.InvokeAsync(e);
        }
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }
}
