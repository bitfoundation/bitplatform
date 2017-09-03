using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;

namespace BitChangeSetManager.Api
{
    public class ConstantsController : BitChangeSetManagerDtoSetController<ConstantDto, Constant, Guid>
    {
        public ConstantsController(IBitChangeSetManagerRepository<Constant> repository)
            : base(repository)
        {
        }

        protected override bool IsReadOnly()
        {
            return true;
        }
    }
}