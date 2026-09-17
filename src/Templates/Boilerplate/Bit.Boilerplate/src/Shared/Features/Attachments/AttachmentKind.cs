//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Attachments;

/// <summary>
/// Persisted as an integer, so every member has an explicit value: the template options remove some members, and
/// implicit values would then shift between projects that share a database.
/// </summary>
public enum AttachmentKind
{
    /// <summary>
    /// Resized to fit within 256*256px, preserving the aspect ratio.
    /// </summary>
    UserProfileImageSmall = 0,
    UserProfileImageOriginal = 1,
    //#if (module == "Sales" || module == "Admin")
    /// <summary>
    /// Resized to fit within 512*512px, preserving the aspect ratio.
    /// </summary>
    ProductPrimaryImageMedium = 2,
    ProductPrimaryImageOriginal = 3,
    //#endif
    //#if (signalR == true)
    /// <summary>
    /// An image the user attached to a message in the AI chat panel.
    /// </summary>
    AiChatImage = 4
    //#endif
}
