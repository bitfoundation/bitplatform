using Bit.Model.Contracts;
using System;

namespace Bit.Tests.Model.Dto
{
    public class SampleBaseDto : IDto
    {
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }
    }

    public class SampleInheritedDto : SampleBaseDto
    {
        public virtual string LastName { get; set; }
    }
}
