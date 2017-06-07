using System;
using Bit.Core.Contracts;

namespace Bit.Core.Implementations
{
    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        public virtual DateTimeOffset GetCurrentUtcDateTime()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}