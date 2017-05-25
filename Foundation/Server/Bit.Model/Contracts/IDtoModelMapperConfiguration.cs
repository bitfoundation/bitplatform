using AutoMapper;

namespace Bit.Model.Contracts
{
    public interface IDtoModelMapperConfiguration
    {
        void Configure(IMapperConfigurationExpression mapperConfigExpression);
    }
}
