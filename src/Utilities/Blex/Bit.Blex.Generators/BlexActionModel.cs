namespace Bit.Blex.Generators;

/// <param name="ImplName">Name of the user-written action method the wrapper calls.</param>
/// <param name="WrapperName">Name of the generated public wrapper.</param>
/// <param name="ActionLabel">Name the action is dispatched and recorded under.</param>
/// <param name="IsAsync">True when the method returns Task, ValueTask or one of their generic forms.</param>
/// <param name="IsValueTask">True when the method returns ValueTask or ValueTask&lt;T&gt;.</param>
/// <param name="Parameters">The method's parameters.</param>
/// <param name="ResultTypeFqn">
/// The type the wrapper returns a value of -- <c>T</c> of a sync method returning <c>T</c>, or of one returning
/// <c>Task&lt;T&gt;</c>/<c>ValueTask&lt;T&gt;</c> -- or <c>null</c> when the method returns nothing (<c>void</c>,
/// <c>Task</c>, <c>ValueTask</c>).
/// </param>
internal sealed record BlexActionModel(
    string ImplName,
    string WrapperName,
    string ActionLabel,
    bool IsAsync,
    bool IsValueTask,
    string? ResultTypeFqn,
    BlexEquatableArray<BlexParamModel> Parameters) : System.IEquatable<BlexActionModel>;
