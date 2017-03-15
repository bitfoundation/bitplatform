using Foundation.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Foundation.Api.Contracts.Metadata
{
    public class AppMetadata
    {
        public virtual List<EnvironmentCulture> Messages { get; set; } = new List<EnvironmentCulture>();

        public virtual List<ViewMetadata> Views { get; set; } = new List<ViewMetadata>();

        public virtual List<DtoMetadata> Dtos { get; set; } = new List<DtoMetadata>();

        public virtual List<ProjectMetadata> Projects { get; set; } = new List<ProjectMetadata>();
    }

    public interface IAppMetadataProvider
    {
        Task<AppMetadata> GetAppMetadata();
    }
}
