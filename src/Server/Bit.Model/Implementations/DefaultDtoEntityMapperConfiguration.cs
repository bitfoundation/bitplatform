using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using AutoMapper;
using Bit.Model.Contracts;

namespace Bit.Model.Implementations
{
    public class DefaultDtoEntityMapperConfiguration : IDtoEntityMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            mapperConfigExpression.CreateMissingTypeMaps = true;

            mapperConfigExpression.ForAllPropertyMaps(p => p.DestinationProperty.GetCustomAttribute<ForeignKeyAttribute>() != null
                       && p.DestinationProperty.GetCustomAttribute<InversePropertyAttribute>() != null
                       && !typeof(IEnumerable).IsAssignableFrom(p.DestinationProperty.ReflectedType)
                       && typeof(IDto).IsAssignableFrom(p.DestinationProperty.ReflectedType),
                (pConfig, member) =>
                {
                    pConfig.Ignored = true;
                });
        }
    }
}
