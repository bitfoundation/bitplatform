using System.Collections.Generic;

namespace Bit.BlazeDb.Generators;

/// <summary>An index over a tuple of properties, declared at type level.</summary>
internal sealed class BlazeDbCompoundIndexModel
{
    public string Name = "";
    public bool Ordered;
    public bool Unique;
    public List<BlazeDbPropModel> Props = new List<BlazeDbPropModel>();

    /// <summary>The value-tuple type used as the index key, e.g. "(string A, int B)".</summary>
    public string TupleType =>
        "(" + string.Join(", ", Props.ConvertAll(p => p.TypeDisplay + " " + p.Name)) + ")";
}
