using AutoMapper;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System.Linq;
using Bit.Model.Contracts;

namespace BitChangeSetManager.DataAccess
{
    public class BitChangeSetManagerDtoEntityMapperConfiguration : IDtoEntityMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            int customersCount = 0;

            mapperConfigExpression.CreateMap<ChangeSet, ChangeSetDto>()
                .ForMember(changeSetMember => changeSetMember.IsDeliveredToAll, config => config.MapFrom(changeSet => changeSet.Deliveries.Count() == customersCount));

            mapperConfigExpression.CreateMap<Province, ProvinceDto>()
                .ForMember(province => province.CitiesCount, config => config.MapFrom(province => province.Cities.LongCount()));
        }
    }
}
