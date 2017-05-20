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
                            new EnvironmentCultureValue { Name = "EditChangeSet" , Title = "Edit change set" },
                            new EnvironmentCultureValue { Name = "ChangeSets" , Title = "Change Sets" },
                            new EnvironmentCultureValue { Name = "DeliveriesOf" , Title = "Deliveries of" },
                            new EnvironmentCultureValue { Name = "ChangeSetHasBeenInsertedByUser" , Title = "Change set {{title}} has been inserted by {{userName}}" },
                            new EnvironmentCultureValue { Name = "Templates", Title = "Templates"}
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr" ,
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "AddNewChangeSet" , Title = "درج ChangeSet جدید" },
                            new EnvironmentCultureValue { Name = "AddEditChangeSet" , Title = "درج/ویرایش ChangeSet" },
                            new EnvironmentCultureValue { Name = "ChangeSetDataIsInvalid" , Title = "اطلاعات ChangeSet شما اشتباه است" },
                            new EnvironmentCultureValue { Name = "EditChangeSet" , Title = "ویرایش ChangeSet" },
                            new EnvironmentCultureValue { Name = "ChangeSets" , Title = "Change Set ها" },
                            new EnvironmentCultureValue { Name = "DeliveriesOf" , Title = "ارائه های" },
                            new EnvironmentCultureValue { Name = "ChangeSetHasBeenInsertedByUser" , Title = "Change set {{title}} به وسیله {{userName}} درج شد" },
                            new EnvironmentCultureValue { Name = "Templates", Title = "الگوها"}
                        }
                    }
                }
            });

            return await base.BuildMetadata();
        }
    }
}
