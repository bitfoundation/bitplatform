using Foundation.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace Foundation.Test.Model.Dto
{
    public class TestCityDto : IDto
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }
    }
}