﻿using System.Linq;

namespace Bit.Client.Web.BlazorUI
{
    public static class BitPersonaPresenceSize
    {
        public const string Size6 = "6px";
        public const string Size8 = "8px";
        public const string Size12 = "12px";
        public const string Size16 = "16px";
        public const string Size20 = "20px";
        public const string Size28 = "28px";
        public const string Size32 = "32px";

        public static int SizeToPixels(string size)
        {
            return int.Parse(size.Substring(0, size.Count() - 2));
        }
    }
}
