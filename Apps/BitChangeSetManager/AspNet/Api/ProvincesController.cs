using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;

namespace BitChangeSetManager.Api
{
    public class ProvincesController : BitChangeSetManagerDtoSetController<ProvinceDto, Province, Guid>
    {

        protected override bool IsReadOnly()
        {
            return true;
        }
    }
}