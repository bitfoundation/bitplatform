using AutoMapper;
using AutoMapper.Internal;
using Bit.Core.Contracts;
using Bit.Model.Contracts;
using System;

namespace Bit.Model.Implementations
{
    public class DefaultMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            if (mapperConfigExpression == null)
                throw new ArgumentNullException(nameof(mapperConfigExpression));

#if DotNetStandard2_0 || UAP10_0_17763
            mapperConfigExpression.ForAllMaps((typeMap, mapExp) =>
#else
            mapperConfigExpression.Internal().ForAllMaps((typeMap, mapExp) =>
#endif
            {
                mapExp.PreserveReferences();
            });
        }
    }
}
