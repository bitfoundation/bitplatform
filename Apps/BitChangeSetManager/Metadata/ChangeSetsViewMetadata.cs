using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitChangeSetManager.Metadata
{
    public class ChangeSetsViewMetadata : DefaultViewMetadataBuilder
    {
        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddViewMetadata(new ViewMetadata
            {
                ViewName = "changeSetsView",
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs" ,
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "AddNewChangeSet" , Title = "Add new change set" },
                            new EnvironmentCultureValue { Name = "AddEditChangeSet" , Title = "Add/Edit change set" },
                            new EnvironmentCultureValue { Name = "ChangeSetDataIsInvalid" , Title = "Your change set's data is invalid" },
                            new EnvironmentCultureValue { Name = "EditChangeSet" , Title = "Edit change set" }
                        }
                    }
                }
            });

            return await base.BuildMetadata();
        }
    }
}