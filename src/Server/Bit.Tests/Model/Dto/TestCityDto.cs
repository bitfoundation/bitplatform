using Bit.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.Tests.Model.Dto
{
    public class TestCityDto : IDto
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }
    }
}
