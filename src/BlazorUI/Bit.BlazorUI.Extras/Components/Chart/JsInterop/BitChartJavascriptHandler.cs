namespace Bit.BlazorUI;

/// <summary>
/// Represents a JavaScript function with a certain signature which will be called internally by JavaScript.
/// The type parameter <typeparamref name="T"/> defines the signature for the JavaScript method but doesn't have any actual use other than that.
/// </summary>
/// <typeparam name="T">The signature of the method you want to invoke from JavaScript. This type parameter is
/// nothing more than a convention which tells you what parameters and return type is expected from the JavaScript function.
/// Just like you might mistype the method name, you might return the wrong value from JavaScript. These cases can't be caught
/// by the compiler so make sure to double-check those.</typeparam>
public class BitChartJavascriptHandler<T> : IBitChartMethodHandler<T>
    where T : Delegate
{
    /// <summary>
    /// Gets the namespace and name of the JavaScript function to be called, separated by a point.
    /// <para>E.g. "SampleFunctions.ItemHoverHandler"</para>
    /// <para>Note 1: You must create this function in a JS file in wwwroot and reference it in index.html / _Host.cshtml.</para>
    /// <para>Note 2: Make sure the function has the expected parameters and return type. See <typeparamref name="T"/> for the expected signature.</para>
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartJavascriptHandler{T}"/>.
    /// </summary>
    /// <param name="methodName">The namespace and name of a JavaScript function (see <see cref="MethodName"/> for details).</param>
    public BitChartJavascriptHandler(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException("The method name cannot be null or whitespace. It has to include the namespace and name of the js-function.");

        if (methodName.Length < 3 || methodName.Count(c => c == '.') > 1)
            throw new ArgumentException("The method name has to contain the namespace and name of the js-function separated by a single point.");

        MethodName = methodName;
    }

    /// <summary>
    /// Converts a string to a <see cref="BitChartJavascriptHandler{T}"/> implicitly.
    /// </summary>
    /// <param name="methodName">The namespace and name of a JavaScript function to be called, separated by a point (see <see cref="MethodName"/> for details).</param>
    public static implicit operator BitChartJavascriptHandler<T>(string methodName) => new BitChartJavascriptHandler<T>(methodName);
}
