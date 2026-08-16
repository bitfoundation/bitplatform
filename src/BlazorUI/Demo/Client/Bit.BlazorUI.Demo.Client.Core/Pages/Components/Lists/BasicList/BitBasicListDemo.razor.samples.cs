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
</BitBasicList>


<BitBasicList Items=""fruits"" Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 150px;"" />";
    private readonly string example1CsharpCode = @"
private readonly List<Person> fewPeople = [.. Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

private readonly List<string> fruits = [""Apple"", ""Apricot"", ""Banana"", ""Cherry"", ""Fig"",
                                        ""Grape"", ""Lemon"", ""Mango"", ""Orange"", ""Peach""];

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example2RazorCode = @"
<BitBasicList Items=""emptyPeople"" Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 150px;"">
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            Name: <strong>@person.FirstName</strong>
        </div>
    </RowTemplate>
    <EmptyContent>
        <BitStack Alignment=""BitAlignment.Center"" Style=""height: 150px;"">
            <BitIcon IconName=""@BitIconName.SearchIssue"" Size=""BitSize.Large"" />
            <BitText Typography=""BitTypography.Body1"">Nobody to show here yet.</BitText>
        </BitStack>
    </EmptyContent>
</BitBasicList>";
    private readonly string example2CsharpCode = @"
private readonly List<Person> emptyPeople = [];

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example3RazorCode = @"
<BitBasicList Items=""fewPeople"" Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 300px;"">
    <HeaderTemplate>
        <BitStack Horizontal HorizontalAlign=""BitAlignment.SpaceBetween""
                  Style=""padding: 0.5rem 1rem; background: #75737329; position: sticky; top: 0;"">
            <BitText Typography=""BitTypography.Subtitle1"">People</BitText>
            <BitTag Color=""BitColor.Info"">@fewPeople.Count</BitTag>
        </BitStack>
    </HeaderTemplate>
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            Name: <strong>@person.FirstName</strong>
        </div>
    </RowTemplate>
    <FooterTemplate>
        <BitStack Alignment=""BitAlignment.Center"" Style=""padding: 0.5rem;"">
            <BitText Typography=""BitTypography.Caption1"">— end of the list —</BitText>
        </BitStack>
    </FooterTemplate>
</BitBasicList>";
    private readonly string example3CsharpCode = @"
private readonly List<Person> fewPeople = [.. Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example4RazorCode = @"
<BitToggleButton @bind-IsChecked=""isLoading"" Text=""@(isLoading ? ""Stop loading"" : ""Start loading"")"" />

<BitBasicList Items=""fewPeople"" Loading=""isLoading""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 200px;"">
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            Name: <strong>@person.FirstName</strong>
        </div>
    </RowTemplate>
    <LoadingTemplate>
        <BitStack Horizontal Alignment=""BitAlignment.Center"">
            <BitRollingSquareLoading />
            <BitText>Fetching people...</BitText>
        </BitStack>
    </LoadingTemplate>
</BitBasicList>";
    private readonly string example4CsharpCode = @"
private bool isLoading;

private readonly List<Person> fewPeople = [.. Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example5RazorCode = @"
<BitBasicList Virtualize
              Items=""lotsOfPeople""
              Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            <img width=""100"" height=""100"" src=""https://picsum.photos/100/100?random=@(person.Id)"">
            <div style=""margin-left:3%; display: inline-block;"">
                <p>Id: <strong>@person.Id</strong></p>
                <p>Full Name: <strong>@person.FirstName @person.LastName</strong></p>
                <p>Job: <strong>@person.Job</strong></p>
            </div>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example5CsharpCode = @"
private readonly List<Person> lotsOfPeople = [.. Enumerable.Range(0, 8000).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example6RazorCode = @"
<BitBasicList Virtualize
              ItemSize=""300""
              OverscanCount=""5""
              Items=""lotsOfPeople""
              Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            <img width=""100"" height=""100"" src=""https://picsum.photos/100/100?random=@(person.Id)"">
            <div style=""margin-left:3%; display: inline-block;"">
                <p>Id: <strong>@person.Id</strong></p>
                <p>Full Name: <strong>@person.FirstName @person.LastName</strong></p>
                <p>Job: <strong>@person.Job</strong></p>
            </div>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example6CsharpCode = @"
private readonly List<Person> lotsOfPeople = [.. Enumerable.Range(0, 8000).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example7RazorCode = @"
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
    private readonly string example7CsharpCode = @"
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

            return BitBasicListItemsProviderResult.From(data!.Items, data!.TotalCount);
        }
        catch
        {
            return BitBasicListItemsProviderResult.Empty<ProductDto>();
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

    private readonly string example8RazorCode = @"
<BitBasicList Virtualize
              ItemSize=""32""
              TItem=""CategoryOrProductDto""
              ItemsProvider=""categoriesAndProductsProvider""
              Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    @* Every row and the placeholder are 32px tall on purpose: the virtualization sizes its scroll
       region by a single item size, so rows of differing heights keep it correcting that size on
       every render and never let the list settle. *@
    <RowTemplate Context=""catOrProd"">
        @if (catOrProd.IsProduct)
        {
            <div @key=""@($""{catOrProd.CategoryId}-{catOrProd.ProductId}"")"" style=""height: 32px; box-sizing: border-box; border-bottom: 1px #8a8886 solid; padding: 5px 10px; display: flex; flex-flow: row; align-items: center; white-space: nowrap; overflow: hidden;"">
                <div style=""width: 240px; overflow: hidden; text-overflow: ellipsis;"">Name: <strong>@catOrProd.Name</strong></div>
                <div>Price: <strong>@catOrProd.Price</strong></div>
            </div>
        }
        else
        {
            <div @key=""catOrProd.CategoryId"" style=""height: 32px; box-sizing: border-box; border-bottom: 1px #8a8886 solid; padding: 5px 20px; display: flex; align-items: center; white-space: nowrap; overflow: hidden; background-color: #75737329;"">
                <div>@catOrProd.Name</div>
            </div>
        }
    </RowTemplate>
    <VirtualizePlaceholder>
        <div style=""height: 32px; box-sizing: border-box; border-bottom: 1px #8a8886 solid; padding: 5px 20px; display: flex; align-items: center;"">
            Loading...
        </div>
    </VirtualizePlaceholder>
</BitBasicList>";
    private readonly string example8CsharpCode = @"
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

            return BitBasicListItemsProviderResult.From(data!.Items, data!.TotalCount);
        }
        catch
        {
            return BitBasicListItemsProviderResult.Empty<CategoryOrProductDto>();
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

    private readonly string example9RazorCode = @"
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
              OnLoadMore=""c => loadedCount = c""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 250px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            Full Name: <b>@person.FirstName @person.LastName</b>
        </div>
    </RowTemplate>
    <LoadMoreTemplate Context=""isLoadingMore"">
        @if (isLoadingMore is false)
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

<div>Loaded so far: <b>@loadedCount</b></div>


<BitBasicList LoadMore
              Virtualize
              ItemsProvider=""loadMoreVirtualizeProvider""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 250px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            Full Name: <b>@person.FirstName @person.LastName</b>
        </div>
    </RowTemplate>
    <LoadMoreTemplate Context=""isLoadingMore"">
        @if (isLoadingMore is false)
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
    private readonly string example9CsharpCode = @"
private int loadedCount;

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

    private readonly string example10RazorCode = @"
<BitBasicList LoadMore
              AutoLoad
              AutoLoadThreshold=""150""
              ItemsProvider=""autoLoadProvider""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 300px;"">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            Full Name: <b>@person.FirstName @person.LastName</b>
        </div>
    </RowTemplate>
    <LoadMoreTemplate>
        <BitStack FitHeight Horizontal Alignment=""BitAlignment.Center"" Style=""padding:1rem"">
            <BitRollingSquareLoading />
            <BitText>Loading more people...</BitText>
        </BitStack>
    </LoadMoreTemplate>
</BitBasicList>";
    private readonly string example10CsharpCode = @"
private readonly List<Person> lotsOfPeople = [.. Enumerable.Range(0, 8000).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

private BitBasicListItemsProvider<Person> autoLoadProvider = default!;

protected override void OnInitialized()
{
    autoLoadProvider = async req =>
    {
        await Task.Delay(700);

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

    private readonly string example11RazorCode = @"
<BitBasicList Horizontal Items=""fewPeople"" Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""person"">
        <div style=""width: 150px; margin: 10px; padding: 10px; text-align: center; background-color: #75737329;"">
            <div><strong>@person.FirstName</strong></div>
            <div>@person.Job</div>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example11CsharpCode = @"
private readonly List<Person> fewPeople = [.. Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example12RazorCode = @"
<BitStack Horizontal Wrap>
    <BitButton OnClick=""() => listRef?.ScrollToStartAsync(true) ?? Task.CompletedTask"">Scroll to start</BitButton>
    <BitButton OnClick=""() => listRef?.ScrollToEndAsync(true) ?? Task.CompletedTask"">Scroll to end</BitButton>
    <BitButton OnClick=""() => listRef?.ScrollToIndexAsync(50, true) ?? Task.CompletedTask"">Scroll to #51</BitButton>
    <BitButton OnClick=""AddPerson"">Add a person</BitButton>
</BitStack>

<BitBasicList @ref=""listRef"" Items=""mutablePeople""
              Style=""border: 1px #a19f9d solid; border-radius: 4px; height: 250px;"">
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            <strong>@person.Id</strong> - @person.FirstName
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example12CsharpCode = @"
private bool scrollToEndPending;
private BitBasicList<Person>? listRef;

private readonly List<Person> mutablePeople = [.. Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

private async Task AddPerson()
{
    var id = mutablePeople.Count + 1;

    mutablePeople.Add(new Person
    {
        Id = id,
        FirstName = $""Person {id}"",
        LastName = $""Person Family {id}"",
        Job = $""Programmer {id}""
    });

    // The collection instance itself did not change, so the list is told to pick up its new contents.
    if (listRef is not null)
    {
        await listRef.RefreshDataAsync();

        // The new row is only scrollable to once it has been rendered, so the scrolling waits for that render.
        scrollToEndPending = true;
    }
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (scrollToEndPending && listRef is not null)
    {
        scrollToEndPending = false;

        await listRef.ScrollToEndAsync(true);
    }

    await base.OnAfterRenderAsync(firstRender);
}

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example13RazorCode = @"
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
</BitBasicList>


<BitBasicList LoadMore
              Items=""fewPeople""
              Styles=""@(new() { Root = ""border: 1px solid tomato; border-radius: 4px; height: 250px;"",
                                Header = ""padding: 0.5rem 1rem; font-weight: bold; background: tomato; color: white;"",
                                LoadMoreText = ""color: tomato; font-weight: bold;"" })"">
    <HeaderTemplate>People</HeaderTemplate>
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            Name: <strong>@person.FirstName</strong>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example13CsharpCode = @"
private readonly List<Person> lotsOfPeople = [.. Enumerable.Range(0, 8000).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

private readonly List<Person> fewPeople = [.. Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""Person {i + 1}"",
    LastName = $""Person Family {i + 1}"",
    Job = $""Programmer {i + 1}""
})];

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example14RazorCode = @"
<BitBasicList Dir=""BitDir.Rtl"" Items=""fewPeopleRtl"" Style=""border: 1px #a19f9d solid; border-radius: 4px;"">
    <RowTemplate Context=""person"">
        <div style=""padding: 5px 20px; margin: 10px; background-color: #75737329;"">
            <p>شناسه: <strong>@person.Id</strong></p>
            <p>نام کامل: <strong>@person.FirstName @person.LastName</strong></p>
            <p>شغل: <strong>@person.Job</strong></p>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example14CsharpCode = @"
private readonly List<Person> fewPeopleRtl = [.. Enumerable.Range(0, 100).Select(i => new Person
{
    Id = i + 1,
    FirstName = $""شخص {i + 1}"",
    LastName = $""نام خانواگی شخص {i + 1}"",
    Job = $""برنامه نویس {i + 1}""
})];

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";
}
