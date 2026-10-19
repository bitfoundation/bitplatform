namespace Bit.Borm;

/// <summary>
/// A condition backed by an arbitrary delegate. Powerful but not serializable to JSON;
/// use <see cref="BormFieldCondition"/> / <see cref="BormConditionGroup"/> when round-tripping schemas.
/// </summary>
public sealed class BormPredicateCondition : IBormCondition
{
    private readonly Func<IBormDataReader, bool> _predicate;

    public BormPredicateCondition(Func<IBormDataReader, bool> predicate, params string[] dependencies)
    {
        _predicate = predicate;
        Dependencies = dependencies;
    }

    public IEnumerable<string> Dependencies { get; }

    public bool Evaluate(IBormDataReader data) => _predicate(data);
}
