using Bit.Core.Contracts;
using System;

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
