namespace Bit.Bcrdt;

/// <summary>
/// The globally unique address of a single logical unit of collaborative content.
/// </summary>
/// <remarks>
/// <para>
/// An identifier pairs the client that created the content with that client's logical clock at the
/// moment of creation. Because a client's clock advances by the <i>length</i> of everything it
/// creates, an identifier addresses a unit rather than an operation: if client <c>c</c> types ten
/// characters starting at clock 100, then <c>(c, 105)</c> is the sixth of those characters even
/// though only one block was ever created.
/// </para>
/// <para>
/// That property is what lets the engine split a run-length-encoded block without invalidating
/// anything that points into it, which in turn is what makes efficient text editing possible.
/// </para>
/// </remarks>
/// <param name="Client">The identifier of the creating client session.</param>
/// <param name="Clock">The creating client's logical clock at the moment of creation.</param>
public readonly record struct BcrdtItemId(ulong Client, long Clock) : IComparable<BcrdtItemId>
{
    /// <summary>
    /// Orders identifiers by client, then clock.
    /// </summary>
    /// <remarks>
    /// This ordering is a deterministic total order over identifiers, but it carries no causal
    /// meaning: two identifiers from different clients say nothing about which happened first.
    /// Use it for sorting and binary search, never to decide concurrency.
    /// </remarks>
    public int CompareTo(BcrdtItemId other)
    {
        int byClient = Client.CompareTo(other.Client);
        return byClient != 0 ? byClient : Clock.CompareTo(other.Clock);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Client}@{Clock}";

    /// <summary>Writes the identifier as two unsigned varints.</summary>
    public void Write(ref BcrdtBufferWriter writer)
    {
        writer.WriteVarUInt(Client);
        writer.WriteVarUInt((ulong)Clock);
    }

    /// <summary>Reads an identifier written by <see cref="Write"/>.</summary>
    public static BcrdtItemId Read(ref BcrdtBufferReader reader)
    {
        ulong client = reader.ReadVarUInt();
        ulong clock = reader.ReadVarUInt();
        if (clock > long.MaxValue) throw new BcrdtFormatException("Clock exceeds Int64 range.");
        return new BcrdtItemId(client, (long)clock);
    }
}
