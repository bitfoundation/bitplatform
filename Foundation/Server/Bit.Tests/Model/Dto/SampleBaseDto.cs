using Foundation.Model.Contracts;
using System;

namespace Foundation.Test.Model.Dto
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
