using System.Collections.ObjectModel;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bit.BlazorUI;

/// <summary>
/// Represents a C#-delegate which can be called by JavaScript.
/// </summary>
/// <typeparam name="T">The type of the delegate you want to invoke from JavaScript.</typeparam>
// This class will be serialized by System.Text.Json in the end since we restore the objects
// before passing them to IJsRuntime. Therefore fields to serialize have to be public properties.
public sealed class BitChartDelegateHandler<T> : IBitChartMethodHandler<T>, IDisposable
    where T : Delegate
{
    private static readonly ParameterInfo[] _delegateParameters;
    private static readonly bool _delegateHasReturnValue;

    private readonly T _function;
    private readonly IList<int> _ignoredIndices;

    /// <summary>
    /// Gets the name of the method which should be called from JavaScript.
    /// In this case it's always the name of the <see cref="Invoke"/>-method.
    /// </summary>
    public string MethodName => nameof(Invoke);

    /// <summary>
    /// Gets a reference to this object which is used to invoke the stored delegate from JavaScript.
    /// </summary>
    [JsonIgnore] // This property only has to be serialized by the JSRuntime where a custom converter will be used.
    public DotNetObjectReference<BitChartDelegateHandler<T>> HandlerReference { get; }

    /// <summary>
    /// Gets a value indicating whether or not this delegate will return a value.
    /// </summary>
    public bool ReturnsValue => _delegateHasReturnValue;

    /// <summary>
    /// Gets the indices of the ignored callback parameters. The parameters at these indices won't
    /// be sent to C# and won't be deserialized. These indices are defined by the
    /// <see cref="BitChartIgnoreCallbackValueAttribute"/>s on the delegate passed to this instance.
    /// </summary>
    public IReadOnlyCollection<int> IgnoredIndices { get; }

    static BitChartDelegateHandler()
    {
        // https://stackoverflow.com/a/429564/10883465
        MethodInfo internalDelegateMethod = typeof(T).GetMethod("Invoke");

        _delegateParameters = internalDelegateMethod.GetParameters();
        _delegateHasReturnValue = internalDelegateMethod.ReturnType != typeof(void);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartDelegateHandler{T}"/>.
    /// </summary>
    /// <param name="function">The delegate you want to invoke from JavaScript.</param>
    public BitChartDelegateHandler(T function)
    {
        _function = function ?? throw new ArgumentNullException(nameof(function));
        ParameterInfo[] parameters = _function.GetMethodInfo().GetParameters();
        _ignoredIndices = new List<int>();
        IgnoredIndices = new ReadOnlyCollection<int>(_ignoredIndices);
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].GetCustomAttribute<BitChartIgnoreCallbackValueAttribute>(false) != null)
            {
                _ignoredIndices.Add(i);
            }
        }

        HandlerReference = DotNetObjectReference.Create(this);
    }

    /// <summary>
    /// Invokes the delegate dynamically. This method should only be called from JavaScript.
    /// </summary>
    /// <param name="jsonArgs">
    /// All the arguments for the method as array of json-strings.
    /// This array can contain ANYTHING, do not trust its values.
    /// </param>
    [JSInvokable]
    public object Invoke(params string[] jsonArgs)
    {
        if (_delegateParameters.Length != jsonArgs.Length)
            throw new ArgumentException($"The function expects {_delegateParameters.Length} arguments but found {jsonArgs.Length}.");

        if (_delegateParameters.Length == 0)
            return _function.DynamicInvoke(null);

        object[] invokationArgs = new object[_delegateParameters.Length];
        for (int i = 0; i < _delegateParameters.Length; i++)
        {
            if (_ignoredIndices.Contains(i))
                continue;

            Type deserializeType = _delegateParameters[i].ParameterType;
            if (deserializeType == typeof(object) ||
                typeof(JToken).IsAssignableFrom(deserializeType))
            {
                invokationArgs[i] = JToken.Parse(jsonArgs[i]);
            }
            else
            {
#if DEBUG
                Console.WriteLine($"Deserializing: {jsonArgs[i]} to {deserializeType.Name}");
#endif
                invokationArgs[i] = JsonConvert.DeserializeObject(jsonArgs[i], deserializeType, BitChartJsInterop.JsonSerializerSettings);
            }
        }

        return _function.DynamicInvoke(invokationArgs);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        HandlerReference.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The <see cref="Dispose"/> method doesn't have any unmanaged resources to free BUT once this object is finalized
    /// we need to prevent any further use of the <see cref="DotNetObjectReference"/> to this object. Since the <see cref="HandlerReference"/>
    /// will only be disposed if this <see cref="BitChartDelegateHandler{T}"/> instance is disposed or when <c>dispose</c> is called from JavaScript
    /// (which shouldn't happen) we HAVE to dispose the reference when this instance is finalized.
    /// </summary>
    ~BitChartDelegateHandler()
    {
        Dispose();
    }

    /// <summary>
    /// Converts a delegate of type <typeparamref name="T"/> to a <see cref="BitChartDelegateHandler{T}"/> implicitly.
    /// </summary>
    /// <param name="function"></param>
    public static implicit operator BitChartDelegateHandler<T>(T function) => new BitChartDelegateHandler<T>(function);
}
