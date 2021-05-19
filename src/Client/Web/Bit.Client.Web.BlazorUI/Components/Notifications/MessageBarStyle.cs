using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI
{
    public enum BitMessageBarType
    {
        Info,
        Warning,
        Error,
        Blocked,
        SevereWarning,
        Success
    }

    public static class MessageBarIcon
    {
        public static Dictionary<BitMessageBarType, string> IconMap = new()
        {
            [BitMessageBarType.Info] = "Info",
            [BitMessageBarType.Warning] = "Info",
            [BitMessageBarType.Error] = "ErrorBadge",
            [BitMessageBarType.Blocked] = "Blocked2",
            [BitMessageBarType.SevereWarning] = "Warning",
            [BitMessageBarType.Success] = "Completed"
        };
    }
}
