namespace Bit.Bcrdt;

/// <summary>
/// The factory of the connections to the other tabs, registered apart from the network's own
/// <see cref="IBcrdtTransportFactory"/>.
/// </summary>
internal sealed record BcrdtCrossTabTransports(IBcrdtTransportFactory Factory);
