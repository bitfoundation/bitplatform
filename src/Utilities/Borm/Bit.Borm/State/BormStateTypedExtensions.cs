namespace Bit.Borm;

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
