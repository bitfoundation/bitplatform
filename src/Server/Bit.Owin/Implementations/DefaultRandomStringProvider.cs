using System;
using Bit.Core.Contracts;

namespace Bit.Owin.Implementations
{
    public class DefaultRandomStringProvider : IRandomStringProvider
    {
        public virtual string GetRandomNonSecureString(int length)
        {
            if (length > 32)
                throw new NotSupportedException($"Length greater than 32 is not supported. Provided length: {length}");

            return Guid.NewGuid().ToString("N").Substring(length);
        }
    }
}