using System;

namespace Bit.Owin.Contracts
{
    public interface IUrlStateProvider
    {
        dynamic GetState(Uri uri);
    }
}
