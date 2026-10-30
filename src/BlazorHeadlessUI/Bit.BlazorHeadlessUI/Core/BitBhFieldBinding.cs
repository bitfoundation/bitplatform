using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Connects a form control to Blazor's <see cref="EditContext"/> (inside an
/// <c>EditForm</c>): notifies field changes, tracks validation messages and
/// re-renders the owner when validation state changes. Works without an
/// <see cref="EditContext"/> too (everything becomes a no-op).
/// </summary>
internal sealed class BitBhFieldBinding : IDisposable
{
    private readonly Action _onValidationStateChanged;
    private EditContext? _editContext;
    private FieldIdentifier? _field;
    private LambdaExpression? _expression;

    public BitBhFieldBinding(Action onValidationStateChanged)
    {
        _onValidationStateChanged = onValidationStateChanged;
    }

    public FieldIdentifier? Field => _field;

    public void Update(EditContext? editContext, LambdaExpression? expression)
    {
        if (!ReferenceEquals(editContext, _editContext))
        {
            if (_editContext is not null) _editContext.OnValidationStateChanged -= HandleValidationStateChanged;
            _editContext = editContext;
            if (_editContext is not null) _editContext.OnValidationStateChanged += HandleValidationStateChanged;
        }

        if (expression is null || editContext is null)
            _field = null;
        else if (!ReferenceEquals(expression, _expression))
            _field = Create(expression);
        _expression = expression;
    }

    /// <summary>Whether the bound field currently has validation messages.</summary>
    public bool IsInvalid => _field is { } f && _editContext is not null && _editContext.GetValidationMessages(f).Any();

    /// <summary>The bound field's validation messages.</summary>
    public IEnumerable<string> Messages => _field is { } f && _editContext is not null
        ? _editContext.GetValidationMessages(f)
        : [];

    /// <summary>The EditContext CSS class (<c>valid</c>, <c>invalid</c>, <c>modified</c>) for the field.</summary>
    public string? CssClass => _field is { } f && _editContext is not null ? _editContext.FieldCssClass(f) : null;

    public void NotifyChanged()
    {
        if (_field is { } f && _editContext is not null)
            _editContext.NotifyFieldChanged(f);
    }

    /// <summary>Hands the validation state to the enclosing field (its error message, <c>data-invalid</c>, <c>aria-describedby</c>).</summary>
    public void ReportTo(BitBhFieldContext? field) => field?.ReportValidation(IsInvalid, Messages);

    /// <summary>Creates a <see cref="FieldIdentifier"/> from an accessor like <c>() => model.Email</c>.</summary>
    internal static FieldIdentifier? Create(LambdaExpression expression)
    {
        var body = expression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } convert)
            body = convert.Operand;
        if (body is not MemberExpression member)
            return null;

        var owner = Evaluate(member.Expression);
        return owner is null ? null : new FieldIdentifier(owner, member.Member.Name);
    }

    private static object? Evaluate(Expression? expression)
    {
        switch (expression)
        {
            case null:
                return null;
            case ConstantExpression constant:
                return constant.Value;
            case MemberExpression { Member: System.Reflection.FieldInfo field } fieldAccess:
                return field.GetValue(Evaluate(fieldAccess.Expression));
            case MemberExpression { Member: System.Reflection.PropertyInfo property } propertyAccess:
                return property.GetValue(Evaluate(propertyAccess.Expression));
            default:
                // A typed delegate: the non-generic Expression.Lambda has to make up its delegate type at
                // run time, which needs dynamic code (IL3050) and does not exist under native AOT.
                return Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile()();
        }
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) => _onValidationStateChanged();

    public void Dispose()
    {
        if (_editContext is not null) _editContext.OnValidationStateChanged -= HandleValidationStateChanged;
        _editContext = null;
    }
}
