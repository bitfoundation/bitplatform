//+:cnd:noEmit
namespace Boilerplate.Shared.Enums;

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
    ProductPrimaryImageMedium,
    ProductPrimaryImageOriginal
    //#endif
}
