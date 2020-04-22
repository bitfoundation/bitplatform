using System;
using System.Collections.Generic;
using System.Reflection;
using Bit.WebApi.Contracts;

namespace Bit.WebApi.Implementations
{
    public class DefaultWebApiAssembliesProvider : IApiAssembliesProvider
    {
        protected DefaultWebApiAssembliesProvider()
        {

        }

        private readonly IEnumerable<Assembly> _assemblies = default!;

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
