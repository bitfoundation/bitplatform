using System.Collections.Generic;
using System.Reflection;

namespace Bit.Api.Contracts.Project
{
    public interface IApiAssembliesProvider
    {
        IEnumerable<Assembly> GetApiAssemblies();
    }
}