using System.Collections.Generic;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations.Metadata;
using Foundation.Model.DomainModels;

namespace Foundation.Api.Metadata.Dtos
{
    public class UserSettingMetadataBuilder : DefaultDtoMetadataBuilder<UserSetting>
    {
        public override IEnumerable<ObjectMetadata> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            AddMemberMetadata(nameof(UserSetting.Id), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.Theme), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.UserId), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.Culture), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.DesiredTimeZone), new DtoMemberMetadata
            {

            });

            AddMemberMetadata(nameof(UserSetting.Version), new DtoMemberMetadata
            {

            });

            return base.BuildMetadata();
        }
    }
}
