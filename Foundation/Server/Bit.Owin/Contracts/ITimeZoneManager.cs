using System;

namespace Bit.Owin.Contracts
{
    public interface ITimeZoneManager
    {
        DateTimeOffset MapFromServerToClient(DateTimeOffset date);

        DateTimeOffset MapFromClientToServer(DateTimeOffset date);
    }
}
