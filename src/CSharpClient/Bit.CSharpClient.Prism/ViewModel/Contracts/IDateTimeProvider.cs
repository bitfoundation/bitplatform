using System;

namespace Bit.ViewModel.Contracts
{
    public interface IDateTimeProvider
    {
        DateTimeOffset GetCurrentUtcDateTime();
    }
}
