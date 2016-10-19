using System;
using Foundation.Core.Contracts;

namespace Foundation.Core.Implementations
{
    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        public virtual DateTimeOffset GetCurrentUtcDateTime()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}