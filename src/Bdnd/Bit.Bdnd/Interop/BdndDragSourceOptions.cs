namespace Bit.Bdnd;

/// <summary>Options of a drag source registration in <c>bit-bdnd.js</c>.</summary>
/// <param name="Id">The draggable's handler id (also part of the drag's DataTransfer type).</param>
/// <param name="CanDrag">Whether a drag may start.</param>
/// <param name="UseHandle">Whether a drag must start on a <see cref="BdndDragHandle"/>.</param>
/// <param name="UseEmptyPreview">Hide the native drag image (custom drag layers).</param>
/// <param name="EffectAllowed">The value assigned to <c>dataTransfer.effectAllowed</c>.</param>
internal sealed record BdndDragSourceOptions(
    string Id, bool CanDrag, bool UseHandle, bool UseEmptyPreview, string EffectAllowed);
