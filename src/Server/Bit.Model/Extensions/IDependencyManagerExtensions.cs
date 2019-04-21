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
            IConfigurationProvider RegisterMapperConfiguration(IDependencyResolver resolver)
            {
                IEnumerable<IMapperConfiguration> configs = resolver.Resolve<IEnumerable<IMapperConfiguration>>();

                void ConfigureMapper(IMapperConfigurationExpression cfg)
                {
                    configs.ToList().ForEach(c => c.Configure(cfg));
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

            Type[] openTypes = new[]
            {
                typeof(IValueResolver<,,>),
                typeof(IMemberValueResolver<,,,>),
                typeof(ITypeConverter<,>),
                typeof(IValueConverter<,>),
                typeof(IMappingAction<,>)
            };

            foreach (TypeInfo type in openTypes.SelectMany(openType => AssemblyContainer.Current.AssembliesWithDefaultAssemblies().SelectMany(asm => asm.GetLoadableExportedTypes())
                .Where(t => t.IsClass
                    && !t.IsAbstract
                    && t.AsType().ImplementsGenericInterface(openType))))
            {
                dependencyManager.Register(type, type, lifeCycle: DependencyLifeCycle.Transient, overwriteExciting: false);
            }

            return dependencyManager;
        }

        static bool ImplementsGenericInterface(this Type type, Type interfaceType)
            => type.IsGenericType(interfaceType) || type.GetTypeInfo().ImplementedInterfaces.Any(@interface => @interface.IsGenericType(interfaceType));

        static bool IsGenericType(this Type type, Type genericType)
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
