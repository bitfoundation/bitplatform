namespace Bit.Belemetry;

/// <summary>What <c>AddBitBelemetryServices</c> could tell about the host from the service collection it was called on.</summary>
/// <param name="HasWebHost">Whether ASP.NET Core hosting (<c>IWebHostEnvironment</c>) was registered, i.e. this is a server.</param>
internal sealed record BelemetryHostDetection(bool HasWebHost);
