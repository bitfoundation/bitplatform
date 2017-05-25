using BitChangeSetManager.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Core.Models;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations.Metadata;

namespace BitChangeSetManager.Metadata
{
    public class DeliveryDtoMetadata : DefaultDtoMetadataBuilder<DeliveryDto>
    {
        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(DeliveryDto.Id), new DtoMemberMetadata
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

            AddMemberMetadata(nameof(DeliveryDto.CustomerName), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Customer name" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "نام مشتری" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(DeliveryDto.DeliveredOn), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Delivered on" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "ارائه شده در" }
                        }
                    }
                }
            });

            return await base.BuildMetadata();
        }
    }
}