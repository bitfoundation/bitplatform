using Bit.Core.Contracts;
using System;

namespace Bit.Owin.Implementations
{
    public class DefaultRandomStringProvider : IRandomStringProvider
    {
        public virtual string GetRandomString(int length)
        {
            string result = "";

            while (result.Length < length)
            {
                result += Guid.NewGuid().ToString("N");
            }

            return result.Substring(length);
        }
    }
}