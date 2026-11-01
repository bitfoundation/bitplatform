namespace Bit.Bcrdt.Tests;

/// <summary>
/// Updates written byte by byte, for what no replica would ever send: the hostile and extreme inputs
/// the engine and the stores must survive.
/// </summary>
internal static class HandWrittenUpdates
{
    public delegate void Writing(ref BcrdtBufferWriter writer);

    public static byte[] Build(Writing write)
    {
        var writer = new BcrdtBufferWriter(64);
        try
        {
            write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// An update holding one author's <paramref name="count"/> collected ranges of <paramref name="length"/>
    /// units each, from <paramref name="clock"/> on, and no deletions.
    /// </summary>
    public static byte[] CollectedRange(ulong client, ulong clock, int length = 1, int count = 1) => Build((ref BcrdtBufferWriter writer) =>
    {
        writer.WriteVarUInt(1);
        writer.WriteVarUInt((ulong)count);
        writer.WriteVarUInt(client);
        writer.WriteVarUInt(clock);
        for (int i = 0; i < count; i++)
        {
            writer.WriteByte((byte)BcrdtContentRef.GarbageCollected);
            writer.WriteVarUInt((ulong)length);
        }
        writer.WriteVarUInt(0);
    });
}
