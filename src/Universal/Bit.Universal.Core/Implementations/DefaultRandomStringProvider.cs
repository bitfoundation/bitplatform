using Bit.Core.Contracts;
using System;
using System.Globalization;

namespace Bit.Core.Implementations
{
    public class DefaultRandomStringProvider : IRandomStringProvider
    {
        public virtual string GetRandomString(int length)
        {
            string result = "";

            while (result.Length < length)
            {
                result += Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            }

            return result.Substring(0, length);
        }
    }
}
