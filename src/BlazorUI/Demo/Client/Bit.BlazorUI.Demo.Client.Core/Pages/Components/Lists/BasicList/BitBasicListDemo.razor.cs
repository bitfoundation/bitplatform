namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.BasicList;

public partial class BitBasicListDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitBasicListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "EmptyContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content that will be rendered when there is no item to show.",
        },
        new()
        {
            Name = "FitHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the height of the list to fit its content.",
        },
        new()
        {
            Name = "FitSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the width and height of the list to fit its content.",
        },
        new()
        {
            Name = "FitWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the width of the list to fit its content.",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the height of the list to 100%.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the width and height of the list to 100%.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the width of the list to 100%.",
        },
        new()
        {
            Name = "Items",
            Type = "ICollection<TItem>",
            DefaultValue = "new Array.Empty<TItem>()",
            Description = "The list of items to render.",
        },
        new()
        {
            Name = "ItemSize",
            Type = "float",
            DefaultValue = "50",
            Description = "Size of each item in pixels. Defaults to 50px.",
        },
        new()
        {
            Name = "ItemsProvider",
            Type = "BitBasicListItemsProvider<TItem>?",
            DefaultValue = "null",
            Description = "The function providing items to the list.",
        },
        new()
        {
            Name = "LoadMore",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the LoadMore mode for the list.",
        },
        new()
        {
            Name = "LoadMoreSize",
            Type = "int",
            DefaultValue = "20",
            Description = "The number of items to be loaded and rendered after the LoadMore button is clicked. Defaults to 20.",
        },
        new()
        {
            Name = "LoadMoreTemplate",
            Type = "RenderFragment<bool>?",
            DefaultValue = "null",
            Description = "The template of the LoadMore button.",
        },
        new()
        {
            Name = "LoadMoreText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom text of the default LoadMore button. Defaults to \"LoadMore\".",
        },
        new()
        {
            Name = "OverscanCount",
            Type = "int",
            DefaultValue = "3",
            Description = "A value that determines how many additional items will be rendered before and after the visible region in Virtualize mode.",
        },
        new()
        {
            Name = "Role",
            Type = "string",
            DefaultValue = "list",
            Description = "The role attribute of the html element of the list.",
        },
        new()
        {
            Name = "RowTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The template to render each row.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitBasicListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Virtualize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables virtualization in rendering the list.",
        },
        new()
        {
            Name = "VirtualizePlaceholder",
            Type = "RenderFragment<PlaceholderContext>?",
            DefaultValue = "null",
            Description = "The template for items that have not yet rendered.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitBasicListClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the list.",
                },
                new()
                {
                    Name = "LoadMoreButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the LoadMore button of the list.",
                },
                new()
                {
                    Name = "LoadMoreText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the LoadMore text of the list.",
                },
            ]
        }
    ];



    private readonly List<Person> lotsOfPeople = [.. Enumerable.Range(0, 8000).Select(i => new Person
    {
        Id = i + 1,
        FirstName = $"Person {i + 1}",
        LastName = $"Person Family {i + 1}",
        Job = $"Programmer {i + 1}"
    })];

    private readonly List<Person> fewPeople = [.. Enumerable.Range(0, 100).Select(i => new Person
    {
        Id = i + 1,
        FirstName = $"Person {i + 1}",
        LastName = $"Person Family {i + 1}",
        Job = $"Programmer {i + 1}"
    })];

    private readonly List<Person> fewPeopleRtl = [.. Enumerable.Range(0, 100).Select(i => new Person
    {
        Id = i + 1,
        FirstName = $"شخص {i + 1}",
        LastName = $"نام خانواگی شخص {i + 1}",
        Job = $"برنامه نویس {i + 1}"
    })];

    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;

    private BitBasicListItemsProvider<ProductDto> productsProvider = default!;
    private BitBasicListItemsProvider<CategoryOrProductDto> categoriesAndProductsProvider = default!;

    private BitBasicListItemsProvider<Person> loadMoreProvider = default!;
    private BitBasicListItemsProvider<Person> loadMoreVirtualizeProvider = default!;

    protected override void OnInitialized()
    {
        productsProvider = async req =>
        {
            try
            {
                var query = new Dictionary<string, object?>()
                {
                    { "$top", req.Count},
                    { "$skip", req.StartIndex }
                };

                var url = NavManager.GetUriWithQueryParameters("Products/GetProducts", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

                return BitBasicListItemsProviderResult.From(data!.Items!, data!.TotalCount);
            }
            catch
            {
                return BitBasicListItemsProviderResult.From<ProductDto>([], 0);
            }
        };

        categoriesAndProductsProvider = async req =>
        {
            try
            {
                var query = new Dictionary<string, object?>()
                {
                    { "$top", req.Count},
                    { "$skip", req.StartIndex }
                };

                var url = NavManager.GetUriWithQueryParameters("Products/GetCategoriesAndProducts", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultCategoryOrProductDto);

                return BitBasicListItemsProviderResult.From(data!.Items!, data!.TotalCount);
            }
            catch
            {
                return BitBasicListItemsProviderResult.From<CategoryOrProductDto>([], 0);
            }
        };

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
}
