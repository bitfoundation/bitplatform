//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Attachments;

public enum AttachmentKind
{
    /// <summary>
    /// Resized to fit within 256*256px, preserving the aspect ratio.
    /// </summary>
    UserProfileImageSmall,
    UserProfileImageOriginal,
    //#if (module == "Sales" || module == "Admin")
    /// <summary>
    /// Resized to fit within 512*512px, preserving the aspect ratio.
    /// </summary>
    ProductPrimaryImageMedium,
    ProductPrimaryImageOriginal,
    //#endif
    //#if (signalR == true)
    /// <summary>
    /// An image the user attached to a message in the AI chat panel.
    /// </summary>
    AiChatImage
    //#endif
}
