using Bit.Model.Contracts;
using System;

namespace BitChangeSetManager.Model
{
    public enum BitCulture
    {
        FaIr, EnUs
    }

    public class User : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string UserName { get; set; }

        public virtual string Password { get; set; }

        public virtual BitCulture Culture { get; set; }
    }
}