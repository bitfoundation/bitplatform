namespace Bit.BlazorUI;

/// <summary>
/// Marks a class that only registers the packages' services (AddBitBlazorUIServices and the like), which the
/// publish-time asset trimming does not follow.
/// </summary>
/// <remarks>
/// Where ILLink does not trim the publish, the trimming works out what an app can render from the library types its code
/// references, and from everything those reach in turn. Registering a service reaches the service, and a service reaches
/// what it renders - the modal service the modal - so following the one call every app makes would keep the stylesheets
/// and scripts of every service-rendered component in every app. Registering renders nothing, though, and an app that
/// uses a service names it itself (an @inject of it), which is what keeps what the service renders. Read by name, by
/// Bit.BlazorUI.Build: renaming it is a change to that task too.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class BitAssetTrimmingRegistrarAttribute : Attribute
{
}
