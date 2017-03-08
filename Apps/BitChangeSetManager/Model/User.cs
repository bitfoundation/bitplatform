using Foundation.Model.Contracts;
using System;

namespace BitChangeSetManager.Model
{
    public class User : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string UserName { get; set; }

        public virtual string Password { get; set; }
    }
}