using AdminPanel.Server.Api.Models.Account;
using AdminPanel.Server.Api.Models.Categories;
using AdminPanel.Server.Api.Models.Products;
using AdminPanel.Shared.Dtos.Account;
using AdminPanel.Shared.Dtos.Categories;
using AdminPanel.Shared.Dtos.Products;
using Riok.Mapperly.Abstractions;

namespace AdminPanel.Server.Api.Models;

/// <summary>
/// When you have an IQueryable of an Entity or Model classes from EntityFrameworkCore, 
/// you ultimately need to convert it into an IQueryable of DTO classes and return it to the client.
/// The client can also implement pagination during the API call by sending values for $top, $skip, etc in query string.
/// Ultimately, the query is executed by aspnetcore and the data gets streamed from the database to the client, which is the most optimal case.
/// For this, you need to write a ProjectTo for each Entity you intend to return a query of DTO class from,
/// and also write a Map for when a DTO is sent to the server for create or update api,
/// so you can convert it to an Entity and save it in the database using ef core.
/// You also need a Map method to convert DTO to Entity and vice versa,
/// as well as a Patch method for the update scenario. For the update scenario, first find the Entity from the database with its Id,
/// then patch the latest changes from the DTO sent by the client, and finally save it.
/// The methods below have been used in the controller classes.
/// For more information and to learn about customizing the mapping process, visit the website below:
/// https://mapperly.riok.app/docs/intro/
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class Mapper
{
    public static partial IQueryable<CategoryDto> Project(this IQueryable<Category> query);
    public static partial CategoryDto Map(this Category source);
    public static partial Category Map(this CategoryDto source);
    public static partial void Patch(this CategoryDto source, Category destination);
    public static partial void Patch(this Category source, CategoryDto destination);


    public static partial IQueryable<ProductDto> Project(this IQueryable<Product> query);
    public static partial ProductDto Map(this Product source);
    public static partial Product Map(this ProductDto source);
    public static partial void Patch(this ProductDto source, Product destination);

    [MapperIgnoreSource(nameof(Product.Category))]
    public static partial void Patch(this Product source, ProductDto destination);


    public static partial UserDto Map(this User source);
    public static partial void Patch(this EditUserDto source, User destination);

    private static partial User MapInternal(this SignUpRequestDto source);
    public static User Map(this SignUpRequestDto source)
    {
        var destination = source.MapInternal();

        destination.UserName = source.Email; // after map sample.

        return destination;
    }
}
