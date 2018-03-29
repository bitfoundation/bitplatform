using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Bit.Model.Contracts;

namespace Bit.Tests.Model.Dto
{
    public class TestCustomerDto : IDto, ISyncableDto
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual Guid CityId { get; set; }

        public virtual string CityName { get; set; }

        public virtual long Version { get; set; }

        public virtual bool IsArchived { get; set; }
    }
}
