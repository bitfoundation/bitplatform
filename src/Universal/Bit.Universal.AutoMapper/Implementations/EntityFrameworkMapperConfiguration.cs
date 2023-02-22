using AutoMapper;
using AutoMapper.Internal;
using Bit.Model.Contracts;
using Bit.Model.Implementations;
using System;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Bit.Data.EntityFramework.Implementations
{
    public class EntityFrameworkMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            if (mapperConfigExpression == null)
                throw new ArgumentNullException(nameof(mapperConfigExpression));

            bool MapperPropConfigurationCondition(PropertyMap p)
            {
                return (p.DestinationMember.GetCustomAttribute<ForeignKeyAttribute>() != null || p.DestinationMember.GetCustomAttribute<InversePropertyAttribute>() != null)
                       && !typeof(IEnumerable).IsAssignableFrom(p.DestinationMember.ReflectedType)
                       && DtoMetadataWorkspace.Current.IsDto(p.DestinationMember.ReflectedType!.GetTypeInfo());
            }

#if DotNetStandard2_0 || UAP10_0_17763
            mapperConfigExpression.ForAllPropertyMaps(
#else
            mapperConfigExpression.Internal().ForAllPropertyMaps(
#endif
                MapperPropConfigurationCondition, (p, member) =>
            {
                p.Ignored = true;
            });
        }
    }
}
