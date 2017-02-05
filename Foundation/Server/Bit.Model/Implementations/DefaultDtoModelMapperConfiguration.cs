using AutoMapper;
using Foundation.Model.Contracts;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Foundation.Model.Implementations
{
    public class DefaultDtoModelMapperConfiguration : IDtoModelMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            mapperConfigExpression.CreateMissingTypeMaps = true;

            mapperConfigExpression.ForAllPropertyMaps(
                (p) =>
                {
                    return p.DestinationProperty.GetCustomAttribute<ForeignKeyAttribute>() != null
                            && p.DestinationProperty.GetCustomAttribute<InversePropertyAttribute>() != null
                            && !typeof(IEnumerable).IsAssignableFrom(p.DestinationProperty.ReflectedType);
                },
                (pConfig, member) =>
                {
                    pConfig.Ignored = true;
                });
        }
    }
}
