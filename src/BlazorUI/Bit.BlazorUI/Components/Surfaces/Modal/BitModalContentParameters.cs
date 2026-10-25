using System.Linq.Expressions;
using System.Reflection;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters of the component a modal service shows as the content of a modal, named by the component's own
/// properties rather than by strings:
/// <code>
/// await modalService.Show&lt;ConfirmContent&gt;(new BitModalContentParameters&lt;ConfirmContent&gt;
/// {
///     { c =&gt; c.Question, "Delete the project?" }
/// });
/// </code>
/// </summary>
/// <remarks>
/// It is the <see cref="Dictionary{TKey, TValue}"/> of names and values every Show overload already takes, so it goes
/// wherever one does, the parameters factory included. What it adds is the compiler's say: a parameter renamed or
/// removed, or a value of the wrong type, fails the build rather than the showing of the modal.
/// </remarks>
/// <typeparam name="TComponent">The component shown as the content of the modal.</typeparam>
public class BitModalContentParameters<TComponent> : Dictionary<string, object> where TComponent : IComponent
{
    /// <summary>
    /// Adds the value of one parameter of the content, named by the property it sets: <c>c =&gt; c.Question</c>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The expression names something other than a <c>[Parameter]</c> property of <typeparamref name="TComponent"/>,
    /// the value cannot be assigned to it, or the parameter was added already.
    /// </exception>
    public void Add<TValue>(Expression<Func<TComponent, TValue>> parameter, TValue value)
    {
        var property = GetProperty(parameter);

        // The compiler lets the value be of a type WIDER than the property's - a long for an int, an object for a
        // string - by inferring that type and converting the property to it inside the lambda. The value is then
        // checked here, where it is still the line that wrote it that gets told.
        if (value is null
                ? property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null
                : property.PropertyType.IsInstanceOfType(value) is false)
        {
            throw new ArgumentException(
                $"A value of type '{value?.GetType().Name ?? "null"}' cannot be assigned to the parameter '{property.Name}' of type '{property.PropertyType.Name}'.",
                nameof(value));
        }

        Add(property.Name, value!);
    }

    // The property the expression reads straight off the component. Checked here rather than left to the renderer,
    // which only finds out when the modal renders - after Show has returned - and reports it against the render tree
    // rather than against the line that named the wrong member.
    private static PropertyInfo GetProperty<TValue>(Expression<Func<TComponent, TValue>> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        var body = parameter.Body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
            ? convert.Operand
            : parameter.Body;

        if (body is MemberExpression { Member: PropertyInfo property } member &&
            member.Expression == parameter.Parameters[0] &&
            Attribute.IsDefined(property, typeof(ParameterAttribute), true))
        {
            return property;
        }

        throw new ArgumentException(
            $"The expression '{parameter}' does not name a [Parameter] property of '{typeof(TComponent).Name}'. Name one as 'c => c.Parameter'.",
            nameof(parameter));
    }
}
