using System;
using System.ComponentModel.DataAnnotations;
using Bit.Model.Contracts;

namespace Bit.Tests.Model.Dto
{
    public class TestCityDto : IDto
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }
    }
}