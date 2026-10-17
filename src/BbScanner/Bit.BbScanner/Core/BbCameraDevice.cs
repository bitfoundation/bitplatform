namespace Bit.BbScanner;

/// <summary>A camera the browser is willing to open.</summary>
/// <param name="DeviceId">Opaque identifier to pass to <see cref="BbScanner.StartAsync"/>.</param>
/// <param name="Label">
/// Human readable name. Browsers hide it until the user has granted camera permission at least
/// once, so it may be a generic placeholder on the first run.
/// </param>
public sealed record BbCameraDevice(string DeviceId, string Label);
