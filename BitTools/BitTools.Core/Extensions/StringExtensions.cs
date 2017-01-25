using System.Linq;

namespace System
{
    public static class StringExtensions
    {
        public static string AsCamelCase(this string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return string.Join(string.Empty, value.ToCharArray().Select((c, i) => { if (i == 0) return char.ToLowerInvariant(c); else return c; }));
        }
    }
}