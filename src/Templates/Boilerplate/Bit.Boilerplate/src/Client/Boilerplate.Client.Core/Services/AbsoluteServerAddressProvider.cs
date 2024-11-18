namespace Boilerplate.Client.Core.Services;

/// <summary>
/// The `ServerAddress` setting in `Client.Core/appsettings.json` can be either relative or absolute.
/// If the server address is relative, we prepend it with `builder.HostEnvironment.BaseAddress` in Blazor WebAssembly 
/// or with the request URL from `IHttpContextAccessor.HttpContext.Request` in Blazor Server.
/// The resulting server address is useful in various scenarios, such as binding an image's `src` attribute to a server API, 
/// like retrieving the current user's profile image.
/// </summary>
public class AbsoluteServerAddressProvider
{
    public required Func<Uri> GetAddress { get; init; }

    public static implicit operator string(AbsoluteServerAddressProvider provider)
    {
        return provider.GetAddress().ToString();
    }

    public static implicit operator Uri(AbsoluteServerAddressProvider provider)
    {
        return provider.GetAddress();
    }

    public override string ToString()
    {
        return this;
    }
}
