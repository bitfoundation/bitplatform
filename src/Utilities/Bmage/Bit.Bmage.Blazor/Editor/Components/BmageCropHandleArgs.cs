namespace Bit.Bmage;

/// <summary>A drag starting on a crop handle.</summary>
public readonly record struct BmageCropHandleArgs(BmageCropHandle Handle, BmageCanvasPointerArgs Pointer);
