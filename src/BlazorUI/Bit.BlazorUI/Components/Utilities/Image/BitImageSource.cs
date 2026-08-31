namespace Bit.BlazorUI;

/// <summary>
/// One alternative source of a <see cref="BitImage"/>, rendered as a source element of the picture the
/// image is then wrapped in.
/// </summary>
/// <remarks>
/// This is the art-direction and the format-negotiation half of responsive images, which the srcset of
/// the image itself cannot express. The browser walks the sources in order and takes the FIRST one
/// whose <see cref="Media"/> and <see cref="Type"/> it is satisfied by, so the order is the priority:
/// the narrowest condition and the newest format first, with the image's own
/// <see cref="BitImage.Src"/> as the answer that is always understood.
/// <br />
/// Two things it is for. A different crop of the same subject at a different viewport - a wide hero on
/// a desktop and a square one on a phone - which is a decision about the composition rather than about
/// the number of pixels. And a modern format offered to whoever can read it: an AVIF and a WebP source
/// ahead of the JPEG that every browser understands.
/// </remarks>
public class BitImageSource
{
    /// <summary>
    /// The set of images this source offers, with their width or density descriptors
    /// (e.g. "photo-480.avif 480w, photo-960.avif 960w"). This is the only required member.
    /// </summary>
    public string? Srcset { get; set; }

    /// <summary>
    /// The media query the source applies to (e.g. "(max-width: 600px)"). A source with none applies
    /// whatever the viewport is, so it belongs last among the sources that carry one.
    /// </summary>
    public string? Media { get; set; }

    /// <summary>
    /// How wide the image will be laid out at, for the browser to choose among a width-descriptor
    /// <see cref="Srcset"/> with (e.g. "(max-width: 600px) 100vw, 32rem").
    /// </summary>
    public string? Sizes { get; set; }

    /// <summary>
    /// The MIME type of the images this source offers (e.g. "image/avif"). A browser that cannot read
    /// the type skips the source without fetching anything, which is what makes a modern format safe
    /// to offer ahead of the one everything understands.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// The intrinsic width, in pixels, of the images this source offers. Given together with
    /// <see cref="Height"/> it is what lets the browser reserve the right room before the image
    /// arrives, per source.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// The intrinsic height, in pixels, of the images this source offers.
    /// </summary>
    public int? Height { get; set; }
}
