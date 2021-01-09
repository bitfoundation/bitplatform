using AutoMapper;
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

            mapperConfigExpression.ForAllMaps((typeMap, mapExp) =>
            {
                mapExp.PreserveReferences();
            });
        }
    }
}
