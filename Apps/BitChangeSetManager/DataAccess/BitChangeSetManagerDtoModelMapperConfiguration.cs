using AutoMapper;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using Foundation.Model.Contracts;
using System.Linq;

namespace BitChangeSetManager.DataAccess
{
    public class BitChangeSetManagerDtoModelMapperConfiguration : IDtoModelMapperConfiguration
    {
        public virtual void Configure(IMapperConfigurationExpression mapperConfigExpression)
        {
            IQueryable<Customer> customersQuery = null;

            mapperConfigExpression.CreateMap<ChangeSet, ChangeSetDto>()
                .ForMember(changeSetMember => changeSetMember.IsDeliveredToAll, config => config.MapFrom(changeSet => customersQuery.All(c => c.DeliveredChangeSets.Any(deliveredChangeSet => changeSet.Id == deliveredChangeSet.Id))));
        }
    }
}
