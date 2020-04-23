using AutoMapper;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System.Linq;
using Bit.Model.Contracts;

namespace BitChangeSetManager.DataAccess.Implementations
{
    public class BitChangeSetManagerMapperConfiguration : IMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            int customersCount = 0;

            mapperConfigExpression.CreateMap<ChangeSet, ChangeSetDto>()
                .ForMember(changeSetMember => changeSetMember.IsDeliveredToAll, config => config.MapFrom(changeSet => changeSet.Deliveries.Count() == customersCount))
                .ReverseMap();

            mapperConfigExpression.CreateMap<Province, ProvinceDto>()
                .ForMember(province => province.CitiesCount, config => config.MapFrom(province => province.Cities.LongCount()))
                .ReverseMap();

            mapperConfigExpression.CreateMap<ChangeSetDeliveryRequirement, ChangeSetDeliveryRequirementDto>()
                .ReverseMap();

            mapperConfigExpression.CreateMap<ChangeSetImage, ChangeSetImagetDto>()
                .ReverseMap();

            mapperConfigExpression.CreateMap<ChangeSetSeverity, ChangeSetSeverityDto>()
                .ReverseMap();

            mapperConfigExpression.CreateMap<City, CityDto>()
                .ReverseMap();

            mapperConfigExpression.CreateMap<Constant, ConstantDto>()
                .ReverseMap();

            mapperConfigExpression.CreateMap<Customer, CustomerDto>()
                .ReverseMap();

            mapperConfigExpression.CreateMap<Delivery, DeliveryDto>()
                .ReverseMap();

            mapperConfigExpression.CreateMap<User, UserDto>()
                .ReverseMap();
        }
    }
}
