using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bit.Bink;

/// <summary>
/// How embedded images are shared while elements are written or read: an image's data is written once and referred
/// to by its content id everywhere else. A document shares images through its asset table; an operation through the
/// images every participant already holds.
/// </summary>
internal sealed class BinkImageAssets
{
    /// <summary>The longest content id accepted from input.</summary>
    public const int MaxIdLength = 64;

    // Ids are cached by the URI string itself, which every version of an element showing the image shares, so a
    // large picture is hashed once, not on every save or every frame of a drag.
    private static readonly ConditionalWeakTable<string, string> s_ids = new();

    /// <summary>Writing: whether the image with this content id may be written as a reference instead of its data.</summary>
    public Func<string, bool>? CanRefer { get; init; }

    /// <summary>Writing: told each image written in full, with its content id.</summary>
    public Action<string, BinkImageSource>? Written { get; init; }

    /// <summary>Reading: the image a reference names, or null when it is not available.</summary>
    public Func<string, BinkImageSource?>? Resolve { get; init; }

    /// <summary>Reading: told each embedded image read in full, so later references to it can be resolved.</summary>
    public Action<BinkImageSource>? Read { get; init; }

    // Elements state the picture's natural size themselves; one variant per size keeps them sharing an instance.
    private Dictionary<(string Id, int Width, int Height), BinkImageSource>? _sized;

    /// <summary>
    /// The shared image a reference resolved to, at the natural size the referring element states: the same instance
    /// for every element that states the same size.
    /// </summary>
    public BinkImageSource Sized(string id, BinkImageSource shared, int width, int height)
    {
        if (shared.Width == width && shared.Height == height) return shared;
        _sized ??= new Dictionary<(string, int, int), BinkImageSource>();
        ref BinkImageSource? sized = ref CollectionsMarshal.GetValueRefOrAddDefault(_sized, (id, width, height), out _);
        // A different picture under the same id would be a different reference; the id names the content, so that
        // only happens with hostile input, and then the variant follows what was resolved.
        if (sized is null || !ReferenceEquals(sized.Uri, shared.Uri)) sized = shared.WithSize(width, height);
        return sized;
    }

    /// <summary>
    /// Reading: whether a reference that cannot be resolved fails the read with <see cref="BinkMissingAssetException"/>,
    /// so the caller can fetch the image, instead of dropping the element as a malformed one.
    /// </summary>
    public bool ThrowWhenMissing { get; init; }

    /// <summary>
    /// The content id of an image: the first 16 bytes of the SHA-256 of its URI, in hexadecimal. The same picture
    /// has the same id on every participant and in every file, whoever embedded it.
    /// </summary>
    public static string IdOf(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return s_ids.GetValue(uri, static value =>
        {
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(MemoryMarshal.AsBytes(value.AsSpan()), hash);
            return Convert.ToHexString(hash[..16]).ToLowerInvariant();
        });
    }
}

/// <summary>
/// An operation referred to an image by its content id, and the reader does not hold that image. The operation is not
/// malformed, so this is not a <see cref="BinkFormatException"/>: the image went over the wire earlier, before this
/// participant could keep it, and the session fetches it rather than rejecting the change.
/// </summary>
internal sealed class BinkMissingAssetException(string id) : Exception($"The image {id} the change refers to is not available here.")
{
    /// <summary>The content id of the missing image.</summary>
    public string AssetId { get; } = id;
}
