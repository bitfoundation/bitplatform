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
                .ForMember(d => d.IsDeliveredToAll, config => config.MapFrom(e => e.Deliveries.Count() == customersCount))
                .ReverseMap()
                .ForPath(e => e.City.Name, opt => opt.Ignore())
                .ForPath(e => e.Province.Name, opt => opt.Ignore())
                .ForPath(e => e.DeliveryRequirement.Title, opt => opt.Ignore())
                .ForPath(e => e.Severity.Title, opt => opt.Ignore());

            mapperConfigExpression.CreateMap<Province, ProvinceDto>()
                .ForMember(d => d.CitiesCount, config => config.MapFrom(e => e.Cities.LongCount()))
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
                .ReverseMap()
                .ForPath(e => e.ChangeSet.Title, opt => opt.Ignore());

            mapperConfigExpression.CreateMap<User, UserDto>()
                .ReverseMap();
        }
    }
}
