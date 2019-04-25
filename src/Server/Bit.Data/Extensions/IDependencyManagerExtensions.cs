using AutoMapper;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Model.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterDtoEntityMapper(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterGeneric(typeof(IDtoEntityMapper<,>).GetTypeInfo(), typeof(DefaultDtoEntityMapper<,>).GetTypeInfo(), DependencyLifeCycle.PerScopeInstance);
            return dependencyManager.RegisterAutoMapper();
        }

        public static IDependencyManager RegisterRepository(this IDependencyManager dependencyManager, TypeInfo repositoryType)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (repositoryType == null)
                throw new ArgumentNullException(nameof(repositoryType));

            IEnumerable<TypeInfo> interfaces = repositoryType.GetInterfaces().Select(p => p.GetTypeInfo());

            TypeInfo[] repositoryContracts = interfaces
                .Where(IsRepositoryContract)
                .Select(i => i.GetTypeInfo())
                .ToArray();

            if (!repositoryContracts.Any())
                throw new InvalidOperationException($"Type {repositoryType.FullName} has no repository contract");

            if (repositoryType.IsGenericType)
                dependencyManager.RegisterGeneric(repositoryContracts, repositoryType);
            else
                dependencyManager.Register(repositoryContracts, repositoryType);

            return dependencyManager;
        }

        private static bool IsRepositoryContract(this TypeInfo type)
        {
            if (type == null)
                throw new NullReferenceException();

            return type.GetInterfaces()
                .Select(p => p.GetTypeInfo()).Concat(new[] { type })
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().GetTypeInfo() == typeof(IRepository<>).GetTypeInfo());
        }
    }
}
