using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations.Metadata;
using Bit.Tests.Model.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Tests.Owin.Metadata.Dtos
{
    public class ProductDtoMetadata : DefaultDtoMetadataBuilder<ProductDto>
    {
        public override Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddDtoMetadata(new DtoMetadata { });

            base.AddLookup<CountryDto>(nameof(ProductDto.BuildLocationId_InMemoryTest), nameof(CountryDto.Id), nameof(CountryDto.Name), it => it.SomeProperty == 1 || it.SomeProperty == 2);
            base.AddLookup<CountryDto>(nameof(ProductDto.BuildLocationId_OnlineTest), nameof(CountryDto.Id), nameof(CountryDto.Name), it => it.SomeProperty == 1 || it.SomeProperty == 3);
            base.AddLookup<CountryDto>(nameof(ProductDto.BuildLocationId_OfflineDatabaseTest), nameof(CountryDto.Id), nameof(CountryDto.Name), it => it.SomeProperty == 2);
            base.AddLookup<CountryDto>(nameof(ProductDto.BuildLocationId), nameof(CountryDto.Id), nameof(CountryDto.Name));

            return base.BuildMetadata();
        }
    }
}
