using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bit.BlazorUI;

/// <summary>
/// The settings of a <see cref="BitAiSignalRTransport"/>.
/// </summary>
public sealed record BitAiSignalRTransportOptions
{
    /// <summary>
    /// The name of <c>BitAiHub</c>'s streaming method. Written out rather than read off the hub, which is in
    /// Bit.BlazorUI.AI.Server, a package no client references; the tests pin the two to each other.
    /// </summary>
    public const string DefaultStreamMethodName = "StreamBitAiEvents";

    /// <summary>The name of <c>BitAiHub</c>'s method taking a decision on a tool call (see <see cref="DefaultStreamMethodName"/>).</summary>
    public const string DefaultApprovalMethodName = "SendBitAiApproval";

    /// <summary>The name of the streaming hub method. Defaults to <see cref="DefaultStreamMethodName"/>, <c>BitAiHub.StreamMethodName</c>.</summary>
    public string StreamMethodName { get; init; } = DefaultStreamMethodName;

    /// <summary>The name of the hub method taking a decision on a tool call. Defaults to <see cref="DefaultApprovalMethodName"/>, <c>BitAiHub.ApprovalMethodName</c>.</summary>
    public string ApprovalMethodName { get; init; } = DefaultApprovalMethodName;

    /// <summary>Serializer hooks, for custom parts, and the JSON settings, for the app's own types.</summary>
    public BitAiConversationSerializerOptions? Serializer { get; init; }

    /// <summary>
    /// Whether a request starts the connection when it is disconnected. On by default; turn it off for a connection the
    /// app starts and reconnects itself. Either way, a request waits for a connection that is starting or reconnecting
    /// (until its cancellation) rather than failing on it.
    /// </summary>
    public bool StartConnection { get; init; } = true;
}

/// <summary>
/// A transport that streams each reply from a hub method over a SignalR connection, in the package's wire format
/// (<see cref="BitAiWireSerializer"/>).
/// </summary>
/// <remarks>
/// <para>
/// The server end is a <c>BitAiHub</c> (Bit.BlazorUI.AI.Server). The connection is the app's: build it as usual (the JSON hub protocol,
/// the default) and register the transport with
/// <see cref="BitAiSignalRBuilderExtensions.UseSignalRTransport"/>.
/// One connection can carry the app's other hubs' traffic too, and the replies of several sessions at once.
/// </para>
/// <para>
/// Stopping a reply cancels its hub stream, which cancels the token the hub's reply runs with. A
/// decision on a tool call the server holds goes to the hub over the same connection. An event of a type this version
/// of the package does not know is skipped.
/// </para>
/// </remarks>
public sealed class BitAiSignalRTransport : IBitAiTransport, IBitAiApprovalTransport
{
    private static readonly TimeSpan SettlePollInterval = TimeSpan.FromMilliseconds(100);
    private static readonly ConditionalWeakTable<HubConnection, SemaphoreSlim> StartGates = new();

    private readonly HubConnection _connection;
    private readonly BitAiSignalRTransportOptions _options;

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

    // Waits out a connection that is starting or reconnecting (a hub call fails on one that is not connected), and starts
    // one that is disconnected when the options say so. A disconnected one left to the app is left as it is, and the call
    // then fails with the connection's own error.
    private async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            switch (_connection.State)
            {
                case HubConnectionState.Connected:
                    return;

                case HubConnectionState.Connecting or HubConnectionState.Reconnecting:
                    // HubConnection raises no event when a start it did not run here completes, so the state is polled.
                    await Task.Delay(SettlePollInterval, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    if (!_options.StartConnection) return;
                    await StartAsync(cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }

    private async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        // One gate per connection rather than per transport: the transport is scoped, and the scopes of several
        // sessions (or circuits) can share one connection.
        var starting = StartGates.GetValue(_connection, static _ => new SemaphoreSlim(1, 1));

        await starting.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection.State is HubConnectionState.Disconnected) await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException) when (_connection.State is not HubConnectionState.Disconnected)
        {
            // The app's own code started it in the meantime; the loop waits for it to connect.
        }
        finally
        {
            starting.Release();
        }
    }
}
