using Bit.Model.Contracts;
using System;

namespace BitChangeSetManager.Dto
{
    public class ConstantDto : IDto
    {
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string Title { get; set; }
    }
}