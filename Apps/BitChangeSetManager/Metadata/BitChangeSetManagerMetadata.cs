using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitChangeSetManager.Metadata
{
    public class BitChangeSetManagerMetadata : DefaultProjectMetadataBuilder
    {
        public const string InsertIsDeined = nameof(InsertIsDeined);
        public const string UpdateIsDeined = nameof(UpdateIsDeined);
        public const string DeleteIsDeined = nameof(DeleteIsDeined);

        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddProjectMetadata(new ProjectMetadata
            {
                ProjectName = "BitChangeSetManager",
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Yes", Title = "Yes" },
                            new EnvironmentCultureValue { Name = "No", Title = "No" },
                            new EnvironmentCultureValue { Name = "Ok", Title = "Ok" },
                            new EnvironmentCultureValue { Name = "Commands", Title = "Commands" },
                            new EnvironmentCultureValue { Name = nameof(InsertIsDeined), Title = "Insert is denied" },
                            new EnvironmentCultureValue { Name = nameof(UpdateIsDeined), Title = "Update is denied" },
                            new EnvironmentCultureValue { Name = nameof(DeleteIsDeined), Title = "Delete is denied" }
                        }
                    }
                }
            });

            return await base.BuildMetadata();
        }
    }
}