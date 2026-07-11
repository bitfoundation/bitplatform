using System.Text;

namespace Foundation;

public static partial class NSDataExtensions
{
    extension(NSData data)
    {
        public string? ToHexString()
        {
            var bytes = data.ToArray();

            if (bytes == null)
                return null;

            StringBuilder sb = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
                sb.AppendFormat("{0:x2}", b);

            return sb.ToString().ToUpperInvariant();
        }
    }
}
