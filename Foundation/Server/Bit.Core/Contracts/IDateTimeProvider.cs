using System;

namespace Bit.Core.Contracts
{
    public interface IDateTimeProvider
    {
        DateTimeOffset GetCurrentUtcDateTime();
    }
}