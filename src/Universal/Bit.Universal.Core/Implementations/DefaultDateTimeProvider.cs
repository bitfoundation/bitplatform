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

        private static IDateTimeProvider _current = default!;

        public static IDateTimeProvider Current
        {
            get
            {
                if (_current == null)
                    _current = new DefaultDateTimeProvider();
                return _current;
            }
            set => _current = value;
        }
    }
}