using System.Linq.Expressions;

namespace Bit.Borm;

/// <summary>
/// The state of a form bound to a <typeparamref name="TModel"/>, addressed by expression rather than by
/// path text — <c>form.SetValue(x =&gt; x.Lines[i].Quantity, 3)</c> — so renaming a property renames
/// every place the page reads or writes it, and a typo is a compile error rather than a value that
/// quietly goes nowhere. Get one with <see cref="BormStateTypedExtensions.For{TModel}"/>.
/// </summary>
/// <remarks>
/// A thin view: every call resolves the expression to its path and asks the <see cref="State"/>, so the
/// two can be used side by side and say the same thing.
/// </remarks>
public readonly struct BormStateOf<TModel>
{
    internal BormStateOf(BormState state) => State = state ?? throw new ArgumentNullException(nameof(state));

    /// <summary>The state this view reads and writes.</summary>
    public BormState State { get; }

    /// <summary>The path <paramref name="selector"/> stands for, e.g. <c>Lines[2].Quantity</c>.</summary>
    public string Path<TValue>(Expression<Func<TModel, TValue>> selector) => BormExpressionPath.OfValue(selector);

    /// <summary>Reads a value, converted to <typeparamref name="TValue"/> when the store holds another type.</summary>
    public TValue? GetValue<TValue>(Expression<Func<TModel, TValue>> selector) => State.GetValue<TValue>(Path(selector));

    /// <inheritdoc cref="BormState.SetValue" />
    public void SetValue<TValue>(Expression<Func<TModel, TValue>> selector, TValue value) => State.SetValue(Path(selector), value);

    /// <inheritdoc cref="BormState.SetValueQuietly" />
    public void SetValueQuietly<TValue>(Expression<Func<TModel, TValue>> selector, TValue value)
        => State.SetValueQuietly(Path(selector), value);

    /// <inheritdoc cref="BormState.GetFieldState" />
    public BormFieldState GetFieldState<TValue>(Expression<Func<TModel, TValue>> selector) => State.GetFieldState(Path(selector));

    /// <inheritdoc cref="BormState.MessagesFor" />
    public IReadOnlyList<BormValidationMessage> MessagesFor<TValue>(Expression<Func<TModel, TValue>> selector)
        => State.MessagesFor(Path(selector));

    /// <inheritdoc cref="BormState.IsDirty" />
    public bool IsDirty<TValue>(Expression<Func<TModel, TValue>> selector) => State.IsDirty(Path(selector));

    /// <inheritdoc cref="BormState.IsTouched" />
    public bool IsTouched<TValue>(Expression<Func<TModel, TValue>> selector) => State.IsTouched(Path(selector));

    /// <inheritdoc cref="BormState.ValidateFieldAsync(string, bool)" />
    public ValueTask<bool> ValidateFieldAsync<TValue>(Expression<Func<TModel, TValue>> selector, bool includeAsync = true)
        => State.ValidateFieldAsync(Path(selector), includeAsync);

    /// <inheritdoc cref="BormState.FocusAsync" />
    public ValueTask<bool> FocusAsync<TValue>(Expression<Func<TModel, TValue>> selector) => State.FocusAsync(Path(selector));

    /// <inheritdoc cref="BormState.ResetField" />
    public void ResetField<TValue>(Expression<Func<TModel, TValue>> selector) => State.ResetField(Path(selector));

    /// <inheritdoc cref="BormState.SetServerError" />
    public void SetServerError<TValue>(Expression<Func<TModel, TValue>> selector, string message)
        => State.SetServerError(Path(selector), message);
}

/// <summary>Typed access to a <see cref="BormState"/>.</summary>
public static class BormStateTypedExtensions
{
    /// <summary>
    /// A view of <paramref name="state"/> that addresses fields by expression on
    /// <typeparamref name="TModel"/>: <c>state.For&lt;Order&gt;().GetValue(x =&gt; x.Total)</c>.
    /// </summary>
    /// <exception cref="ArgumentException">The form is bound to a model that is not a <typeparamref name="TModel"/>.</exception>
    public static BormStateOf<TModel> For<TModel>(this BormState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        // A dictionary-backed form has no model type to disagree with; a typed one has to match, or every
        // path the view produces would name properties the model does not have.
        if (state.Data.Root is { } root && root is not IDictionary<string, object?> && root is not TModel)
            throw new ArgumentException(
                $"The form is bound to a {root.GetType().Name}, not a {typeof(TModel).Name}.", nameof(state));
        return new BormStateOf<TModel>(state);
    }
}
