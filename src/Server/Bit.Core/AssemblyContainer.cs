using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bit.Core
{
    public class AssemblyContainer
    {
        public static AssemblyContainer Current { get; } = new AssemblyContainer();

        protected virtual List<Assembly> AppAssemblies { get; set; } = new List<Assembly>();

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Init()
        {
            AppAssemblies = new List<Assembly> { Assembly.GetCallingAssembly() };
        }

        public void AddAppAssemblies(params Assembly[] assemblies)
        {
            AppAssemblies.AddRange(assemblies);
        }

        public Assembly[] AssembliesWithDefaultAssemblies(params Assembly[] assembies)
        {
            if (assembies == null)
                throw new ArgumentNullException(nameof(assembies));

            return assembies
                .Union(AppAssemblies)
                .ToArray();
        }
    }
}
