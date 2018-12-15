using AutoMapper;
using Bit.Model.Contracts;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Bit.Data.EntityFramework.Implementations
{
    public class EntityFrameworkMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            bool MapperPropConfigurationCondition(PropertyMap p)
            {
                return (p.DestinationMember.GetCustomAttribute<ForeignKeyAttribute>() != null || p.DestinationMember.GetCustomAttribute<InversePropertyAttribute>() != null)
                       && !typeof(IEnumerable).IsAssignableFrom(p.DestinationMember.ReflectedType)
                       && typeof(IDto).IsAssignableFrom(p.DestinationMember.ReflectedType);
            }

            mapperConfigExpression.ForAllPropertyMaps(MapperPropConfigurationCondition, (p, member) =>
            {
                p.Ignored = true;
            });
        }
    }
}
