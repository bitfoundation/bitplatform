using Bit.Cli.Infrastructure;

namespace Bit.Cli.Telemetry;

public enum TelemetryLevel
{
    Off,
    Errors,
    All
}

public sealed record TelemetryDecision(TelemetryLevel Level, bool LogOnly, string Reason, bool CanSend)
{
    public bool IsActive => Level is not TelemetryLevel.Off && (CanSend || LogOnly);

    public bool IsDefault => Reason == DefaultReason;

    public const string DefaultReason = "the default";

    public static TelemetryLevel? ParseLevel(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "off" or "false" or "0" or "none" or "no" => TelemetryLevel.Off,
        "errors" or "error" or "on" or "true" or "1" => TelemetryLevel.Errors,
        "all" or "usage" => TelemetryLevel.All,
        _ => null
    };

    public static string Name(TelemetryLevel level) => level switch
    {
        TelemetryLevel.Off => "off",
        TelemetryLevel.Errors => "errors",
        _ => "all"
    };

    public static TelemetryDecision Resolve(CliEnvironment environment, CliSettings settings, string? connectionString)
    {
        var canSend = string.IsNullOrWhiteSpace(connectionString) is false;

        if (environment.IsVariableTrue("DO_NOT_TRACK"))
            return new(TelemetryLevel.Off, false, "DO_NOT_TRACK", canSend);

        if (environment.IsVariableTrue("DOTNET_CLI_TELEMETRY_OPTOUT"))
            return new(TelemetryLevel.Off, false, "DOTNET_CLI_TELEMETRY_OPTOUT", canSend);

        if (environment.GetVariable("BIT_CLI_TELEMETRY") is { } variable)
        {
            if (variable.Trim().Equals("log", StringComparison.OrdinalIgnoreCase))
                return new(TelemetryLevel.All, true, "BIT_CLI_TELEMETRY=log", canSend);

            if (ParseLevel(variable) is { } fromVariable)
                return new(fromVariable, false, "BIT_CLI_TELEMETRY", canSend);
        }

        if (ParseLevel(settings.Telemetry) is { } fromSettings)
            return new(fromSettings, false, "bit telemetry", canSend);

        return new(TelemetryLevel.Errors, false, DefaultReason, canSend);
    }
}
