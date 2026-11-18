namespace Bit.BevTools;

/// <summary>
/// Where a component's output is in the page, as a path from the container of the interactive root component it
/// belongs to. <see cref="Anchored"/> roots are found from the BevTools panel, which renders inside them; any other root
/// by <see cref="RootSignature"/> (the kinds of its first logical children) and, among the <see cref="RootCount"/>
/// identical roots of this runtime, by <see cref="RootOrdinal"/>; the page hub finds it by position only when the page
/// holds exactly that many such roots, and otherwise only when the path fits a single one of them.
/// </summary>
public sealed record BevToolsDomLocation(long InstanceId, string Name, bool Anchored, string RootSignature, int RootOrdinal, int RootCount, bool RootAmbiguous, IReadOnlyList<BevToolsDomStep> Path);
