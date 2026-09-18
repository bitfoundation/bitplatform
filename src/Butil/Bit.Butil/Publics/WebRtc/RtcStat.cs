using System.Collections.Generic;

namespace Bit.Butil;

/// <summary>One entry from <see cref="PeerConnectionHandle.GetStats"/>.</summary>
/// <param name="Id">The entry's id, which other entries reference to point at it.</param>
/// <param name="Type">
/// What it describes: <c>"candidate-pair"</c>, <c>"inbound-rtp"</c>, <c>"outbound-rtp"</c>,
/// <c>"transport"</c>, <c>"codec"</c> and a dozen more.
/// </param>
/// <param name="Values">
/// The entry's members, flattened to strings - they differ per <paramref name="Type"/>, so there is
/// no honest record shape for them. The ones usually wanted: <c>currentRoundTripTime</c> and
/// <c>nominated</c> on a candidate pair; <c>bytesReceived</c>, <c>packetsLost</c> and
/// <c>jitter</c> on an inbound stream.
/// </param>
public record RtcStat(string Id, string Type, Dictionary<string, string> Values);
