namespace Bit.BlazeDb;

/// <summary>What a committed write did to a row.</summary>
public enum BlazeDbChangeKind
{
    /// <summary>The row did not exist before the commit and does after it.</summary>
    Inserted,

    /// <summary>The row existed before the commit and still does, with new values.</summary>
    Updated,

    /// <summary>The row existed before the commit and does not after it.</summary>
    Deleted,
}
