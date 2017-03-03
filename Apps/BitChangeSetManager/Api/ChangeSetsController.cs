using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using Foundation.Api.ApiControllers;

namespace BitChangeSetManager.Api
{
    public class ChangeSetsController : DefaultDtoSetController<ChangeSet, ChangeSetDto>
    {
        public ChangeSetsController(IBitChangeSetManagerRepository<ChangeSet> changeSetsRepository)
            : base(changeSetsRepository)
        {

        }
    }
}