using AutoMapper;

namespace Bit.Model.Contracts
{
    public interface IMapperConfiguration
    {
        void Configure(IMapperConfigurationExpression mapperConfigExpression);
    }
}
