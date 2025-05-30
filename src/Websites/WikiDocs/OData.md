# Simplifying OData with `Microsoft.AspNetCore.OData` in bit Boilerplate

If you've ever tried using OData before, you know it can be a bit of a headache. Between creating EDM models, inheriting from `ODataController`, dealing with strict routing conventions, and writing verbose configuration code, it often felt more like a burden than a convenience.

Well, Microsoft has completely redesigned Microsoft.AspNetCore.OData in recent versions. The new design is much more aligned with modern ASP.NET Core practices, giving developers full flexibility while still enabling rich OData querying capabilities — all without forcing you into rigid patterns.

## You Can Use OData Like Never Before

- ✅ No need for `ODataController`
- ✅ No need for EDM model setup
- ✅ No strict routing requirements
- ✅ Works seamlessly with both regular controllers and minimal APIs
- ✅ Supports `$filter`, `$top`, `$skip`, `$orderby`, `$select`, and more

All you need is an action (or endpoint) that returns or accepts an `IQueryable<T>`, or optionally apply query options manually.

---

## Enable OData Query Support Anywhere

Let’s start simple. Imagine you have a standard Web API controller:

```csharp
[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public IQueryable<ProductDto> Get()
    {
        return _db.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            });
    }
}
```

To enable OData query support on this endpoint, all you need to do is add the `[EnableQuery]` attribute:

```csharp
[HttpGet]
[EnableQuery]
public IQueryable<ProductDto> Get()
{
    return _db.Products
        .Where(p => p.IsActive)
        .Select(...);
}
```

That’s it. Now, clients can query your endpoint using familiar OData syntax like:

```
/products?$top=5
/products?$filter=Price gt 100&$orderby=Name
/products?$select=Id,Name
```

And the best part? The filtering, sorting, and projection will be applied **after** your server-side logic (like `.Where(p => p.IsActive)`), so clients cannot bypass your business rules.

This gives you full flexibility while keeping the benefits of OData querying.

---

## What If I Don’t Want to Return IQueryable?

Sometimes, you might want to process the query results before returning them — perhaps mapping to a DTO or applying additional transformations. In that case, you can take control by accepting `ODataQueryOptions<T>` as a parameter and applying it yourself:

```csharp
[HttpGet]
public async Task<ActionResult<List<ProductDto>>> Get(
    [FromServices] AppDbContext db,
    ODataQueryOptions<ProductDto> options)
{
    var query = db.Products
        .Where(p => p.IsActive)
        .Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price
        });

    var result = await options.ApplyTo(query).ToListAsync();
    
    return Ok(result);
}
```

This way, you get to decide when and how the query gets executed — all while allowing clients to use familiar OData query strings.

---

## Supported Query Options

Here are some common OData query parameters you can use:

| Query String              | Description                                                  |
|---------------------------|--------------------------------------------------------------|
| `$top=5`                  | Returns only the top 5 items                                 |
| `$skip=10`                | Skips the first 10 items                                     |
| `$orderby=Name`           | Orders by Name ascending                                     |
| `$orderby=Name desc`      | Orders by Name descending                                    |
| `$filter=Price gt 100`    | Filters items where Price > 100                              |
| `$select=Id,Name`         | Only includes Id and Name fields                             |

These are all supported out of the box, and they get translated into efficient SQL queries — meaning only the necessary data is fetched from the database.