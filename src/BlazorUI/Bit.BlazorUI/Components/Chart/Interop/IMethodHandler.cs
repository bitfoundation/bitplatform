namespace Bit.BlazorUI;

#pragma warning disable CS0618 // Type or member is obsolete

/// <summary>
/// Represents a type that's able to handle method calls coming from JavaScript.
/// </summary>
/// <typeparam name="T">The signature of the method this instance represents.
/// It may be an actual object or just a convention for the programmer.</typeparam>
public interface IMethodHandler<T> : IMethodHandler
    where T : Delegate
{
}

#pragma warning restore CS0618 // Type or member is obsolete

/// <summary>
/// Represents a type that's able to handle method calls coming from JavaScript.
/// In order to maintain the strongly typed nature of C#, please prefer using <see cref="IMethodHandler{T}"/>.
/// </summary>
[Obsolete("Use " + nameof(IMethodHandler) + "<T> instead.")]
public interface IMethodHandler
{
    /// <summary>
    /// The name of the method which should be called from JavaScript.
    /// In the case of <see cref="JavaScriptHandler{T}"/>, this is a reference
    /// to a JavaScript namespace + function. In the case of <see cref="DelegateHandler{T}"/>,
    /// this is the name of the delegate to be invoked by blazor.
    /// </summary>
    public string MethodName { get; }
}
