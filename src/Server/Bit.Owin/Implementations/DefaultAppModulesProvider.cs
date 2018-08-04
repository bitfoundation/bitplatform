using Bit.Core;
using Bit.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bit.Owin.Implementations
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class AppModuleAttribute : Attribute
    {
        public AppModuleAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public Type Type { get; }
    }

    /// <summary>
    /// By default it finds <see cref="AppModuleAttribute" /> attributes in assemblies, then it returns instances of <see cref="IAppModule"/> provided by those attributes. You can change its behavior by setting its <see cref="Current"/> to another implementation of <see cref="IAppModulesProvider"/>
    /// </summary>
    public class DefaultAppModulesProvider : IAppModulesProvider
    {
        private static IAppModulesProvider _current;

        private IAppModule[] _result;
        private readonly object[] _args;

        protected DefaultAppModulesProvider()
        {
        }

        public DefaultAppModulesProvider(params object[] args)
        {
            _args = args;
        }

        /// <summary>
        /// Current <see cref="IAppModulesProvider"/> implementation used by bit
        /// </summary>
        public static IAppModulesProvider Current
        {
            get => _current ?? (_current = new DefaultAppModulesProvider());
            set => _current = value;
        }

        public virtual IEnumerable<IAppModule> GetAppModules()
        {
            if (_result == null)
            {
                string bitOwinAssemblyName = AssemblyContainer.Current.GetBitOwinAssembly().GetName().Name;

                object InstantiateAppModule(AppModuleAttribute depManagerAtt)
                {
                    return (_args?.Length > 0) ? Activator.CreateInstance(depManagerAtt.Type, _args) : Activator.CreateInstance(depManagerAtt.Type);
                }

                _result = AssemblyContainer.Current.AssembliesWithDefaultAssemblies()
                    .Where(asm => asm.GetReferencedAssemblies().Any(asmRef => asmRef.Name == bitOwinAssemblyName))
                    .SelectMany(asm => asm.GetCustomAttributes<AppModuleAttribute>())
                    .Select(InstantiateAppModule)
                    .Cast<IAppModule>()
                    .ToArray();
            }

            return _result;
        }
    }
}
