using AutoMapper;
using Bit.Model.Contracts;
using Bit.Model.Implementations;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EntityFrameworkCoreMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            if (mapperConfigExpression == null)
                throw new ArgumentNullException(nameof(mapperConfigExpression));

            mapperConfigExpression.ForAllPropertyMaps(p =>
            {
                var type = p.DestinationType;

                if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
                    type = type.HasElementType ? type.GetElementType()! : type.GetGenericArguments().ExtendedSingle($"Getting element type of {p.DestinationName}");

                return DtoMetadataWorkspace.Current.IsDto(type.GetTypeInfo());
            }, (p, conf) =>
            {
                conf.ExplicitExpansion();
            });
        }
    }
}
