using System;

namespace Foundation.Api.Contracts
{
    public interface ITimeZoneManager
    {
        DateTimeOffset MapFromServerToClient(DateTimeOffset date);

        DateTimeOffset MapFromClientToServer(DateTimeOffset date);
    }
}
