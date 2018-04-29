using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bit.Core;
using Bit.Core.Contracts;

namespace Bit.Owin.Implementations
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class AppModuleAttribute : Attribute
    {
        public AppModuleAttribute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Type = type;
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
            get
            {
                if (_current == null)
                    _current = new DefaultAppModulesProvider();

                return _current;
            }
            set => _current = value;
        }

        public virtual IEnumerable<IAppModule> GetAppModules()
        {
            if (_result == null)
            {
                string bitOwinAssemblyName = AssemblyContainer.Current.GetBitOwinAssembly().GetName().Name;

                object InstantiateAppModule(AppModuleAttribute depManagerAtt)
                {
                    return (_args != null && _args.Any()) ? Activator.CreateInstance(depManagerAtt.Type, _args) : Activator.CreateInstance(depManagerAtt.Type);
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