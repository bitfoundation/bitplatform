using AutoMapper;

namespace Foundation.Model.Contracts
{
    public interface IDtoModelMapperConfiguration
    {
        void Configure(IMapperConfigurationExpression mapperConfigExpression);
    }
}
