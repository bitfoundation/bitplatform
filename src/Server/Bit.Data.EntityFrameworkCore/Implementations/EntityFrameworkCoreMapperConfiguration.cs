using AutoMapper;
using Bit.Model.Contracts;
using System.Collections;
using System.Linq;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EntityFrameworkCoreMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            mapperConfigExpression.ForAllPropertyMaps(p =>
            {
                var type = p.DestinationType;

                if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
                    type = type.HasElementType ? type.GetElementType() : type.GetGenericArguments().ExtendedSingle($"Getting element type of {p.DestinationName}");

                return typeof(IDto).IsAssignableFrom(type);
            }, (p, conf) =>
            {
                conf.ExplicitExpansion();
            });
        }
    }
}
