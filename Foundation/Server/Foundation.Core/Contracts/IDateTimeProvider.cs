using System;

namespace Foundation.Core.Contracts
{
    public interface IDateTimeProvider
    {
        DateTimeOffset GetCurrentUtcDateTime();
    }
}