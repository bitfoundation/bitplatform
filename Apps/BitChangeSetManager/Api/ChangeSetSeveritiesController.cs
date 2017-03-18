using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class ChangeSetSeveritiesController : DefaultReadOnlyDtoSetController<ChangeSetSeverity, ChangeSetSeverityDto>
    {
        public ChangeSetSeveritiesController(IBitChangeSetManagerRepository<ChangeSetSeverity> repository)
            : base(repository)
        {
        }
    }
}