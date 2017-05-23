using System;
using System.ComponentModel.DataAnnotations;
using Foundation.Model.Contracts;

namespace Foundation.Model.DomainModels
{
    [Serializable]
    public class JobInfoDto : IDto
    {
        [Key]
        public virtual string Id { get; set; }

        public virtual string State { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
