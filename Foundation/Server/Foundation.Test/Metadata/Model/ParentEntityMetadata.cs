using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Test.Model.DomainModels;
using System.Collections.Generic;

namespace Foundation.Test.Metadata.Model
{
    public class ParentEntityMetadata : DefaultDtoMetadataBuilder<ParentEntity>
    {
        public override IEnumerable<ObjectMetadata> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(ParentEntity.Id), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(ParentEntity.Name), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(ParentEntity.Version), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(ParentEntity.ChildEntities), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(ParentEntity.TestModel), new DtoMemberMetadata
            {

            });

            return base.BuildMetadata();
        }
    }
}
