//+:cnd:noEmit
namespace Boilerplate.Server.Api.Models.Attachments;

public enum AttachmentKind
{
    /// <summary>
    /// 256*256px
    /// </summary>
    UserProfileImageSmall,
    UserProfileImageOriginal,
    //#if (module == "Sales" || module == "Admin")
    /// <summary>
    /// 512*515px
    /// </summary>
    ProductImageMedium,
    ProductImageOriginal
    //#endif
}
