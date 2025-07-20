namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

/// <summary>
/// Represents a delegate that creates a chain of HTTP message handlers.
///
/// This delegate optionally takes underlying transportHandler (Useful for SignalR, integration tests using test server etc),
/// and returns a chain of handlers that will process HTTP messages sequentially.
/// The returned chain will include the following handlers, in order:
///
/// 1. <see cref="LoggingDelegatingHandler"/>
/// 2. <see cref="CacheDelegatingHandler"/>
/// 3. <see cref="RequestHeadersDelegatingHandler" />
/// 4. <see cref="AuthDelegatingHandler" />
/// 5. <see cref="RetryDelegatingHandler" />
/// 6. <see cref="ExceptionDelegatingHandler" />
/// 
/// </summary>
/// <param name="transportHandler">The underlying HTTP message handler to use for network communication.</param>
/// <returns>The constructed chain of HTTP message handlers.</returns>
public delegate HttpMessageHandler HttpMessageHandlersChainFactory(HttpMessageHandler? transportHandler = null);
