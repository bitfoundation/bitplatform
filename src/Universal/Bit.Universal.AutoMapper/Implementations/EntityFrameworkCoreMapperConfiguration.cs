using AutoMapper;
using AutoMapper.Internal;
using Bit.Model.Contracts;
using System;
using System.Collections;
using System.Linq;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EntityFrameworkCoreMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            if (mapperConfigExpression == null)
                throw new ArgumentNullException(nameof(mapperConfigExpression));

#if DotNetStandard2_0 || UAP10_0_17763
            mapperConfigExpression.ForAllPropertyMaps(p =>
#else
            mapperConfigExpression.Internal().ForAllPropertyMaps(p =>
#endif
            {
                var type = p.DestinationType;

                if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
                    type = type.HasElementType ? type.GetElementType()! : type.GetGenericArguments().ExtendedSingle($"Getting element type of {p.DestinationName}");

                return typeof(IDto).IsAssignableFrom(type);
            }, (p, conf) =>
            {
                conf.ExplicitExpansion();
            });
        }
    }
}
