using System;

namespace Bit.Butil.Build;

/// <summary>A metadata table and a one-based row in it. Row 0 means "nothing".</summary>
public readonly struct MetadataToken(int table, int row)
{
    public int Table { get; } = table;

    public int Row { get; } = row;

    public bool IsNil => Row == 0;
}
