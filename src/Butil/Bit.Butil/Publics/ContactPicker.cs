using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Contact_Picker_API">Contact Picker API</see>
/// (<c>navigator.contacts</c>).
/// </summary>
/// <remarks>
/// Available on Chromium-based mobile browsers only. Users always see a native picker
/// - there's no programmatic access to a user's contacts.
/// </remarks>
public class ContactPicker(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.contacts</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.contactPicker.isSupported");

    /// <summary>
    /// Returns the list of properties the platform can expose. Common values: <c>"name"</c>,
    /// <c>"email"</c>, <c>"tel"</c>, <c>"address"</c>, <c>"icon"</c>.
    /// </summary>
    public ValueTask<string[]> GetProperties() => js.Invoke<string[]>("BitButil.contactPicker.getProperties");

    /// <summary>
    /// Opens the contact picker and returns the user's selection. Must be invoked from a
    /// user-gesture handler.
    /// </summary>
    /// <param name="properties">Subset of <see cref="GetProperties"/>. Defaults to name/email/tel.</param>
    /// <param name="multiple">When true, the user can pick more than one contact.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ContactInfo))]
    public ValueTask<ContactInfo[]> Select(string[]? properties = null, bool multiple = false)
    {
        // Treat null *and* empty as "use the default set". An empty array is truthy in JS, so the
        // TS side can't fall back on its own - normalize it here so the two defaults stay in sync.
        var props = properties is { Length: > 0 } ? properties : new[] { "name", "email", "tel" };
        return js.Invoke<ContactInfo[]>("BitButil.contactPicker.select", props, multiple);
    }
}
