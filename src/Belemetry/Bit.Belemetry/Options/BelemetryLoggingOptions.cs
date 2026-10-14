using Microsoft.Extensions.Logging;

namespace Bit.Belemetry;

/// <summary><c>ILogger</c> collection.</summary>
public sealed class BelemetryLoggingOptions
{
    /// <summary>
    /// The minimum level collected. Defaults to <see cref="LogLevel.Information"/>. Per-category levels can also be set
    /// through the standard logging configuration under the <c>Belemetry</c> provider alias
    /// (<c>"Logging": { "Belemetry": { "LogLevel": { ... } } }</c>).
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>Whether the values of active logging scopes become attributes. Defaults to <see langword="true"/>.</summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>Whether the message template of structured entries is kept as <c>log.record.template</c>. Defaults to <see langword="false"/>.</summary>
    public bool IncludeMessageTemplate { get; set; }

    /// <summary>Category prefixes never collected (Belemetry's own categories are always excluded).</summary>
    public List<string> ExcludedCategories { get; } = [];
}
