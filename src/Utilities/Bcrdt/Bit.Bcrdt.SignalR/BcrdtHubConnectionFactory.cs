using Microsoft.AspNetCore.SignalR.Client;

namespace Bit.Bcrdt;

/// <summary>Builds the hub connection a <see cref="BcrdtSignalRTransport"/> will run on.</summary>
/// <remarks>
/// A factory rather than a ready-made connection, because the transport replaces its connection on
/// every reconnect: handing it a single instance would leave it holding a closed object after the
/// first drop, with no way to obtain a fresh one.
/// </remarks>
/// <returns>A connection that has not been started.</returns>
public delegate HubConnection BcrdtHubConnectionFactory();
