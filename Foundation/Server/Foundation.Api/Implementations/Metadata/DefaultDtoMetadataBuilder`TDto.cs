using System;
using System.Reflection;
using Foundation.Api.Contracts.Metadata;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Foundation.Core.Models;

namespace Foundation.Api.Implementations.Metadata
{
    public class DefaultDtoMetadataBuilder<TDto> : DefaultMetadataBuilder, IDtoMetadataBuilder<TDto>
        where TDto : class
    {
        private DtoMetadata _dtoMetadata;

        public virtual IDtoMetadataBuilder<TDto> AddDtoMetadata(DtoMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            _dtoMetadata = metadata;

            _dtoMetadata.DtoType = typeof(TDto).GetTypeInfo().FullName;

            AllMetadata.Add(_dtoMetadata);

            return this;
        }

        public virtual IDtoMetadataBuilder<TDto> AddMemberMetadata(string memberName, DtoMemberMetadata metadata)
        {
            if (memberName == null)
                throw new ArgumentNullException(nameof(memberName));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            return AddMemberMetadata(typeof(TDto).GetTypeInfo().GetProperty(memberName), metadata);
        }

        public virtual IDtoMetadataBuilder<TDto> AddMemberMetadata(PropertyInfo member, DtoMemberMetadata metadata)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            if (_dtoMetadata == null)
                throw new InvalidOperationException($"{nameof(AddDtoMetadata)} must be called first");

            metadata.DtoMemberName = member.Name;

            if (metadata.IsRequired == false)
                metadata.IsRequired = member.GetCustomAttribute<RequiredAttribute>() != null;

            if (metadata.Pattern == null)
                metadata.Pattern = member.GetCustomAttribute<RegularExpressionAttribute>()?.Pattern;

            _dtoMetadata.MembersMetadata.Add(metadata);

            return this;
        }
    }
}
