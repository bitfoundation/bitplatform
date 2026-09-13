namespace Bit.Butil;

/// <summary>
/// How <see cref="ElementReferenceDomExtensions.GetHtml(Microsoft.AspNetCore.Components.ElementReference, GetHtmlOptions?)"/>
/// should treat shadow roots it meets while serializing.
/// </summary>
public class GetHtmlOptions
{
    /// <summary>
    /// True to serialize the contents of every shadow root that was attached with
    /// <c>serializable: true</c>. Shadow content is omitted by default, which is why the innerHTML
    /// of a component-heavy tree so often reads as empty.
    /// </summary>
    public bool? SerializableShadowRoots { get; set; }

    internal GetHtmlJsOptions ToJsObject() => new()
    {
        SerializableShadowRoots = SerializableShadowRoots
    };
}
