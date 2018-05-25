using AutoMapper;
using Bit.Model.Contracts;

namespace Bit.Model.Implementations
{
    public class DefaultDtoEntityMapperConfiguration : IDtoEntityMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            mapperConfigExpression.ValidateInlineMaps = false;

            mapperConfigExpression.CreateMissingTypeMaps = true;
        }
    }
}
