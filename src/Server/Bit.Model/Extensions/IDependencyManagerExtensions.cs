using AutoMapper;
using Bit.Model.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterAutoMapper(this IDependencyManager dependencyManager)
        {
            TypeInfo[] allTypes = AssemblyContainer.Current.AssembliesWithDefaultAssemblies()
                .SelectMany(asm => asm.GetLoadableExportedTypes())
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToArray();

            TypeInfo profileTypeInfo = typeof(Profile).GetTypeInfo();

            TypeInfo[] profiles = allTypes
                .Where(t => profileTypeInfo.IsAssignableFrom(t))
                .ToArray();

            IConfigurationProvider RegisterMapperConfiguration(IDependencyResolver resolver)
            {
                IEnumerable<IMapperConfiguration> configs = resolver.Resolve<IEnumerable<IMapperConfiguration>>();

                void ConfigureMapper(IMapperConfigurationExpression cfg)
                {
                    configs.ToList().ForEach(c => c.Configure(cfg));

                    foreach (TypeInfo profileType in profiles)
                    {
                        cfg.AddProfile((Profile)resolver.Resolve(profileType));
                    }
                }

                MapperConfiguration mapperConfig = new MapperConfiguration(ConfigureMapper);

                return mapperConfig;
            }

            IMapper RegisterMapper(IDependencyResolver resolver)
            {
                IConfigurationProvider mapperConfig = resolver.Resolve<IConfigurationProvider>();

                IMapper mapper = mapperConfig.CreateMapper(serviceType => resolver.Resolve(serviceType.GetTypeInfo()));

                return mapper;
            }

            // See https://github.com/AutoMapper/AutoMapper.Extensions.Microsoft.DependencyInjection
            dependencyManager.RegisterUsing(RegisterMapperConfiguration, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterUsing(RegisterMapper, lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExciting: false);

            TypeInfo[] openTypes = new[]
            {
                typeof(IValueResolver<,,>).GetTypeInfo(),
                typeof(IMemberValueResolver<,,,>).GetTypeInfo(),
                typeof(ITypeConverter<,>).GetTypeInfo(),
                typeof(IValueConverter<,>).GetTypeInfo(),
                typeof(IMappingAction<,>).GetTypeInfo()
            };

            foreach (TypeInfo type in openTypes.SelectMany(openType => allTypes.Where(t => t.ImplementsGenericInterface(openType))))
            {
                dependencyManager.Register(type, type, lifeCycle: DependencyLifeCycle.Transient, overwriteExciting: false);
            }

            foreach (TypeInfo profileType in profiles)
            {
                dependencyManager.Register(profileType, profileType, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            }

            return dependencyManager;
        }

        static bool ImplementsGenericInterface(this TypeInfo type, TypeInfo interfaceType)
            => type.IsGenericType(interfaceType) || type.GetTypeInfo().ImplementedInterfaces.Select(@interface => @interface.GetTypeInfo()).Any(@interface => @interface.IsGenericType(interfaceType));

        static bool IsGenericType(this TypeInfo type, TypeInfo genericType)
            => type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == genericType;

        public static IDependencyManager RegisterMapperConfiguration<TMapperConfiguration>(this IDependencyManager dependencyManager)
            where TMapperConfiguration : class, IMapperConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IMapperConfiguration, TMapperConfiguration>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }
    }
}
