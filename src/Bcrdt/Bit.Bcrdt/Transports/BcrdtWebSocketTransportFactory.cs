using System.Net.WebSockets;

namespace Bit.Bcrdt;

/// <summary>
/// Creates a <see cref="BcrdtWebSocketTransport"/> per document against one endpoint.
/// </summary>
/// <remarks>
/// <para>
/// One socket per document, rather than one shared socket multiplexing all of them. It costs more
/// sockets, and buys a failure domain per document - a document that is refused or is backing off
/// does not stall the others - and it keeps the protocol free of a document identifier in every
/// message, because a socket is only ever about one document.
/// </para>
/// <para>
/// Registered as the application's <see cref="IBcrdtTransportFactory"/>, this is the only place that
/// knows the collaboration stack is speaking WebSockets at all.
/// </para>
/// </remarks>
public sealed class BcrdtWebSocketTransportFactory : IBcrdtTransportFactory
{
    /// <summary>The token replaced with the document identifier, when the template contains it.</summary>
    public const string DocumentIdPlaceholder = "{documentId}";

    /// <summary>
    /// The query parameter the document identifier is appended as when the template has no
    /// <see cref="DocumentIdPlaceholder"/>. The server endpoint reads the same name.
    /// </summary>
    public const string DocumentIdQueryKey = "documentId";

    private readonly string _template;
    private readonly Action<ClientWebSocketOptions>? _configure;

    /// <summary>Creates transports against an endpoint template.</summary>
    /// <param name="endpoint">
    /// The endpoint, which may contain <see cref="DocumentIdPlaceholder"/>. See
    /// <see cref="BuildUri(string, string)"/> for how the document identifier is applied.
    /// </param>
    /// <param name="configure">Applied to each socket's options, for authentication headers or subprotocols.</param>
    public BcrdtWebSocketTransportFactory(string endpoint, Action<ClientWebSocketOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        _template = endpoint;
        _configure = configure;
    }

    /// <summary>Creates transports against an endpoint template.</summary>
    /// <remarks>
    /// The template is taken from <see cref="Uri.OriginalString"/> rather than the parsed form, so
    /// that a <see cref="DocumentIdPlaceholder"/> in the path survives - the parsed form would have
    /// percent-escaped its braces and the substitution would silently never match.
    /// </remarks>
    /// <param name="endpoint">The endpoint, which may contain <see cref="DocumentIdPlaceholder"/>.</param>
    /// <param name="configure">Applied to each socket's options, for authentication headers or subprotocols.</param>
    public BcrdtWebSocketTransportFactory(Uri endpoint, Action<ClientWebSocketOptions>? configure = null)
        : this((endpoint ?? throw new ArgumentNullException(nameof(endpoint))).OriginalString, configure)
    {
    }

    /// <summary>Applied to every transport created. See <see cref="BcrdtWebSocketTransport.ConnectTimeout"/>.</summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Applied to every transport created. See <see cref="BcrdtWebSocketTransport.MaxMessageSize"/>.</summary>
    public int MaxMessageSize { get; init; } = BcrdtWebSocketFraming.DefaultMaxMessageSize;

    /// <inheritdoc/>
    public IBcrdtTransport Create(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        return new BcrdtWebSocketTransport(BuildUri(_template, documentId), _configure)
        {
            ConnectTimeout = ConnectTimeout,
            MaxMessageSize = MaxMessageSize,
        };
    }

    /// <summary>
    /// Applies a document identifier to an endpoint template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Substitutes <see cref="DocumentIdPlaceholder"/> where the template contains it, and appends a
    /// <see cref="DocumentIdQueryKey"/> query parameter otherwise. Those are the two shapes the
    /// server endpoint in <c>Bit.Bcrdt.Server</c> reads, so a client
    /// and a server configured from the same address agree without either being told which form is
    /// in use.
    /// </para>
    /// <para>
    /// A fragment is dropped: it is meaningless in a WebSocket address (RFC 6455 forbids one), and a
    /// query parameter appended after it would be part of the fragment and never reach the server.
    /// </para>
    /// <para>
    /// An <c>http</c> or <c>https</c> address is rewritten to <c>ws</c> or <c>wss</c>.
    /// <see cref="ClientWebSocket"/> rejects the HTTP schemes outright, and being handed the address
    /// the endpoint is served on is the overwhelmingly common mistake - accepting it costs nothing
    /// and turns a runtime argument exception into a working connection.
    /// </para>
    /// </remarks>
    /// <param name="template">The endpoint template.</param>
    /// <param name="documentId">The document identifier to apply, escaped as needed.</param>
    /// <returns>The address a transport for that document should connect to.</returns>
    public static Uri BuildUri(string template, string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(template);
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        string escaped = Uri.EscapeDataString(documentId);

        int fragment = template.IndexOf('#', StringComparison.Ordinal);
        string head = fragment >= 0 ? template[..fragment] : template;

        string address;
        if (head.Contains(DocumentIdPlaceholder, StringComparison.Ordinal))
        {
            address = head.Replace(DocumentIdPlaceholder, escaped, StringComparison.Ordinal);
        }
        else
        {
            string separator = !head.Contains('?', StringComparison.Ordinal) ? "?"
                : head.EndsWith('?') || head.EndsWith('&') ? string.Empty
                : "&";
            address = $"{head}{separator}{DocumentIdQueryKey}={escaped}";
        }

        Uri uri = new(address, UriKind.Absolute);

        return uri.Scheme switch
        {
            "http" => new UriBuilder(uri) { Scheme = "ws" }.Uri,
            "https" => new UriBuilder(uri) { Scheme = "wss" }.Uri,
            _ => uri,
        };
    }
}
