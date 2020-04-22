using System.Collections.Generic;
using Bit.Core.Models;

namespace Bit.Owin.Contracts.Metadata
{
    public class ProjectMetadata : ObjectMetadata
    {
        public virtual string ProjectName { get; set; } = default!;

        public virtual List<EnvironmentCulture> Messages { get; set; } = new List<EnvironmentCulture>();
    }

    public interface IProjectMetadataBuilder
    {
        IProjectMetadataBuilder AddProjectMetadata(ProjectMetadata metadata);
    }
}
