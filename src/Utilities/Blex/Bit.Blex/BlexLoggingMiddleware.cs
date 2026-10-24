using Microsoft.Extensions.Logging;

namespace Bit.Blex;

/// <summary>
/// Writes actions and their failures to an <see cref="ILogger"/> (category <c>Bit.Blex</c>). Registered by
/// <see cref="BlexOptions.UseLogging"/>, which also sends the failures Blex isolates
/// (<see cref="BlexManager.OnError"/>) to the same logger.
/// </summary>
/// <remarks>
/// Only names are logged -- the qualified action name and its sequence number -- never action arguments or
/// state, which may hold what a log must not.
/// </remarks>
public sealed class BlexLoggingMiddleware : IBlexMiddleware
{
    private static readonly Action<ILogger, string, Exception?> ActionFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, "BlexActionFailed"), "Blex action {Action} failed");

    private static readonly Action<ILogger, string, string?, Exception?> IsolatedFailureMessage =
        LoggerMessage.Define<string, string?>(LogLevel.Warning, new EventId(3, "BlexIsolatedFailure"), "Blex isolated a {Source} failure ({Detail})");

    private readonly ILogger _logger;
    private readonly LogLevel _actionLevel;
    private readonly Action<ILogger, string, int, Exception?> _actionMessage;

    /// <summary>Creates the middleware over <paramref name="logger"/>.</summary>
    /// <param name="logger">Where to write.</param>
    /// <param name="actionLevel">The level each action is logged at; <see cref="LogLevel.None"/> logs failures only.</param>
    public BlexLoggingMiddleware(ILogger logger, LogLevel actionLevel = LogLevel.Debug)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _actionLevel = actionLevel;
        _actionMessage = LoggerMessage.Define<string, int>(actionLevel, new EventId(1, "BlexAction"), "Blex action {Action} (#{Sequence})");
    }

    /// <inheritdoc />
    public void OnAction(BlexActionContext context)
    {
        if (_actionLevel != LogLevel.None && _logger.IsEnabled(_actionLevel))
            _actionMessage(_logger, context.QualifiedName, context.Sequence, null);
    }

    /// <inheritdoc />
    public void OnActionError(BlexActionErrorContext context)
        => ActionFailedMessage(_logger, context.QualifiedName, context.Exception);

    /// <summary>Logs a failure Blex isolated from the pipeline -- what <see cref="BlexManager.OnError"/> receives.</summary>
    public void LogIsolatedFailure(BlexError error)
        => IsolatedFailureMessage(_logger, error.Source, error.Detail, error.Exception);
}

/// <summary>The place <see cref="BlexOptions.UseLogging"/> took in the middleware order, resolved when the manager is built.</summary>
internal sealed record BlexLoggingRegistration(LogLevel ActionLevel);
