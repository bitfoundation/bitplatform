using Microsoft.AspNetCore.Components;
using Bit.Butil.Demo.Client.Pages;

namespace Bit.Butil.Demo.Client.Docs;

/// <summary>
/// The preconditions an API imposes on the calling page, beyond simply being implemented.
/// </summary>
[Flags]
public enum ApiNeeds
{
    None = 0,

    /// <summary>Only available over HTTPS or on localhost.</summary>
    SecureContext = 1,

    /// <summary>The browser prompts the user, and the call fails if permission is denied.</summary>
    Permission = 2,

    /// <summary>Must be called from a user-gesture handler such as a click.</summary>
    UserGesture = 4,

    /// <summary>Behind an experimental or origin-trial flag in at least one shipping engine.</summary>
    Experimental = 8,
}
