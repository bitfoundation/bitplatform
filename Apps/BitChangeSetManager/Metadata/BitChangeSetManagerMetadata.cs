using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Core.Models;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations.Metadata;

namespace BitChangeSetManager.Metadata
{
    public class BitChangeSetManagerMetadata : DefaultProjectMetadataBuilder
    {
        public const string InsertIsDeined = nameof(InsertIsDeined);
        public const string UpdateIsDeined = nameof(UpdateIsDeined);
        public const string DeleteIsDeined = nameof(DeleteIsDeined);
        public const string OnlyGetIsAllowed = nameof(OnlyGetIsAllowed);

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
                            new EnvironmentCultureValue { Name = nameof(DeleteIsDeined), Title = "Delete is denied" },
                            new EnvironmentCultureValue { Name = nameof(OnlyGetIsAllowed), Title = "Only Get is allowed" },
                            new EnvironmentCultureValue { Name = "Save", Title = "Save" },
                            new EnvironmentCultureValue { Name = "Cancel", Title = "Cancel" },
                            new EnvironmentCultureValue { Name = "CloseMenu", Title = "Close Menu" },
                            new EnvironmentCultureValue { Name = "OpenMenu", Title = "Open Menu" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Yes", Title = "بلی" },
                            new EnvironmentCultureValue { Name = "No", Title = "خیر" },
                            new EnvironmentCultureValue { Name = "Ok", Title = "باشه" },
                            new EnvironmentCultureValue { Name = "Commands", Title = "فرامین" },
                            new EnvironmentCultureValue { Name = nameof(InsertIsDeined), Title = "درج مجاز نیست" },
                            new EnvironmentCultureValue { Name = nameof(UpdateIsDeined), Title = "ویرایش مجاز نیست" },
                            new EnvironmentCultureValue { Name = nameof(DeleteIsDeined), Title = "حذف مجاز نیست" },
                            new EnvironmentCultureValue { Name = nameof(OnlyGetIsAllowed), Title = "فقط دریافت مجاز است" },
                            new EnvironmentCultureValue { Name = "Save", Title = "ذخیره" },
                            new EnvironmentCultureValue { Name = "Cancel", Title = "انصراف" },
                            new EnvironmentCultureValue { Name = "CloseMenu", Title = "بستن منو" },
                            new EnvironmentCultureValue { Name = "OpenMenu", Title = "باز کردن منو" }
                        }
                    }
                }
            });

            return await base.BuildMetadata();
        }
    }
}