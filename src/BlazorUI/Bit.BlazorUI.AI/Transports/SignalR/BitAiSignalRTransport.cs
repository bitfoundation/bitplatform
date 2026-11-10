using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bit.BlazorUI;

/// <summary>
/// The settings of a <see cref="BitAiSignalRTransport"/>.
/// </summary>
public sealed record BitAiSignalRTransportOptions
{
    /// <summary>The name of the streaming hub method. Defaults to <see cref="BitAiHub.StreamMethodName"/>.</summary>
    public string StreamMethodName { get; init; } = BitAiHub.StreamMethodName;

    /// <summary>The name of the hub method taking a decision on a tool call. Defaults to <see cref="BitAiHub.ApprovalMethodName"/>.</summary>
    public string ApprovalMethodName { get; init; } = BitAiHub.ApprovalMethodName;

    /// <summary>Serializer hooks, for custom parts, and the JSON settings, for the app's own types.</summary>
    public BitAiConversationSerializerOptions? Serializer { get; init; }

    /// <summary>
    /// Whether a request starts the connection when it is not connected. On by default; turn it off for a connection the
    /// app starts and reconnects itself.
    /// </summary>
    public bool StartConnection { get; init; } = true;
}

/// <summary>
/// A transport that streams each reply from a hub method over a SignalR connection, in the package's wire format
/// (<see cref="BitAiWireSerializer"/>).
/// </summary>
/// <remarks>
/// <para>
/// The server end is a <see cref="BitAiHub"/>. The connection is the app's: build it as usual (the JSON hub protocol,
/// the default) and register the transport with
/// <see cref="BitAiBuilder.UseSignalRTransport(Func{IServiceProvider, HubConnection}, BitAiSignalRTransportOptions?)"/>.
/// One connection can carry the app's other hubs' traffic too, and the replies of several sessions at once.
/// </para>
/// <para>
/// Stopping a reply cancels its hub stream, which cancels the token the hub's <see cref="BitAiHub"/> reply runs with. A
/// decision on a tool call the server holds goes to the hub over the same connection. An event of a type this version
/// of the package does not know is skipped.
/// </para>
/// </remarks>
public sealed class BitAiSignalRTransport : IBitAiTransport, IBitAiApprovalTransport
{
    private readonly HubConnection _connection;
    private readonly BitAiSignalRTransportOptions _options;
    private readonly SemaphoreSlim _starting = new(1, 1);

    /// <summary>Creates the transport.</summary>
    /// <param name="connection">The connection to the hub.</param>
    /// <param name="options">The hub method names and the serializer settings.</param>
    public BitAiSignalRTransport(HubConnection connection, BitAiSignalRTransportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _options = options ?? new BitAiSignalRTransportOptions();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await BitAiWireSerializer.ToSnapshotAsync(request, _options.Serializer, cancellationToken).ConfigureAwait(false);
        var json = BitAiWireSerializer.SerializeRequestToElement(snapshot, _options.Serializer);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var item in _connection.StreamAsync<JsonElement>(_options.StreamMethodName, json, cancellationToken).ConfigureAwait(false))
        {
            if (BitAiWireSerializer.Deserialize(item, _options.Serializer) is { } evt) yield return evt;
        }
    }

    /// <inheritdoc />
    public async ValueTask SendApprovalAsync(BitAiApprovalResponse response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        await _connection.InvokeAsync<bool>(
            _options.ApprovalMethodName,
            BitAiWireSerializer.SerializeApprovalToElement(response, _options.Serializer),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (!_options.StartConnection || _connection.State is not HubConnectionState.Disconnected) return;

        await _starting.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection.State is HubConnectionState.Disconnected) await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _starting.Release();
        }
    }
}
