using System.Linq.Expressions;

namespace Bit.Borm;

/// <summary>Fluent configuration for a wizard step.</summary>
public sealed class BormStepBuilder
{
    private readonly BormStep _step;
    internal BormStepBuilder(BormStep step) => _step = step;

    public BormStepBuilder Title(string title) { _step.Title = title; return this; }
    public BormStepBuilder Description(string description) { _step.Description = description; return this; }

    /// <summary>Adds fields to the step by path.</summary>
    public BormStepBuilder Fields(params string[] fields)
    {
        foreach (var f in fields) _step.Fields.Add(f);
        return this;
    }

    /// <summary>
    /// Adds a field to the step by expression, so renaming the property updates the step too.
    /// Nested members work: <c>Field&lt;Order&gt;(x =&gt; x.Address.City)</c>.
    /// </summary>
    public BormStepBuilder Field<TModel>(Expression<Func<TModel, object?>> selector)
    {
        _step.Fields.Add(BormExpressionPath.Of(selector));
        return this;
    }

    public BormStepBuilder VisibleWhen(IBormCondition condition) { _step.VisibleWhen = condition; return this; }

    /// <summary>Shows the step only while the value at <paramref name="field"/> satisfies the comparison.</summary>
    public BormStepBuilder VisibleWhen(string field, BormConditionOperator op, object? value = null)
        => VisibleWhen(new BormFieldCondition(field, op, value));

    /// <summary>Shows the step only while an expression holds — <c>{AccountKind} = 'business' and {Employees} &gt; 50</c>.</summary>
    /// <exception cref="BormExpressionException">The text is not a valid expression.</exception>
    public BormStepBuilder VisibleWhen(string expression) => VisibleWhen(new BormExpressionCondition(expression));
}
