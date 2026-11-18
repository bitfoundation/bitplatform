using Microsoft.Extensions.Logging;

namespace Bit.Blex;

/// <summary>The place <see cref="BlexOptions.UseLogging"/> took in the middleware order, resolved when the manager is built.</summary>
internal sealed record BlexLoggingRegistration(LogLevel ActionLevel);
