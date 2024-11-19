namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

/// <summary>
/// Represents a delegate that creates a chain of HTTP message handlers.
///
/// This delegate takes an underlying HTTP message handler, typically an <see cref="HttpClientHandler"/>,
/// and returns a chain of handlers that will process HTTP messages sequentially.
/// The returned chain will include the following handlers, in order:
///
/// 1. <see cref="LoggingDelegatingHandler"/>
/// 2. <see cref="RequestHeadersDelegatingHandler" />
/// 3. <see cref="AuthDelegatingHandler" />
/// 4. <see cref="RetryDelegatingHandler" />
/// 5. <see cref="ExceptionDelegatingHandler" />
///
/// The chain is constructed in reverse order, with the provided `transportHandler` as the final
/// link. Each subsequent handler in the chain receives the output of the previous one.
/// </summary>
/// <param name="transportHandler">The underlying HTTP message handler to use for network communication.</param>
/// <returns>The constructed chain of HTTP message handlers.</returns>
public delegate HttpMessageHandler HttpMessageHandlersChainFactory(HttpMessageHandler transportHandler);
