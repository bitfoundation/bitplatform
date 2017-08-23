using AutoMapper;

namespace Bit.Model.Contracts
{
    public interface IDtoEntityMapperConfiguration
    {
        void Configure(IMapperConfigurationExpression mapperConfigExpression);
    }
}
