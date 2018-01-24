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

            dependencyManager.RegisterGeneric(typeof(IDtoEntityMapper<,>).GetTypeInfo(), typeof(DefaultDtoEntityMapper<,>).GetTypeInfo(), DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterUsing((depManager) =>
            {
                IEnumerable<IDtoEntityMapperConfiguration> configs = depManager.Resolve<IEnumerable<IDtoEntityMapperConfiguration>>();

                MapperConfiguration mapperConfig = new MapperConfiguration(cfg =>
                {
                    configs.ToList().ForEach(c => c.Configure(cfg));
                });

                IMapper mapper = mapperConfig.CreateMapper();

                return mapper;

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterDtoEntityMapperConfiguration<TDtoEntityMapperConfiguration>(this IDependencyManager dependencyManager)
            where TDtoEntityMapperConfiguration : class, IDtoEntityMapperConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDtoEntityMapperConfiguration, TDtoEntityMapperConfiguration>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterRepository(this IDependencyManager dependencyManager, TypeInfo repositoryType)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (repositoryType == null)
                throw new ArgumentNullException(nameof(repositoryType));

            HashSet<TypeInfo> interaces = new HashSet<TypeInfo>();
            GetAllInterfaces(repositoryType, ref interaces);

            TypeInfo[] repositoryContracts = interaces
                .Where(i => i.IsRepositoryContract())
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

            HashSet<TypeInfo> allInterfaces = new HashSet<TypeInfo>
            {
                type
            };

            GetAllInterfaces(type, ref allInterfaces);

            return allInterfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition().GetTypeInfo() == typeof(IRepository<>).GetTypeInfo());
        }

        private static void GetAllInterfaces(TypeInfo type, ref HashSet<TypeInfo> result)
        {
            foreach (TypeInfo i in type.GetInterfaces())
            {
                result.Add(i);

                GetAllInterfaces(i, ref result);
            }
        }
    }
}
