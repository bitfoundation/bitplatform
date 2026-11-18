namespace Bit.BevTools;

/// <summary>A table's sort: the column and its direction. Selecting the sorted column again reverses it.</summary>
internal sealed class BevToolsSortState(string key, bool ascending)
{
    public string Key { get; private set; } = key;

    public bool Ascending { get; private set; } = ascending;

    public void Set(string key, bool ascending)
    {
        Key = key;
        Ascending = ascending;
    }

    /// <param name="key">The column selected.</param>
    /// <param name="ascendingFirst">The direction a column starts in: text A to Z, figures largest first.</param>
    public void Select(string key, bool ascendingFirst)
    {
        if (Key == key)
        {
            Ascending = !Ascending;
        }
        else
        {
            Key = key;
            Ascending = ascendingFirst;
        }
    }
}
