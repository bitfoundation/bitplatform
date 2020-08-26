using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bit.Core
{
    /// <summary>
    /// Contains assembly parts of your app. These assemblies are used to find web api controllers, signalr hubs etc.
    /// </summary>
    public class AssemblyContainer
    {
        private static AssemblyContainer _current = default!;

        public static AssemblyContainer Current
        {
            get
            {
                if (_current == null)
                    _current = new AssemblyContainer();

                return _current;
            }
            set => _current = value;
        }

        protected virtual HashSet<Assembly> AppAssemblies { get; set; } = new HashSet<Assembly>();

        /// <summary>
        /// Adds calling assembly (Assembly you're calling this method in there) as main app's assembly
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public virtual void Init()
        {
            AppAssemblies = new HashSet<Assembly> 
            {
#if !UWP
                Assembly.GetCallingAssembly()
#else
                Assembly.GetEntryAssembly() // UWP .NET Native
#endif
            };
        }

        /// <summary>
        /// Add another assemblies of your app if you've multiple projects
        /// </summary>
        public virtual void AddAppAssemblies(params Assembly[] assemblies)
        {
            foreach (Assembly asm in assemblies)
            {
                AppAssemblies.Add(asm);
            }
        }

        public virtual Assembly[] AssembliesWithDefaultAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            return assemblies
                .Union(AppAssemblies)
                .ToArray();
        }
    }
}
