using AutoMapper;
using Bit.Data;
using Bit.Model.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterDtoModelMapper(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterGeneric(typeof(IDtoModelMapper<,>).GetTypeInfo(), typeof(DefaultDtoModelMapper<,>).GetTypeInfo(), DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterUsing(() =>
            {
                IEnumerable<IDtoModelMapperConfiguration> configs = dependencyManager.Resolve<IEnumerable<IDtoModelMapperConfiguration>>();

                MapperConfiguration mapperConfig = new MapperConfiguration(cfg =>
                {
                    configs.ToList().ForEach(c => c.Configure(cfg));
                });

                IMapper mapper = mapperConfig.CreateMapper();

                return mapper;

            }, lifeCycle: DependencyLifeCycle.SingleInstance);

            return dependencyManager;
        }

        public static IDependencyManager RegisterDtoModelMapperConfiguration<TDtoModelMapperConfiguration>(this IDependencyManager dependencyManager)
            where TDtoModelMapperConfiguration : class, IDtoModelMapperConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDtoModelMapperConfiguration, TDtoModelMapperConfiguration>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }
    }
}
