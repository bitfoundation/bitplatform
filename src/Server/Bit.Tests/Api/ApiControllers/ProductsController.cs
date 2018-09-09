using Bit.OData.ODataControllers;
using Bit.Tests.Model.Dto;
using Microsoft.AspNet.OData.Query;

namespace Bit.Tests.Api.ApiControllers
{
    public class ProductsController : DtoController<ProductDto>
    {

    }

    public class CountriesController : DtoController<CountryDto>
    {
        [Function]
        public virtual CountryDto[] GetAllCountries(ODataQueryOptions<CountryDto> query)
        {
            return new[]
            {
                new CountryDto { Id = 1 , Name = "Test1", SomeProperty = 1 },
                new CountryDto { Id = 2 , Name = "Test2", SomeProperty = 2 },
                new CountryDto { Id = 3 , Name = "Test3", SomeProperty = 3 }
            };
        }
    }
}
