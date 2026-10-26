using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiAttachmentChip shows one attachment as a chip: a thumbnail for an image and a glyph of its kind otherwise, its
/// name, and its size - or the progress of an upload still arriving, or why it failed.
/// </summary>
/// <remarks>
/// The chip is a <see cref="BitTag"/>, whose dismiss button removes the attachment when <see cref="OnRemove"/> is set.
/// </remarks>
public partial class BitAiAttachmentChip : BitAiComponentBase
{
    private EventCallback<MouseEventArgs> _onDismiss;
    private BitTag? _tag;



    /// <summary>
    /// Gets or sets the cascading parameters for the attachment chip component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple attachment chip components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiAttachmentChipParams.ParamName)]
    public BitAiAttachmentChipParams? CascadingParameters { get; set; }



    /// <summary>
    /// The attachment to render.
    /// </summary>
    [Parameter, EditorRequired] public BitAiAttachment Attachment { get; set; } = default!;

    /// <summary>
    /// Custom CSS classes for different parts of the attachment chip.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiAttachmentChipClassStyles? Classes { get; set; }

    /// <summary>
    /// Raised when the user removes the attachment. The remove button is hidden while it is not set.
    /// </summary>
    [Parameter] public EventCallback<BitAiAttachment> OnRemove { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the attachment chip.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiAttachmentChipClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aiach";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiAttachmentChipParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // Created only while something listens, since the tag draws its dismiss button for any callback it is handed.
        _onDismiss = OnRemove.HasDelegate
                     ? EventCallback.Factory.Create<MouseEventArgs>(this, HandleOnRemove)
                     : default;

        base.OnParametersSet();
    }



    /// <summary>
    /// Gives the keyboard focus to the remove button of the chip.
    /// </summary>
    /// <remarks>
    /// This is what a row of chips moves the focus on with after removing one of them: the remove button that was pressed
    /// goes with its chip, and a focus left on it falls back to the page. A chip with no <see cref="OnRemove"/> has no
    /// button, and nothing to focus.
    /// </remarks>
    public ValueTask FocusAsync() => _tag is null || OnRemove.HasDelegate is false ? ValueTask.CompletedTask : _tag.FocusAsync();



    private bool IsFailed => Attachment.Status is BitAiAttachmentStatus.Failed;

    // Only an image is drawn from its own content, and a thumbnail is preferred over the full picture. The address comes
    // from the transport as much as from the user (a file part of a reply), so it is read as every image source is
    // (BitAiSafeUrls.Image) and shown only where the image policy shows it outright: a chip is too small for a
    // placeholder, so one the policy holds back or hides shows the glyph of its kind instead.
    private string? Preview
    {
        get
        {
            if (Attachment.Kind is not BitAiAttachmentKind.Image) return null;
            if ((Attachment.ThumbnailUrl ?? Attachment.Url) is not { } url) return null;

            var source = BitAiSafeUrls.Image(url);

            return source != BitAiSafeUrls.BlankImage && DecideImageSource(source) == BitAiImageDecision.Show ? source : null;
        }
    }

    // The glyphs come from the icon font of this package, since the one of the core carries only the few its own
    // components draw.
    private string IconClass => Attachment.Kind switch
    {
        BitAiAttachmentKind.Image => "bit-icon-ai bit-icon-ai--Photo2",
        BitAiAttachmentKind.Audio => "bit-icon-ai bit-icon-ai--Microphone",
        BitAiAttachmentKind.Video => "bit-icon-ai bit-icon-ai--Video",
        _ => "bit-icon-ai bit-icon-ai--Page",
    };

    private string? Details => Attachment.Status switch
    {
        BitAiAttachmentStatus.Uploading => $"{(Attachment.Progress ?? 0) * 100:0}%",
        BitAiAttachmentStatus.Failed => Attachment.Error ?? Strings.UploadFailed,
        _ => Attachment.Size is { } size ? Strings.FormatBytes(size) : null,
    };

    private async Task HandleOnRemove()
    {
        if (Disabled) return;

        await OnRemove.InvokeAsync(Attachment);
    }
}
