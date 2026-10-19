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

    public IEnumerable<string> Dependencies => Conditions.SelectMany(c => c.Dependencies);

    public bool Evaluate(IBormDataReader data)
        => Logic == BormConditionLogic.And
            ? Conditions.All(c => c.Evaluate(data))
            : Conditions.Any(c => c.Evaluate(data));

    public static BormConditionGroup All(params IBormCondition[] conditions) => new(BormConditionLogic.And, conditions);
    public static BormConditionGroup Any(params IBormCondition[] conditions) => new(BormConditionLogic.Or, conditions);
}
