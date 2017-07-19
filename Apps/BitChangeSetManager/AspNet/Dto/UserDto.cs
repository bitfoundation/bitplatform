using Bit.Model.Contracts;
using System;

namespace BitChangeSetManager.Dto
{
    public class UserDto : IDtoWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string UserName { get; set; }
    }
}