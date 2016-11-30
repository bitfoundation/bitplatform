using Foundation.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace Foundation.Test.Model.Dto
{
    public class TestCustomerDto : IDtoWithDefaultGuidKey
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual Guid CityId { get; set; }

        public virtual string CityName { get; set; }
    }
}
