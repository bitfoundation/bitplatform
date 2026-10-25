using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

/// <summary>A named number the engine keeps about a database itself, such as its user version.</summary>
internal sealed record BlazeDbMetaEntry(string Key, long Value);

/// <summary>
/// The engine's own table, registered in every database, for values about the database rather than in
/// it - <see cref="BlazeDbDatabase.UserVersion"/> today. It is an ordinary table on purpose: a write to
/// it joins the open transaction, is journaled and checkpointed like any other, and a version of the
/// engine that predates it carries its rows along untouched as an unregistered table. Snapshots leave
/// it out while it is empty, so a database that never uses it looks exactly as it did before.
/// </summary>
internal static class BlazeDbMetaTable
{
    /// <summary>Table names starting with this are the engine's; an application cannot register one.</summary>
    public const string ReservedPrefix = "__blazedb";

    public const string Name = ReservedPrefix + "_meta";

    public const string UserVersionKey = "user-version";

    public static readonly BlazeDbTableDescriptor<string, BlazeDbMetaEntry> Descriptor = new(
        Name,
        static entry => entry.Key,
        static (writer, entry) =>
        {
            writer.WriteTag(1, BlazeDbWireType.LengthDelimited);
            writer.WriteString(entry.Key);
            writer.WriteTag(2, BlazeDbWireType.VarInt);
            writer.WriteVarInt(entry.Value);
        },
        static (ref BlazeDbBufferReader reader) =>
        {
            var key = "";
            var value = 0L;
            while (reader.Remaining > 0)
            {
                var (field, wire) = reader.ReadTag();
                switch (field)
                {
                    case 1 when wire == BlazeDbWireType.LengthDelimited:
                        key = reader.ReadString();
                        break;
                    case 2 when wire == BlazeDbWireType.VarInt:
                        value = reader.ReadVarInt();
                        break;
                    default:
                        reader.SkipField(wire);
                        break;
                }
            }
            return new BlazeDbMetaEntry(key, value);
        },
        static (writer, key) => writer.WriteString(key),
        static (ref BlazeDbBufferReader reader) => reader.ReadString(),
        keyMember: nameof(BlazeDbMetaEntry.Key));
}
