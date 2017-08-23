using AutoMapper;
using Bit.Model.Contracts;
using Bit.Tests.Model.DomainModels;
using Bit.Tests.Model.Dto;

namespace Bit.Tests.Model.Implementations
{
    public class TestDtoEntityMapperConfiguration : IDtoEntityMapperConfiguration
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
