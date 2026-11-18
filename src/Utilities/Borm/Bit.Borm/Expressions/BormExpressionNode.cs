using System.Collections;

namespace Bit.Borm;

/// <summary>One node of a parsed <see cref="BormExpression"/>.</summary>
internal abstract class BormExpressionNode
{
    /// <summary>Evaluates the node. The result may be <see cref="BormExpressionValue.Unknown"/>.</summary>
    public abstract object? Evaluate(IBormDataReader data);

    /// <summary>Adds every field path this node and its children read to <paramref name="paths"/>.</summary>
    public virtual void CollectPaths(List<string> paths) { }
}
