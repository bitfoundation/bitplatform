namespace Bit.Butil;

/// <summary>
/// Which of the several reasons an element can be invisible
/// <see cref="ElementReferenceExtensions.CheckVisibility(Microsoft.AspNetCore.Components.ElementReference, CheckVisibilityOptions?)"/>
/// should take into account. Everything left null uses the browser's default, which is to consider
/// only whether the element is rendered at all.
/// </summary>
public class CheckVisibilityOptions
{
    /// <summary>True to report an element inside a <c>content-visibility: auto</c> subtree that is currently skipped as invisible.</summary>
    public bool? ContentVisibilityAuto { get; set; }

    /// <summary>True to report an element with <c>opacity: 0</c> - on itself or on an ancestor - as invisible.</summary>
    public bool? OpacityProperty { get; set; }

    /// <summary>True to report an element hidden by <c>visibility: hidden</c> or <c>collapse</c> as invisible.</summary>
    public bool? VisibilityProperty { get; set; }

    /// <summary>The earlier spelling of <see cref="OpacityProperty"/>, still accepted by shipped engines.</summary>
    public bool? CheckOpacity { get; set; }

    /// <summary>The earlier spelling of <see cref="VisibilityProperty"/>, still accepted by shipped engines.</summary>
    public bool? CheckVisibilityCSS { get; set; }

    internal CheckVisibilityJsOptions ToJsObject() => new()
    {
        ContentVisibilityAuto = ContentVisibilityAuto,
        OpacityProperty = OpacityProperty,
        VisibilityProperty = VisibilityProperty,
        CheckOpacity = CheckOpacity,
        CheckVisibilityCSS = CheckVisibilityCSS
    };
}
