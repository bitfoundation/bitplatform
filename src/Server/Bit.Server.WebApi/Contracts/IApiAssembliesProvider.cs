using System.Collections.Generic;
using System.Reflection;

namespace Bit.WebApi.Contracts
{
    public interface IApiAssembliesProvider
    {
        IEnumerable<Assembly> GetApiAssemblies();
    }
}