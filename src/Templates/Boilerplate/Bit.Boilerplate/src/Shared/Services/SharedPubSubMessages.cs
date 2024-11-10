//+:cnd:noEmit
namespace Boilerplate.Shared.Services;

/// <summary>
/// This class is located in the Shared project to defines
/// message keys used for pub/sub messaging between server and client through SignalR.
/// For client-only pub/sub messages, refer to the ClientPubSubMessages class in the Client/Core project.
/// </summary>
public static partial class SharedPubSubMessages
{
    //#if (sample == "Admin")
    public const string DASHBOARD_DATA_CHANGED = nameof(DASHBOARD_DATA_CHANGED);
    //#else
    // To see examples of this class, checkout the admin panel sample.
    //#endif
}
