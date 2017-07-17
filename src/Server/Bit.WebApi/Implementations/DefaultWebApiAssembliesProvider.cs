using System;
using System.Collections.Generic;
using System.Reflection;
using Bit.WebApi.Contracts;

namespace Bit.WebApi.Implementations
{
    public class DefaultWebApiAssembliesProvider : IApiAssembliesProvider
    {
#if DEBUG
        protected DefaultWebApiAssembliesProvider()
        {
        }
#endif

        private readonly IEnumerable<Assembly> _assemblies;

        public DefaultWebApiAssembliesProvider(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            _assemblies = assemblies;
        }

        public virtual IEnumerable<Assembly> GetApiAssemblies()
        {
            return _assemblies;
        }
    }
}
