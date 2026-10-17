namespace Bit.BlazeDb.EntityFrameworkCore.Metadata;

/// <summary>
/// One secondary index, described in the terms the translator reasons about: which properties it
/// covers, whether it can answer ranges as well as equality, and the type of its key - a tuple
/// when the index is compound, which is what a lookup against it has to be given.
/// </summary>
internal sealed record BlazeDbIndex(
    string Name, IReadOnlyList<string> Members, bool Ordered, Type KeyType, object Definition);
