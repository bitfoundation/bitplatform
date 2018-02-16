using Bit.ViewModel.Contracts;
using System;

namespace Bit.ViewModel.Implementations
{
    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        public virtual DateTimeOffset GetCurrentUtcDateTime()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}
