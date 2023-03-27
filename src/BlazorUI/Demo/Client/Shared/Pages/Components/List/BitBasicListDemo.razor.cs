using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.List;

public partial class BitBasicListDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "EnableVirtualization",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables virtualization in rendering the list.",
        },
        new()
        {
            Name = "Items",
            Type = "ICollection<TItem>",
            DefaultValue = "",
            Description = "Gets or sets the list of items to render.",
        },
        new()
        {
            Name = "ItemSize",
            Type = "int",
            DefaultValue = "50",
            Description = "Gets the size of each item in pixels. Defaults to 50px.",
        },
        new()
        {
            Name = "OverscanCount",
            Type = "int",
            DefaultValue = "3",
            Description = "Gets or sets a value that determines how many additional items will be rendered before and after the visible region.",
        },
        new()
        {
            Name = "Role",
            Type = "string",
            DefaultValue = "list",
            Description = "Gets or set the role attribute of the BasicList html element.",
        },
        new()
        {
            Name = "RowTemplate",
            Type = "RenderFragment<TItem>",
            DefaultValue = "",
            Description = "Gets or sets the Template to render each row.",
        }
    };

    private string example1HTMLCode = @"
<BitBasicList Items=""People1"" EnableVirtualization=""true"" Style=""border: 1px #a19f9d solid; border-radius: 3px; "">
    <RowTemplate Context=""person"">
        <div @key=""person.Id"" style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            <img src=""https://picsum.photos/100/100?random=@(person.Id)"">
            <div style=""margin-left:3%; display: inline-block;"">
                <p>Id: <strong>@person.Id</strong></p>
                <p>Full Name: <strong>@person.FirstName @person.LastName</strong></p>
                <p>Job: <strong>@person.Job</strong></p>
            </div>
        </div>
    </RowTemplate>
</BitBasicList>";
    private string example1CSharpCode = @"
List<Person> People = new();
protected override void OnInitialized()
{
    People = GetPeople(8000);    
    base.OnInitialized();
}

private static List<Person> GetPeople(int itemCount)
{
    List<Person> people = new();

    for (int i = 0; i < itemCount; i++)
    {
        people.Add(new Person
        {
            Id = i + 1,
            FirstName = $""Person {i + 1}"",
            LastName = $""Person Family {i + 1}"",
            Job = $""Programmer {i + 1}""
        });
    }

    return people;
}

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example2HTMLCode = @"
<BitBasicList Items=""People2"" EnableVirtualization=""false"" Style=""border: 1px #a19f9d solid; border-radius: 3px; "">
    <RowTemplate Context=""person"">
        <div style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            <img src=""https://picsum.photos/100/100?random=@(person.Id)"">
            <p>Id: <strong>@person.Id</strong></p>
            <p>Full Name: <strong>@person.FirstName @person.LastName</strong></p>
            <p>Job: <strong>@person.Job</strong></p>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example2CSharpCode = @"
List<Person> People = new();
protected override void OnInitialized()
{
    People = GetPeople(100);    
    base.OnInitialized();
}

private List<Person> GetPeople(int itemCount)
{
    List<Person> people = new();

    for (int i = 0; i < itemCount; i++)
    {
        people.Add(new Person
        {
            Id = i + 1,
            FirstName = $""Person {i + 1}"",
            LastName = $""Person Family {i + 1}"",
            Job = $""Programmer {i + 1}""
        });
    }

    return people;
}

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example3HTMLCode = @"
<style>
    .list-item {
        padding: 16px 20px;
        background-color: #f2f2f2;
        margin: 10px 10px;
        width: 20%;
        height: 143px;
        display: inline-grid;
        justify-content: center;
        align-items: center;
    }
</style>
<BitBasicList Items=""People1"" EnableVirtualization=""true"" Role=""list"" Style=""border: 1px #a19f9d solid; border-radius: 3px;"">
    <RowTemplate Context=""person"">
        <div class=""list-item"">
            <span>Id: <strong>@person.Id</strong></span>
            <span>Full Name: <strong>@person.FirstName</strong></span>
            <span>Job: <strong>@person.Job</strong></span>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example3CSharpCode = @"
List<Person> People = new();
protected override void OnInitialized()
{
    People = GetPeople(8000);    
    base.OnInitialized();
}

private static List<Person> GetPeople(int itemCount)
{
    List<Person> people = new();

    for (int i = 0; i < itemCount; i++)
    {
        people.Add(new Person
        {
            Id = i + 1,
            FirstName = $""Person {i + 1}"",
            LastName = $""Person Family {i + 1}"",
            Job = $""Programmer {i + 1}""
        });
    }

    return people;
}

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example4HTMLCode = @"
<BitBasicList Items=""People1"" EnableVirtualization=""true"" OverscanCount=""5"" ItemSize=""300"" Style=""border: 1px #a19f9d solid; border-radius: 3px; "">
    <RowTemplate Context=""person"">
        <div style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            <p>Id: <strong>@person.Id</strong></p>
            <p>Full Name: <strong>@person.FirstName @person.LastName</strong></p>
            <p>Job: <strong>@person.Job</strong></p>
        </div>
    </RowTemplate>
</BitBasicList>";
    private readonly string example4CSharpCode = @"
List<Person> People = new();
protected override void OnInitialized()
{
    People = GetPeople(8000);    
    base.OnInitialized();
}

private static List<Person> GetPeople(int itemCount)
{
    List<Person> people = new();

    for (int i = 0; i < itemCount; i++)
    {
        people.Add(new Person
        {
            Id = i + 1,
            FirstName = $""Person {i + 1}"",
            LastName = $""Person Family {i + 1}"",
            Job = $""Programmer {i + 1}""
        });
    }

    return people;
}

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Job { get; set; }
}";

    private readonly string example5HTMLCode = @"
<BitBasicList EnableVirtualization=""true"" TItem=""FoodRecall"" ItemsProvider=""@foodRecallProvider"" @ref=""basicList"" ItemSize=""85"" Style=""border: 1px #a19f9d solid; border-radius: 3px; "">
    <RowTemplate Context=""context"">
        <div style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            <div>Id: <strong>@context.item.EventId</strong></div>
            <div>State: <strong>@context.item.State</strong></div>
            <div>City: <strong>@context.item.City</strong></div>
        </div>
    </RowTemplate>
    <VirtualizePlaceholder>
        <div style=""border-bottom: 1px #8a8886 solid; padding: 5px 20px; margin: 10px;"">
            <div>Id: <strong>...</strong></div>
            <div>State: <strong>...</strong></div>
            <div>City: <strong>...</strong></div>
        </div>
    </VirtualizePlaceholder>
</BitBasicList>";
    private readonly string example5CSharpCode = @"
private BitBasicList<FoodRecall> basicList;
private BitBasicListItemsProvider<FoodRecall> foodRecallProvider;

protected override void OnInitialized()
{
    foodRecallProvider = async req =>
    {
        try
        {
            var query = new Dictionary<string, object?>
                    {
                        { ""skip"", req.StartIndex },
                        { ""limit"", req.Count }
                    };

            var url = NavManager.GetUriWithQueryParameters(""https://api.fda.gov/food/enforcement.json"", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.FoodRecallQueryResult, req.CancellationToken);

            return BitBasicListItemsProviderResult.From(data!.Results, data!.Meta.Results.Total);
        }
        catch
        {
            return BitBasicListItemsProviderResult.From<FoodRecall>(new List<FoodRecall> { }, 0);
        }
    };

    base.OnInitialized();
}
";

}
