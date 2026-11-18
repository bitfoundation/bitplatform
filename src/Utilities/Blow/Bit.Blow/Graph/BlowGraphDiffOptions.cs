using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blow;

/// <summary>How <see cref="BlowGraphDiff"/> tells a change from none.</summary>
public sealed class BlowGraphDiffOptions
{
    /// <summary>How far a node may move, in flow units on each axis, and still count as where it was. 0 by default.</summary>
    public double PositionTolerance { get; set; }

    /// <summary>When true, selection counts as a change. It does not by default, as clicking around is not editing.</summary>
    public bool IncludeSelection { get; set; }

    /// <summary>
    /// Whether two data payloads are the same. By default: the same object, equal by <see cref="object.Equals(object?)"/>,
    /// or JSON with the same content (<see cref="JsonNode"/>, <see cref="JsonElement"/>) - which is what data read from a
    /// document is.
    /// </summary>
    public Func<object?, object?, bool>? DataEquals { get; set; }

    /// <summary>Property names to leave out of the comparison: "Position" to compare structure only, say.</summary>
    public ISet<string>? Ignore { get; set; }
}
