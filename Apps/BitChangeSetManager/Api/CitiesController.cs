using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class CitiesController : DefaultReadOnlyDtoSetController<City, CityDto>
    {
        public CitiesController(IBitChangeSetManagerRepository<City> repository)
            : base(repository)
        {
        }
    }
}