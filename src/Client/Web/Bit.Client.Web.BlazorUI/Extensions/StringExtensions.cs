namespace Bit.Client.Web.BlazorUI
{
    public static class StringExtensions
    {
        public static bool HasValue(this string? value, bool ignoreWhiteSpace = true)
        {
            return ignoreWhiteSpace
                ? string.IsNullOrWhiteSpace(value) is false
                : string.IsNullOrEmpty(value) is false;
        }

        public static bool HasNoValue(this string? value, bool ignoreWhiteSpace = true)
        {
            return ignoreWhiteSpace
                ? string.IsNullOrWhiteSpace(value)
                : string.IsNullOrEmpty(value);
        }

        public static string? RemoveSuffix(this string? value, string? suffix)
        {
            if (value.HasNoValue() || suffix.HasNoValue()) return value;

            if (value!.EndsWith(suffix!, System.StringComparison.Ordinal))
            {
                return value.Remove(value.Length - suffix!.Length);
            }

            return value;
        }
    }
}
