namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.BasicList;

public partial class BitBasicListDemo
{
    private readonly string example1RazorCode = @"
<BitBasicList Items=""fewPeople"" Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            Name: <strong>@person.FirstName</strong>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example1CsharpCode = @"
private readonly List<Person> fewPeople = Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
}).ToList();

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example2RazorCode = @"
<BitBasicList Virtualize
              Items=""lotsOfPeople""
              Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            <img width=""100px"" height=""100px"" src=""https://picsum.photos/100/100?random=@(person.Id)"">
            <div style=""margin-left:3%; display: inline-block;"">
                <p>Id: <strong>@person.Id</strong></p>
                <p>Full Name: <strong>@person.FirstName @person.LastName</strong></p>
                <p>Job: <strong>@person.Job</strong></p>
            </div>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example2CsharpCode = @"
private readonly List<Person> lotsOfPeople = Enumerable.Range(0, 8000).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
}).ToList();

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example3RazorCode = @"
<BitBasicList Virtualize
              ItemSize=""300""
              OverscanCount=""5""
              Items=""lotsOfPeople""
              Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            <img width=""100px"" height=""100px"" src=""https://picsum.photos/100/100?random=@(person.Id)"">
            <div style=""margin-left:3%; display: inline-block;"">
                <p>Id: <strong>@person.Id</strong></p>
                <p>Full Name: <strong>@person.FirstName @person.LastName</strong></p>
                <p>Job: <strong>@person.Job</strong></p>
            </div>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example3CsharpCode = @"
private readonly List<Person> lotsOfPeople = Enumerable.Range(0, 8000).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
}).ToList();

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example4RazorCode = @"
<BitBasicList Virtualize
              ItemSize=""83""
              TItem=""ProductDto""
              ItemsProvider=""productsProvider""
              Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""product"">
        <div @key=""product.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px;"">
            <div>Id: <strong>@product.Id</strong></div>
            <div>Name: <strong>@product.Name</strong></div>
            <div>Price: <strong>@product.Price</strong></div>
        </div>
    </RowTemplate>
    <VirtualizePlaceholder>
        <div style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px;"">
            <div>Id: <strong>Loading...</strong></div>
            <div>Name: <strong>Loading...</strong></div>
            <div>Price: <strong>Loading...</strong></div>
        </div>
    </VirtualizePlaceholder>
</BitBasicList>";
    private readonly string example4CsharpCode = @"
[Inject] private HttpClient HttpClient { get; set; } = default!;
[Inject] private NavigationManager NavManager { get; set; } = default!;

private BitBasicListItemsProvider<ProductDto> productsProvider;

protected override void OnInitialized()
{
    productsProvider = async req =>
    {
        try
        {
            var query = new Dictionary<string, object>()
            {
                 { ""$top"", req.Count},
                 { ""$skip"", req.StartIndex }
            };
    
            var url = NavManager.GetUriWithQueryParameters(""api/Products/GetProducts"", query);
    
            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);
    
            return BitBasicListItemsProviderResult.From(data!.Items, (int)data!.TotalCount);
        }
        catch
        {
            return BitBasicListItemsProviderResult.From<ProductDto>([], 0);
        }
    };

    base.OnInitialized();
}

public class ProductDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
}

[JsonSerializable(typeof(PagedResult<ProductDto>))]
public partial class AppJsonContext : JsonSerializerContext { }";

    private readonly string example5RazorCode = @"
<BitBasicList Virtualize
              ItemSize=""83""
              TItem=""CategoryOrProductDto""
              ItemsProvider=""categoriesAndProductsProvider""
              Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""catOrProd"">
        @if (catOrProd.IsProduct)
        {
            <div @key=""@($""{catOrProd.CategoryId}-{catOrProd.ProductId}"")"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 10px; display:flex; flex-flow:row;"">
                <div style=""min-width:184px;"">Name: <strong>@catOrProd.Name</strong></div>
                <div>Price: <strong>@catOrProd.Price</strong></div>
            </div>
        }
        else
        {
            <div @key=""catOrProd.CategoryId"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; background-color: #75737329;"">
                <div>@catOrProd.Name</div>
            </div>
        }
    </RowTemplate>
    <VirtualizePlaceholder>
        <div style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px;"">
            Loading...
        </div>
    </VirtualizePlaceholder>
</BitBasicList>";
    private readonly string example5CsharpCode = @"
[Inject] private HttpClient HttpClient { get; set; } = default!;
[Inject] private NavigationManager NavManager { get; set; } = default!;

private BitBasicListItemsProvider<CategoryOrProductDto> categoriesAndProductsProvider;

protected override void OnInitialized()
{
    categoriesAndProductsProvider = async req =>
    {
        try
        {
            var query = new Dictionary<string, object>()
            {
                { ""$top"", req.Count},
                { ""$skip"", req.StartIndex }
            };

            var url = NavManager.GetUriWithQueryParameters(""api/Products/GetCategoriesAndProducts"", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultCategoryOrProductDto);

            return BitBasicListItemsProviderResult.From(data!.Items, (int)data!.TotalCount);
        }
        catch
        {
            return BitBasicListItemsProviderResult.From<CategoryOrProductDto>([], 0);
        }
    };

    base.OnInitialized();
}

public class CategoryOrProductDto
{
    public int? ProductId { get; set; }
    public int? CategoryId { get; set; }
    public bool IsProduct => ProductId is not null;
    public string? Name { get; set; }
    public decimal? Price { get; set; }
}

[JsonSerializable(typeof(PagedResult<CategoryOrProductDto>))]
public partial class AppJsonContext : JsonSerializerContext { }";

    private readonly string example6RazorCode = @"
<BitBasicList LoadMore
              Items=""fewPeople""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 250px;"">
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            Name: <strong>@person.FirstName</strong>
        </div>
    </RowTemplate>
</BitBasicList>


<BitBasicList LoadMore
              Items=""fewPeople""
              LoadMoreText=""Bring more people here""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 250px;"">
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            Name: <b>@person.FirstName</b>
        </div>
    </RowTemplate>
</BitBasicList>


<BitBasicList LoadMore
              Items=""fewPeople""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 250px;"">
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            Name: <b>@person.FirstName</b>
        </div>
    </RowTemplate>
    <LoadMoreTemplate>
        <BitStack FitHeight Horizontal Style=""padding:8px;cursor:pointer"">
            <BitButton IconName=""@BitIconName.Download"" FullWidth>Load more people</BitButton>
        </BitStack>
    </LoadMoreTemplate>
</BitBasicList>


<BitBasicList LoadMore
              Virtualize
              Items=""lotsOfPeople""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 250px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            Full Name: <b>@person.FirstName @person.LastName</b>
        </div>
    </RowTemplate>
</BitBasicList>


<BitBasicList LoadMore
              ItemsProvider=""loadMoreProvider""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 250px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            Full Name: <b>@person.FirstName @person.LastName</b>
        </div>
    </RowTemplate>
    <LoadMoreTemplate Context=""isLoading"">
        @if (isLoading is false)
        {
            <BitStack FitHeight Horizontal Alignment=""BitAlignment.Center"" Style=""padding:1rem;cursor:pointer"">
                <BitIcon IconName=""@BitIconName.Download"" />
                <BitText>Load more people</BitText>
            </BitStack>
        }
        else
        {
            <BitStack FitHeight Horizontal Alignment=""BitAlignment.Center"">
                <BitRollingSquareLoading />
                <BitText>Loading...</BitText>
            </BitStack>
        }
    </LoadMoreTemplate>
</BitBasicList>


<BitBasicList LoadMore
              Virtualize
              ItemsProvider=""loadMoreVirtualizeProvider""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 250px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            Full Name: <b>@person.FirstName @person.LastName</b>
        </div>
    </RowTemplate>
    <LoadMoreTemplate Context=""isLoading"">
        @if (isLoading is false)
        {
            <BitStack FitHeight Horizontal Alignment=""BitAlignment.Center"" Style=""padding:1rem;cursor:pointer"">
                <BitIcon IconName=""@BitIconName.Download"" />
                <BitText>Load more people</BitText>
            </BitStack>
        }
        else
        {
            <BitStack FitHeight Horizontal Alignment=""BitAlignment.Center"">
                <BitRollingSquareLoading />
                <BitText>Loading...</BitText>
            </BitStack>
        }
    </LoadMoreTemplate>
</BitBasicList>";
    private readonly string example6CsharpCode = @"
private readonly List<Person> fewPeople = [.. Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

private readonly List<Person> lotsOfPeople = [.. Enumerable.Range(0, 8000).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

private BitBasicListItemsProvider<Person> loadMoreProvider = default!;
private BitBasicListItemsProvider<Person> loadMoreVirtualizeProvider = default!;

protected override void OnInitialized()
{
    loadMoreProvider = async req =>
    {
        await Task.Delay(1000);

        return BitBasicListItemsProviderResult.From([.. fewPeople.Skip(req.StartIndex).Take(req.Count)], fewPeople.Count);
    };

    loadMoreVirtualizeProvider = async req =>
    {
        await Task.Delay(500);

        return BitBasicListItemsProviderResult.From([.. lotsOfPeople.Skip(req.StartIndex).Take(req.Count)], lotsOfPeople.Count);
    };

    base.OnInitialized();
}

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example7RazorCode = @"
<style>
    .custom-class .list-item {
        gap: 0.5rem;
        color: white;
        display: flex;
        padding: 1rem;
        margin: 0.5rem;
        flex-wrap: wrap;
        border-radius: 0.25rem;
        background-color: tomato;
    }
</style>


<BitBasicList Virtualize
              Role=""list""
              Items=""lotsOfPeople""
              Class=""custom-class""
              Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" class=""list-item"">
            <span>Id: <strong>@person.Id</strong></span>
            <span>Full Name: <strong>@person.FirstName</strong></span>
            <span>Job: <strong>@person.Job</strong></span>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example7CsharpCode = @"
private readonly List<Person> lotsOfPeople = Enumerable.Range(0, 8000).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
}).ToList();

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example8RazorCode = @"
<BitBasicList Dir=""BitDir.Rtl"" Items=""fewPeopleRtl"" Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            <p>شناسه: <strong>@person.Id</strong></p>
            <p>نام کامل: <strong>@person.FirstName @person.LastName</strong></p>
            <p>شغل: <strong>@person.Job</strong></p>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example8CsharpCode = @"
private readonly List<Person> fewPeopleRtl = Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""شخص {i + 1}"",
    LastName = $""نام خانواگی شخص {i + 1}"",
    Job = $""برنامه نویس {i + 1}""
}).ToList();

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";
}
