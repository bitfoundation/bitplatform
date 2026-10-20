namespace Bit.Borm;

/// <summary>
/// Combines child conditions with <see cref="BormConditionLogic.And"/> or <see cref="BormConditionLogic.Or"/>.
/// </summary>
public sealed class BormConditionGroup : IBormCondition
{
    public BormConditionGroup(BormConditionLogic logic, params IBormCondition[] conditions)
    {
        Logic = logic;
        Conditions = [.. conditions];
    }

    public BormConditionLogic Logic { get; }
    public IReadOnlyList<IBormCondition> Conditions { get; }

    /// <summary>
    /// Every path the children read. A child that declares none — a raw predicate — depends on
    /// everything, and so, through it, does the group: flattening it away left the group re-evaluated
    /// only when its <em>other</em> children's fields changed, and a field hidden by the predicate was
    /// never cleared.
    /// </summary>
    public IEnumerable<string> Dependencies
    {
        get
        {
            var all = new List<string>();
            foreach (var condition in Conditions)
            {
                var before = all.Count;
                all.AddRange(condition.Dependencies);
                if (all.Count == before) return [];
            }
            return all;
        }
    }

    /// <summary>
    /// The paths the field conditions in this group name, at any depth — including those beside a child
    /// that depends on everything, which <see cref="Dependencies"/> cannot list. For schema diagnostics.
    /// </summary>
    internal static IEnumerable<string> NamedPaths(IBormCondition? condition) => condition switch
    {
        null => [],
        BormConditionGroup group => group.Conditions.SelectMany(NamedPaths),
        _ => condition.Dependencies
    };

    public bool Evaluate(IBormDataReader data)
        => Logic == BormConditionLogic.And
            ? Conditions.All(c => c.Evaluate(data))
            : Conditions.Any(c => c.Evaluate(data));

    public static BormConditionGroup All(params IBormCondition[] conditions) => new(BormConditionLogic.And, conditions);
    public static BormConditionGroup Any(params IBormCondition[] conditions) => new(BormConditionLogic.Or, conditions);
}
