using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bit.Core;
using Bit.Core.Contracts;

namespace Bit.Owin.Implementations
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DependenciesManagerAttribute : Attribute
    {
        public DependenciesManagerAttribute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Type = type;
        }

        public Type Type { get; }
    }

    /// <summary>
    /// By default it finds <see cref="DependenciesManagerAttribute" /> attributes in assemblies, then it returns instances of <see cref="IDependenciesManager"/> provided by those attributes. You can change its behavior by setting its <see cref="Current"/> to another implementation of <see cref="IDependenciesManagerProvider"/>
    /// </summary>
    public class DefaultDependenciesManagerProvider : IDependenciesManagerProvider
    {
        private static IDependenciesManagerProvider _current;

        private IDependenciesManager[] _result;
        private readonly object[] _args;

#if DEBUG
        protected DefaultDependenciesManagerProvider()
        {
        }
#endif

        public DefaultDependenciesManagerProvider(params object[] args)
        {
            _args = args;
        }

        /// <summary>
        /// Current <see cref="IDependenciesManagerProvider"/> implementation used by bit
        /// </summary>
        public static IDependenciesManagerProvider Current
        {
            get
            {
                if (_current == null)
                    _current = new DefaultDependenciesManagerProvider();

                return _current;
            }
            set => _current = value;
        }

        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            if (_result == null)
            {
                string bitOwinAssemblyName = AssemblyContainer.Current.GetBitOwinAssembly().GetName().Name;

                _result = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(asm => !asm.IsDynamic && !asm.GlobalAssemblyCache)
                    .Where(asm => asm.GetReferencedAssemblies().Any(asmRef => asmRef.Name == bitOwinAssemblyName))
                    .SelectMany(asm => asm.GetCustomAttributes<DependenciesManagerAttribute>())
                    .Select(depManagerAtt => (_args != null && _args.Any()) ? Activator.CreateInstance(depManagerAtt.Type, _args) : Activator.CreateInstance(depManagerAtt.Type))
                    .Cast<IDependenciesManager>()
                    .ToArray();
            }

            return _result;
        }
    }
}