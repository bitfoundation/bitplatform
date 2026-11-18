namespace Bit.BlazeDb;

/// <summary>A named number the engine keeps about a database itself, such as its user version.</summary>
internal sealed record BlazeDbMetaEntry(string Key, long Value);
