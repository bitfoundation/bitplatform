using Bit.Api.ApiControllers;
using Bit.Tests.Model.Dto;
using System.Web.OData.Query;

namespace Bit.Tests.Api.ApiControllers
{
    public class ProductsController : DtoController<ProductDto>
    {

    }

    public class CountriesController : DtoController<CountryDto>
    {
        [Function]
        public virtual CountryDto[] GetAllContries(ODataQueryOptions<CountryDto> query)
        {
            return new CountryDto[]
            {
                new CountryDto { Id = 1 , Name = "Test1", SomeProperty = 1 },
                new CountryDto { Id = 2 , Name = "Test2", SomeProperty = 2 },
                new CountryDto { Id = 3 , Name = "Test3", SomeProperty = 3 }
            };
        }
    }
}
