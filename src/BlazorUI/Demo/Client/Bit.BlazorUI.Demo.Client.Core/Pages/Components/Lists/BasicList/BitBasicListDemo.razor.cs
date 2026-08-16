namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.BasicList;

public partial class BitBasicListDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoLoad",
            Type = "bool",
            DefaultValue = "false",
            Description = "Loads the next page as soon as the end of the loaded items scrolls into view, turning the LoadMore button into an infinite scrolling list. Only effective while LoadMore is enabled.",
        },
        new()
        {
            Name = "AutoLoadThreshold",
            Type = "int",
            DefaultValue = "0",
            Description = "How many pixels before the end of the loaded items the next page starts loading in AutoLoad mode.",
        },
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
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered below the items of the list, inside its scrolling region.",
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
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered above the items of the list, inside its scrolling region.",
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the items of the list out in a row and scrolls it sideways instead of down. Ignored while Virtualize is enabled, since virtualization is vertical only.",
        },
        new()
        {
            Name = "Items",
            Type = "ICollection<TItem>?",
            DefaultValue = "null",
            Description = "The list of items to render.",
        },
        new()
        {
            Name = "ItemSize",
            Type = "float",
            DefaultValue = "50",
            Description = "Size of each item in pixels, used by the Virtualize mode to calculate the scroll range and the number of rows to render.",
        },
        new()
        {
            Name = "ItemsProvider",
            Type = "BitBasicListItemsProvider<TItem>?",
            DefaultValue = "null",
            Description = "The function providing items to the list. It always takes priority over Items, and is called for a region at a time in Virtualize mode, for a page at a time in LoadMore mode, and once for the whole set otherwise.",
        },
        new()
        {
            Name = "ItemsProviderDelay",
            Type = "int",
            DefaultValue = "100",
            Description = "The number of milliseconds the list waits before calling the ItemsProvider in Virtualize mode, which debounces the requests a scroll issues. A value of 0 turns the debouncing off. Never applies to the pages of the LoadMore mode.",
        },
        new()
        {
            Name = "Loading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the loading content of the list in place of its items. The list also raises this state on its own while it is fetching items.",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template rendered while the list is loading its items.",
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
            Description = "The number of items to be loaded and rendered after the LoadMore button is clicked.",
        },
        new()
        {
            Name = "LoadMoreTemplate",
            Type = "RenderFragment<bool>?",
            DefaultValue = "null",
            Description = "The template of the LoadMore button. Its context is whether a page is being loaded at that moment.",
        },
        new()
        {
            Name = "LoadMoreText",
            Type = "string?",
            DefaultValue = "Load more",
            Description = "The custom text of the default LoadMore button.",
        },
        new()
        {
            Name = "OnLoadingChange",
            Type = "EventCallback<bool>",
            Description = "The callback that is invoked when the list starts and stops loading its items.",
        },
        new()
        {
            Name = "OnLoadMore",
            Type = "EventCallback<int>",
            Description = "The callback that is invoked after each page of the LoadMore mode has been appended, with the number of items the list holds at that point.",
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
            Type = "string?",
            DefaultValue = "list",
            Description = "The role attribute of the html element of the list. Set it to null to leave the role off altogether.",
        },
        new()
        {
            Name = "RowTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The template to render each row. Without it each item is rendered as its own text.",
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

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "LoadMoreAsync",
            Type = "Task",
            Description = "Loads the next page of the LoadMore mode, the same way clicking the LoadMore button does.",
        },
        new()
        {
            Name = "RefreshDataAsync",
            Type = "Task",
            Description = "Reloads the items of the list: a LoadMore list starts over from its first page, a virtualized provider list re-requests the region it shows, a plain provider list fetches the whole set again, and a plain list picks up the current contents of its Items collection.",
        },
        new()
        {
            Name = "ScrollToEndAsync",
            Type = "Task",
            Description = "Scrolls the list to its end. Pass true to animate the scrolling.",
        },
        new()
        {
            Name = "ScrollToIndexAsync",
            Type = "Task",
            Description = "Scrolls the list so that the item at the given index sits at its start edge. Pass true as the second argument to animate the scrolling.",
        },
        new()
        {
            Name = "ScrollToOffsetAsync",
            Type = "Task",
            Description = "Scrolls the list to an absolute offset in pixels on its scrolling axis. Pass true as the second argument to animate the scrolling.",
        },
        new()
        {
            Name = "ScrollToStartAsync",
            Type = "Task",
            Description = "Scrolls the list to its start. Pass true to animate the scrolling.",
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
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header container of the list.",
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer container of the list.",
                },
                new()
                {
                    Name = "LoadingContent",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the loading container of the list.",
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

    private readonly List<Person> emptyPeople = [];

    private readonly List<string> fruits = ["Apple", "Apricot", "Banana", "Cherry", "Fig", "Grape", "Lemon", "Mango", "Orange", "Peach"];

    private readonly List<Person> mutablePeople = [.. Enumerable.Range(0, 100).Select(i => new Person
    {
        Id = i + 1,
        FirstName = $"Person {i + 1}",
        LastName = $"Person Family {i + 1}",
        Job = $"Programmer {i + 1}"
    })];

    private bool isLoading;
    private int loadedCount;
    private BitBasicList<Person>? listRef;

    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;

    private BitBasicListItemsProvider<ProductDto> productsProvider = default!;
    private BitBasicListItemsProvider<CategoryOrProductDto> categoriesAndProductsProvider = default!;

    private BitBasicListItemsProvider<Person> loadMoreProvider = default!;
    private BitBasicListItemsProvider<Person> autoLoadProvider = default!;
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

                var url = NavManager.GetUriWithQueryParameters("api/Products/GetProducts", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

                return BitBasicListItemsProviderResult.From(data!.Items!, data!.TotalCount);
            }
            catch
            {
                return BitBasicListItemsProviderResult.Empty<ProductDto>();
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

                var url = NavManager.GetUriWithQueryParameters("api/Products/GetCategoriesAndProducts", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultCategoryOrProductDto);

                return BitBasicListItemsProviderResult.From(data!.Items!, data!.TotalCount);
            }
            catch
            {
                return BitBasicListItemsProviderResult.Empty<CategoryOrProductDto>();
            }
        };

        loadMoreProvider = async req =>
        {
            await Task.Delay(1000);

            return BitBasicListItemsProviderResult.From([.. fewPeople.Skip(req.StartIndex).Take(req.Count)], fewPeople.Count);
        };

        autoLoadProvider = async req =>
        {
            await Task.Delay(700);

            return BitBasicListItemsProviderResult.From([.. lotsOfPeople.Skip(req.StartIndex).Take(req.Count)], lotsOfPeople.Count);
        };

        loadMoreVirtualizeProvider = async req =>
        {
            await Task.Delay(500);

            return BitBasicListItemsProviderResult.From([.. lotsOfPeople.Skip(req.StartIndex).Take(req.Count)], lotsOfPeople.Count);
        };

        base.OnInitialized();
    }



    private async Task AddPerson()
    {
        var id = mutablePeople.Count + 1;

        mutablePeople.Add(new Person
        {
            Id = id,
            FirstName = $"Person {id}",
            LastName = $"Person Family {id}",
            Job = $"Programmer {id}"
        });

        // The collection instance itself did not change, so the list is told to pick up its new contents.
        if (listRef is not null)
        {
            await listRef.RefreshDataAsync();
            await listRef.ScrollToEndAsync(true);
        }
    }
}
