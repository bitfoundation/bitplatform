namespace Bit.Butil;

/// <summary>
/// One payment method the site accepts - a URL-based method identifier such as
/// <c>"https://google.com/pay"</c> or <c>"https://apple.com/apple-pay"</c>, plus whatever
/// configuration that method defines.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Payment_Request_API/Concepts#payment_method_identifiers">Payment method identifiers</see>
/// </summary>
public class PaymentMethod
{
    /// <summary>The payment method identifier, almost always an https URL.</summary>
    public required string SupportedMethods { get; set; }

    /// <summary>
    /// The method's own configuration object, passed to the browser untouched. It is
    /// <see cref="object"/> because every method defines its own shape, and an anonymous object is
    /// the usual way to write one. Under trimming, put a
    /// <c>[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(T))]</c> on your call site
    /// for the type you pass, so <c>System.Text.Json</c> still has its properties to serialize.
    /// </summary>
    public object? Data { get; set; }
}
