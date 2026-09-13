namespace Bit.Butil;

/// <summary>
/// An element entering or leaving picture-in-picture.
/// </summary>
/// <param name="Active">True when the element just entered the floating window, false when it left.</param>
/// <param name="Width">
/// The floating window's width in pixels while <paramref name="Active"/>, otherwise 0. Only ever
/// reported at the moment of entering - the platform does not expose it afterwards.
/// </param>
/// <param name="Height">The floating window's height in pixels while <paramref name="Active"/>, otherwise 0.</param>
public record PictureInPictureChange(bool Active, int Width, int Height);
