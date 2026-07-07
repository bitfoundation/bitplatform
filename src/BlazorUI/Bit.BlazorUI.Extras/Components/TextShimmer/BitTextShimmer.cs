using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// BitTextShimmer is an animated text shimmer in which a bright gradient band sweeps across the text,
/// ideal for AI thinking/loading states or progressive reveals.
/// The effect is implemented in pure CSS (no JavaScript interop), so it works in every Blazor render mode, including static server-side rendering.
/// </summary>
public partial class BitTextShimmer : BitComponentBase
{
    /// <summary>
    /// The resting/dim color of the text. When null, a theme-aware default color is used.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? BaseColor { get; set; }

    /// <summary>
    /// The content to shimmer, which takes precedence over the Text parameter.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The character count used to scale the shimmer band width when the content is supplied using ChildContent.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int ContentLength { get; set; } = 10;

    /// <summary>
    /// The animation duration of one full shimmer sweep in ms.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? Duration { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Keeps the shimmer animating even if the user has requested reduced motion (prefers-reduced-motion).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ForceAnimation { get; set; }

    /// <summary>
    /// The bright highlight color that sweeps across the text. When null, a theme-aware default color is used.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? GradientColor { get; set; }

    /// <summary>
    /// The shimmer band width multiplier. The effective band width (px) is Spread times the character count,
    /// so longer text gets a proportionally wider shine.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public double Spread { get; set; } = 2;

    /// <summary>
    /// The text to display, that is also used to scale the shimmer band width based on its character count.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Text { get; set; }



    protected override string RootElementClass => "bit-tsh";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => ForceAnimation ? "bit-tsh-fam" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() =>
        {
            var length = ChildContent is null && Text is not null ? Text.Length : ContentLength;
            var spread = Math.Max(0, length * Spread);
            return FormattableString.Invariant($"--bit-tsh-spread:{spread}px");
        });

        StyleBuilder.Register(() => Duration.HasValue ? $"--bit-tsh-duration:{Duration.Value}ms" : string.Empty);
        StyleBuilder.Register(() => BaseColor.HasValue() ? $"--bit-tsh-base-clr:{BaseColor}" : string.Empty);
        StyleBuilder.Register(() => GradientColor.HasValue() ? $"--bit-tsh-gradient-clr:{GradientColor}" : string.Empty);
    }



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element.HasValue() ? Element! : "p");
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "style", StyleBuilder.Value);
        builder.AddAttribute(4, "class", ClassBuilder.Value);
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLower());
        builder.AddAttribute(6, "aria-label", AriaLabel);
        builder.AddElementReferenceCapture(7, v => RootElement = v);
        if (ChildContent is not null)
        {
            builder.AddContent(8, ChildContent);
        }
        else
        {
            builder.AddContent(8, Text);
        }
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
