using Bit.Core.Models;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations.Metadata;
using Bit.Tests.Model.DomainModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Tests.Owin.Metadata.Model
{
    public class ParentEntityMetadata : DefaultDtoMetadataBuilder<ParentEntity>
    {
        public override async Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(ParentEntity.Id), new DtoMemberMetadata
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
                    }
                }
            });

            AddMemberMetadata(nameof(ParentEntity.Name), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Name" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ParentEntity.Version), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Version" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ParentEntity.Date), new DtoMemberMetadata
            {
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = "Label" , Title = "Date" }
                        }
                    }
                }
            });

            AddMemberMetadata(nameof(ParentEntity.ChildEntities), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(ParentEntity.TestModel), new DtoMemberMetadata
            {

            });

            AddLookup<ParentEntity>(nameof(ParentEntity.Name), nameof(ParentEntity.Name), nameof(ParentEntity.Name), it => it.Name != "!"); // For testing purposes only :D

            return await base.BuildMetadata();
        }
    }
}
