using Bit.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.Model.Dtos
{
    public class JobInfoDto : IDto
    {
        [Key]
        public virtual string Id { get; set; } = default!;

        public virtual string State { get; set; } = default!;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
