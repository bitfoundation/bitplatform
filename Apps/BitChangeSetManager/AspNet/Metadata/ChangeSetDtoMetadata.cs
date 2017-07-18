using BitChangeSetManager.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Core.Models;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations.Metadata;

namespace BitChangeSetManager.Metadata
{
    public class ChangeSetDtoMetadata : DefaultDtoMetadataBuilder<ChangeSetDto>
    {
        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(ChangeSetDto.Id), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Id" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "شناسه" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.Title), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Title" },
                            new EnvironmentCultureValue { Name = "RequiredMessage" , Title = "Title is required" },
                            new EnvironmentCultureValue { Name = "MaxLengthMessage" , Title = "Title's length is too big" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "عنوان" },
                            new EnvironmentCultureValue { Name = "RequiredMessage" , Title = "عنوان اجباری است" },
                            new EnvironmentCultureValue { Name = "MaxLengthMessage" , Title = "طول عنوان طولانی است" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.CreatedOn), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name =  "Label" , Title = "Created on" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name =  "Label" , Title = "تاریخ ایجاد" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.Description), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Description" },
                            new EnvironmentCultureValue { Name = "MaxLengthMessage" , Title = "Description's length is too big" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "توضیحات" },
                            new EnvironmentCultureValue { Name = "MaxLengthMessage" , Title = "توضیحات طولانی است" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.AssociatedCommitUrl), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Associated commit url" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Url تغییر مربوطه" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.IsDeliveredToAll), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Is delivered to all" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "ارائه شده به همه؟" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.SeverityId), new DtoMemberMetadata
            {
                IsRequired = true,
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Severity" },
                            new EnvironmentCultureValue { Name = "RequiredMessage" , Title = "Severity is required" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "اهمیت" },
                            new EnvironmentCultureValue { Name = "RequiredMessage" , Title = "اهمیت اجباری است" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.DeliveryRequirementId), new DtoMemberMetadata
            {
                IsRequired = true,
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Delivery requirement" },
                            new EnvironmentCultureValue { Name = "RequiredMessage" , Title = "Delivery requirement is required" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "نوع تحویل" },
                            new EnvironmentCultureValue { Name = "RequiredMessage" , Title = "نوع تحویل اجباری است" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.ProvinceId), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Province" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "استان" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.CityId), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "City" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "شهر" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ChangeSetDto.NeedsReviewId), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Needs review" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "احتیاح به بازبینی دارد" }
                        }
                    }
                }
            });

            AddLookup<ChangeSetSeverityDto>(nameof(ChangeSetDto.SeverityTitle), nameof(ChangeSetSeverityDto.Title), nameof(ChangeSetSeverityDto.Title));
            AddLookup<ChangeSetDeliveryRequirementDto>(nameof(ChangeSetDto.DeliveryRequirementTitle), nameof(ChangeSetDeliveryRequirementDto.Title), nameof(ChangeSetDeliveryRequirementDto.Title));
            AddLookup<ChangeSetSeverityDto>(nameof(ChangeSetDto.SeverityId), nameof(ChangeSetSeverityDto.Id), nameof(ChangeSetSeverityDto.Title));
            AddLookup<ChangeSetDeliveryRequirementDto>(nameof(ChangeSetDto.DeliveryRequirementId), nameof(ChangeSetDeliveryRequirementDto.Id), nameof(ChangeSetDeliveryRequirementDto.Title));
            AddLookup<ProvinceDto>(nameof(ChangeSetDto.ProvinceId), nameof(ProvinceDto.Id), nameof(ProvinceDto.Name));
            AddLookup<CityDto>(nameof(ChangeSetDto.CityId), nameof(CityDto.Id), nameof(CityDto.Name));
            AddLookup<ConstantDto>(nameof(ChangeSetDto.NeedsReviewId), nameof(ConstantDto.Id), nameof(ConstantDto.Title), baseFilter: it => it.Name.StartsWith("Ans_"));

            return await base.BuildMetadata();
        }
    }
}