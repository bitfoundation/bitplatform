namespace Bit.BlazorUI;

/// <summary>
/// The loading state of the image of a <see cref="BitImage"/>.
/// </summary>
/// <remarks>
/// The state starts at <see cref="Loading"/> and moves to <see cref="Loaded"/> or <see cref="Error"/>
/// as the browser reports back. Changing the source returns it to <see cref="Loading"/>, and so does
/// falling back to the FallbackSrc after an error, since that is another image being fetched.
/// </remarks>
public enum BitImageState
{
    /// <summary>
    /// The image is loading from its source.
    /// </summary>
    Loading,

    /// <summary>
    /// The image has been loaded successfully.
    /// </summary>
    Loaded,

    /// <summary>
    /// An error has been encountered while loading the image. Where a FallbackSrc is provided, this
    /// state is only reached once that one has failed as well.
    /// </summary>
    Error
}
