// Guards the recursive building of PDF objects that may refer to themselves.

namespace Bit.BlazorUI;

/// <summary>
/// One recursive resolution - a color space built from its base or alternate space, a
/// function built from its sub-functions - tracked as it descends. A builder enters every
/// object it recurses into and leaves it on the way back; entering fails, and the builder
/// falls back to its documented default, when the object is already being resolved further
/// up (a cycle such as an Indexed space over itself), when the chain is deeper than any real
/// document needs, or when the whole resolution has visited more objects than its budget
/// (a shared sub-object fanned out at every level). Without it such input recurses until the
/// stack overflows, which no catch can stop and which takes the whole process down.
/// </summary>
internal sealed class BitPdfResolveScope
{
    private readonly HashSet<object> _active = new(ReferenceEqualityComparer.Instance);
    private readonly int _maxDepth;
    private int _budget;

    /// <param name="maxDepth">The longest chain of nested objects resolved.</param>
    /// <param name="budget">The most objects one resolution enters in total.</param>
    public BitPdfResolveScope(int maxDepth, int budget)
    {
        _maxDepth = maxDepth;
        _budget = budget;
    }

    /// <summary>
    /// Enters <paramref name="node"/> (compared by reference), or returns <c>false</c> when
    /// it is already being resolved, the chain is at its depth limit or the budget is spent.
    /// Every successful call is paired with <see cref="Exit"/>.
    /// </summary>
    public bool TryEnter(object node)
    {
        if (_active.Count >= _maxDepth || _budget <= 0 || !_active.Add(node))
        {
            return false;
        }
        _budget--;
        return true;
    }

    /// <summary>Leaves <paramref name="node"/>, entered by <see cref="TryEnter"/>.</summary>
    public void Exit(object node) => _active.Remove(node);
}
