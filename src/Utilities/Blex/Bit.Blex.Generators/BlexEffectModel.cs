namespace Bit.Blex.Generators;

/// <param name="ImplName">Name of the user-written effect method the wrapper calls.</param>
/// <param name="WrapperName">Name of the generated public wrapper.</param>
/// <param name="ActionLabel">Action name the effect's lifecycle is reported under.</param>
/// <param name="IsValueTask">True when the impl method returns ValueTask rather than Task.</param>
/// <param name="Concurrency">0 = Parallel, 1 = Latest, 2 = Drop, 3 = Queue (mirrors Bit.Blex.BlexEffectConcurrency).</param>
/// <param name="HasCancellationToken">True when the impl method's last parameter is a CancellationToken supplied by the wrapper.</param>
/// <param name="DebounceMilliseconds">Quiet period before the body runs; 0 disables debouncing.</param>
/// <param name="Parameters">The impl method's parameters, minus a trailing CancellationToken.</param>
internal sealed record BlexEffectModel(
    string ImplName,
    string WrapperName,
    string ActionLabel,
    bool IsValueTask,
    int Concurrency,
    bool HasCancellationToken,
    int DebounceMilliseconds,
    BlexEquatableArray<BlexParamModel> Parameters) : System.IEquatable<BlexEffectModel>;
