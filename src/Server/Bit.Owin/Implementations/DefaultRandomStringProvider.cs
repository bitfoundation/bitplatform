using Bit.Core.Contracts;
using System;
using System.Globalization;

namespace Bit.Owin.Implementations
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

            return result.Substring(length);
        }
    }
}