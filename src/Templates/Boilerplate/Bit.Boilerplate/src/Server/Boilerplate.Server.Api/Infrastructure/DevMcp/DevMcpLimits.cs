namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

internal static class DevMcpLimits
{
    public const int CommandTimeoutSeconds = 15;
    public const int MaxTake = 100;
    public const int DefaultTake = 25;
    public const int MaxPayloadBytes = 256 * 1024;
    public const int HangfireMaxTake = 50;
    public const int HangfireFilterScanCap = 500;
}
