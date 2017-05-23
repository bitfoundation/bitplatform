using System.Collections.Generic;
using System.Reflection;

namespace Foundation.Api.Contracts.Project
{
    public interface IApiAssembliesProvider
    {
        IEnumerable<Assembly> GetApiAssemblies();
    }
}