using Foundation.Model.Contracts;
using AutoMapper;
using Foundation.Test.Model.DomainModels;
using Foundation.Test.Model.Dto;

namespace Foundation.Test.Model.Implementations
{
    public class TestDtoModelMapperConfiguration : IDtoModelMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            mapperConfigExpression.CreateMap<TestCustomerDto, TestCustomer>()
                .AfterMap((dto, model) =>
                {
                    model.City = new TestCity { Id = dto.CityId, Name = dto.CityName };
                });

            mapperConfigExpression.CreateMap<TestCustomer, TestCustomerDto>();
        }
    }
}
