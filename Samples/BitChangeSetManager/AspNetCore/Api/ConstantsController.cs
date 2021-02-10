using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;

namespace BitChangeSetManager.Api
{
    public class ConstantsController : BitChangeSetManagerDtoSetController<ConstantDto, Constant, Guid>
    {
        protected override bool IsReadOnly()
        {
            return true;
        }
    }
}