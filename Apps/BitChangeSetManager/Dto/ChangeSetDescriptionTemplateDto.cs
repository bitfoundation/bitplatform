using Foundation.Model.Contracts;
using System.ComponentModel.DataAnnotations;

namespace BitChangeSetManager.Dto
{
    public class ChangeSetDescriptionTemplateDto : IDto
    {
        [Key]
        public virtual string Title { get; set; }

        public virtual string Content { get; set; }
    }
}