using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitEmbed"/> component.
/// </summary>
/// <remarks>
/// It carries what the embeds of an app agree on - their shape, their loading, the permissions and restrictions of the
/// content they frame, and the click-to-load placeholder they show. The address, the title and the name of the
/// placeholder of each one stay on the embed.
/// </remarks>
public partial class BitEmbedParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitEmbed"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitEmbed value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitEmbed";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitEmbedParams))]
    public BitEmbedParams() { }



    public string Name => ParamName;



    /// <summary>
    /// The features the embedded content may use, as the value of the allow attribute of the iframe.
    /// </summary>
    public string? Allow { get; set; }

    /// <summary>
    /// Lets the embedded content go full screen.
    /// </summary>
    public bool? AllowFullScreen { get; set; }

    /// <summary>
    /// The aspect ratio of the frame, as a CSS aspect-ratio value.
    /// </summary>
    public string? AspectRatio { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the embed.
    /// </summary>
    public BitEmbedClassStyles? Classes { get; set; }

    /// <summary>
    /// Allows for browser-level loading of the iframe (lazy or eager).
    /// </summary>
    public BitImageLoading? Loading { get; set; }

    /// <summary>
    /// The largest width of the frame, as a CSS length.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// The content shown in the frame in place of the iframe until the user activates it.
    /// </summary>
    public RenderFragment? PlaceholderTemplate { get; set; }

    /// <summary>
    /// Specifies how much of the address of the current page is sent to the site serving the embedded content.
    /// </summary>
    public BitImageReferrerPolicy? ReferrerPolicy { get; set; }

    /// <summary>
    /// Applies extra restrictions to the embedded content, as the value of the sandbox attribute of the iframe.
    /// </summary>
    /// <remarks>
    /// Only a list of tokens is handed down. An empty value - every restriction at once - is no value to the cascade,
    /// which lets go of an empty string like any other, so it is set on the embed itself.
    /// </remarks>
    public string? Sandbox { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the embed.
    /// </summary>
    public BitEmbedClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitEmbed"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitEmbed"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitEmbed"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitEmbed"/>.
    /// </remarks>
    /// <param name="bitEmbed">
    /// The <see cref="BitEmbed"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitEmbed bitEmbed)
    {
        if (bitEmbed is null) return;

        UpdateBaseParameters(bitEmbed);

        if (Allow.HasValue())
        {
            bitEmbed.TakeFromCascade(nameof(Allow), Allow, static e => e.Allow, static (e, v) => e.Allow = v);
        }

        if (AllowFullScreen.HasValue)
        {
            bitEmbed.TakeFromCascade(nameof(AllowFullScreen), AllowFullScreen.Value, static e => e.AllowFullScreen, static (e, v) => e.AllowFullScreen = v);
        }

        if (AspectRatio.HasValue())
        {
            bitEmbed.TakeFromCascade(nameof(AspectRatio), AspectRatio, static e => e.AspectRatio, static (e, v) => e.AspectRatio = v);
        }

        if (Classes is not null)
        {
            bitEmbed.TakeFromCascade(nameof(Classes), Classes, static e => e.Classes, static (e, v) => e.Classes = v);
        }

        if (Loading.HasValue)
        {
            bitEmbed.TakeFromCascade(nameof(Loading), Loading.Value, static e => e.Loading, static (e, v) => e.Loading = v);
        }

        if (MaxWidth.HasValue())
        {
            bitEmbed.TakeFromCascade(nameof(MaxWidth), MaxWidth, static e => e.MaxWidth, static (e, v) => e.MaxWidth = v);
        }

        if (PlaceholderTemplate is not null)
        {
            bitEmbed.TakeFromCascade(nameof(PlaceholderTemplate), PlaceholderTemplate, static e => e.PlaceholderTemplate, static (e, v) => e.PlaceholderTemplate = v);
        }

        if (ReferrerPolicy.HasValue)
        {
            bitEmbed.TakeFromCascade(nameof(ReferrerPolicy), ReferrerPolicy.Value, static e => e.ReferrerPolicy, static (e, v) => e.ReferrerPolicy = v);
        }

        if (Sandbox.HasValue())
        {
            bitEmbed.TakeFromCascade(nameof(Sandbox), Sandbox, static e => e.Sandbox, static (e, v) => e.Sandbox = v);
        }

        if (Styles is not null)
        {
            bitEmbed.TakeFromCascade(nameof(Styles), Styles, static e => e.Styles, static (e, v) => e.Styles = v);
        }
    }
}
