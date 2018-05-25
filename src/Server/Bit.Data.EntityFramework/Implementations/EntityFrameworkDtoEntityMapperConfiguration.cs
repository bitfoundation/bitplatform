using AutoMapper;
using Bit.Model.Contracts;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Bit.Data.EntityFramework.Implementations
{
    public class EntityFrameworkDtoEntityMapperConfiguration : IDtoEntityMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            bool MapperPropConfigurationCondition(PropertyMap p)
            {
                return (p.DestinationProperty.GetCustomAttribute<ForeignKeyAttribute>() != null || p.DestinationProperty.GetCustomAttribute<InversePropertyAttribute>() != null)
                       && !typeof(IEnumerable).IsAssignableFrom(p.DestinationProperty.ReflectedType)
                       && typeof(IDto).IsAssignableFrom(p.DestinationProperty.ReflectedType);
            }

            mapperConfigExpression.ForAllPropertyMaps(MapperPropConfigurationCondition, (p, member) =>
            {
                p.Ignored = true;
            });
        }
    }
}
