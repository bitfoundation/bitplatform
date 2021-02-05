using System.ComponentModel.DataAnnotations;
using Bit.Model.Contracts;

namespace BitChangeSetManager.Dto
{
    public class ChangeSetDescriptionTemplateDto : IDto
    {
        [Key]
        public virtual string Title { get; set; }

        public virtual string Content { get; set; }
    }
}