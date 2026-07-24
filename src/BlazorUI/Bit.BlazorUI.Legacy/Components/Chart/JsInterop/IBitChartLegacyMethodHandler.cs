namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a type that's able to handle method calls coming from JavaScript.
/// </summary>
/// <typeparam name="T">The signature of the method this instance represents.
/// It may be an actual object or just a convention for the programmer.</typeparam>
public interface IBitChartLegacyMethodHandler<T> : IBitChartLegacyMethodHandler
    where T : Delegate
{
}

/// <summary>
/// Represents a type that's able to handle method calls coming from JavaScript.
/// In order to maintain the strongly typed nature of C#, please prefer using <see cref="IBitChartLegacyMethodHandler{T}"/>.
/// </summary>
//[Obsolete("Use " + nameof(IBitChartLegacyMethodHandler) + "<T> instead.")]
public interface IBitChartLegacyMethodHandler
{
    /// <summary>
    /// The name of the method which should be called from JavaScript.
    /// In the case of <see cref="BitChartLegacyJavascriptHandler{T}"/>, this is a reference
    /// to a JavaScript namespace + function. In the case of <see cref="BitChartLegacyDelegateHandler{T}"/>,
    /// this is the name of the delegate to be invoked by blazor.
    /// </summary>
    public string MethodName { get; }
}
