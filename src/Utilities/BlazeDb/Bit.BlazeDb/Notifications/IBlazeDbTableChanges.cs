namespace Bit.BlazeDb;

internal interface IBlazeDbTableChanges
{
    /// <summary>Raises the table's event with its net changes and adds them, boxed, to <paramref name="all"/> when given.</summary>
    void Deliver(List<BlazeDbRowChange>? all, ref List<Exception>? errors);
}
