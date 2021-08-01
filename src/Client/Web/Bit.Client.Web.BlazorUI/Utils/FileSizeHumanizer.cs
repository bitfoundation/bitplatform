namespace Bit.Client.Web.BlazorUI
{
    public static class FileSizeHumanizer
    {
        public const long OneKiloByte = 1024;
        public const long OneMegaByte = OneKiloByte * 1024;
        public const long OneGigaByte = OneMegaByte * 1024;

        public static string Humanize(this long size)
        {
            string suffix;
            if (size > OneGigaByte)
            {
                float formatedSize = size / (float)OneGigaByte;
                suffix = "GB";
                return $"{formatedSize:0.00}{suffix}";
            }

            if (size > OneMegaByte)
            {
                size /= OneMegaByte;
                suffix = "MB";
            }
            else if (size > OneKiloByte)
            {
                size /= OneKiloByte;
                suffix = "KB";
            }
            else if (size == 1)
            {
                suffix = " byte";
            }
            else
            {
                suffix = " bytes";
            }

            return $"{size}{suffix}";
        }
    }
}
