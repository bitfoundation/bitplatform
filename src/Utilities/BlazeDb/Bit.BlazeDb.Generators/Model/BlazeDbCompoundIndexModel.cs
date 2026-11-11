using System.Collections.Generic;

namespace Bit.BlazeDb.Generators;

/// <summary>An index over a tuple of properties, declared at type level.</summary>
internal sealed class BlazeDbCompoundIndexModel
{
    public string Name = "";
    public bool Ordered;
    public bool Unique;
    public List<BlazeDbPropModel> Props = new List<BlazeDbPropModel>();

    /// <summary>
    /// The value-tuple type used as the index key, e.g. "(string A, int B)". The element names are only for
    /// reading: a property whose name a tuple element cannot carry - <c>Rest</c>, <c>ToString</c>, or
    /// <c>Item2</c> anywhere but second - leaves its element unnamed rather than the generated code uncompilable,
    /// and a keyword is escaped. Generated code reaches the elements by position, never by these names.
    /// </summary>
    public string TupleType
    {
        get
        {
            var elements = new List<string>(Props.Count);
            for (var i = 0; i < Props.Count; i++)
            {
                var name = Props[i].Name;
                elements.Add(IsAllowedElementName(name, i)
                    ? Props[i].TypeDisplay + " " + BlazeDbSourceEmitter.Identifier(name)
                    : Props[i].TypeDisplay);
            }
            return "(" + string.Join(", ", elements) + ")";
        }
    }

    // The names C# refuses for a tuple element (CS8125, CS8126): ItemN at any position other than N, and the
    // members every value tuple already has.
    private static bool IsAllowedElementName(string name, int position)
    {
        switch (name)
        {
            case "CompareTo":
            case "Deconstruct":
            case "Equals":
            case "GetHashCode":
            case "Rest":
            case "ToString":
                return false;
        }
        return !(name.StartsWith("Item", System.StringComparison.Ordinal) &&
                 int.TryParse(name.Substring(4), out var number) &&
                 number != position + 1);
    }
}
