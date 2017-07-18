using BitChangeSetManager.Dto;
using System.IO;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.OData.ODataControllers;

namespace BitChangeSetManager.Api
{
    public class ChangeSetDescriptionTemplateController : DtoController<ChangeSetDescriptionTemplateDto>
    {
        public IPathProvider PathProvider { get; set; }

        public IContentFormatter Formatter { get; set; }

        [Function]
        public async Task<ChangeSetDescriptionTemplateDto[]> GetAllTemplates()
        {
            string templatesFilePath = PathProvider.MapPath("App_Data/templates.json");

            using (StreamReader reader = File.OpenText(templatesFilePath))
            {
                return Formatter.DeSerialize<ChangeSetDescriptionTemplateDto[]>(await reader.ReadToEndAsync());
            }
        }
    }
}