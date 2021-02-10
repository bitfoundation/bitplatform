using Bit.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitChangeSetManager.Model
{
    public enum BitCulture
    {
        FaIr, EnUs
    }

    public class User : IEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual Guid Id { get; set; }

        public virtual string UserName { get; set; }

        public virtual string Password { get; set; }

        public virtual BitCulture Culture { get; set; }

        public byte[] AvatarImage { get; set; }
    }
}