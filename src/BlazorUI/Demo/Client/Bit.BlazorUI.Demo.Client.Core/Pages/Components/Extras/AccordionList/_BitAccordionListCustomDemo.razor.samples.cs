namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListCustomDemo
{
    private readonly string example1RazorCode = @"
<BitAccordionList Items=""basicSections"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example1CsharpCode = @"
public class Section
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Info { get; set; }
    public bool Open { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Section>? Content { get; set; }
    public Action<Section>? Clicked { get; set; }
}

private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsExpanded = { Selector = i => i.Open },
    IsEnabled = { Selector = i => i.IsEnabled },
    Body = { Selector = i => i.Content },
};

private readonly List<Section> basicSections =
[
    new() { Id = ""general"", Name = ""General settings"", Info = ""The general settings"", Content = BodyFor(""..."") },
    new() { Id = ""users"", Name = ""Users"", Info = ""You are currently not an owner"", Content = BodyFor(""..."") },
    new() { Id = ""advanced"", Name = ""Advanced settings"", Info = ""Be careful here"", Content = BodyFor(""..."") },
];

private static RenderFragment<Section> BodyFor(string? text) => section => builder => builder.AddContent(0, text);";

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple Items=""basicSections"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example2CsharpCode = @"
private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Title = { Selector = i => i.Name },
    Description = { Selector = i => i.Info },
    IsExpanded = { Selector = i => i.Open },
    IsEnabled = { Selector = i => i.IsEnabled },
    Body = { Selector = i => i.Content },
};";

    private readonly string example3RazorCode = @"
<BitAccordionList Items=""basicSections"" TItem=""Section"" NameSelectors=""nameSelectors"" DefaultExpandedKey=""users"" />

<BitAccordionList Multiple Items=""basicSections"" TItem=""Section"" NameSelectors=""nameSelectors"" DefaultExpandedKeys=""@([""general"", ""advanced""])"" />";
    private readonly string example3CsharpCode = @"
private readonly List<Section> basicSections =
[
    new() { Id = ""general"", Name = ""General settings"", Info = ""The general settings"", Content = BodyFor(""..."") },
    new() { Id = ""users"", Name = ""Users"", Info = ""You are currently not an owner"", Content = BodyFor(""..."") },
    new() { Id = ""advanced"", Name = ""Advanced settings"", Info = ""Be careful here"", Content = BodyFor(""..."") },
];";

    private readonly string example4RazorCode = @"
<BitAccordionList Items=""basicSections""
                  TItem=""Section""
                  NameSelectors=""nameSelectors""
                  OnExpand=""(Section item) => expandedName = item.Name""
                  OnCollapse=""(Section item) => collapsedName = item.Name""
                  OnToggle=""(Section item) => toggledName = item.Name"" />

<div>Last expanded: <b>@expandedName</b></div>
<div>Last collapsed: <b>@collapsedName</b></div>
<div>Last toggled: <b>@toggledName</b></div>

<BitAccordionList Items=""eventsSections""
                  TItem=""Section""
                  NameSelectors=""@(new() { Key = { Selector = i => i.Id },
                                           Title = { Selector = i => i.Name },
                                           Body = { Selector = i => i.Content },
                                           OnClick = { Selector = i => i.Clicked } })"" />
<div>Item click count: <b>@clickCounter</b></div>";
    private readonly string example4CsharpCode = @"
private int clickCounter;
private string? expandedName;
private string? collapsedName;
private string? toggledName;

private List<Section> eventsSections =
[
    new() { Id = ""s1"", Name = ""Section 1"", Content = BodyFor(""..."") },
    new() { Id = ""s2"", Name = ""Section 2"", Content = BodyFor(""..."") },
    new() { Id = ""s3"", Name = ""Section 3"", Content = BodyFor(""..."") },
];

protected override void OnInitialized()
{
    foreach (var section in eventsSections)
    {
        section.Clicked = _ => { clickCounter++; StateHasChanged(); };
    }
}";

    private readonly string example5RazorCode = @"
<BitButtonGroup Items=""bindingButtons"" TItem=""BitButtonGroupItem"" />

<div>Bound expanded key: <b>@boundExpandedKey</b></div>

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" Items=""basicSections"" TItem=""Section"" NameSelectors=""nameSelectors"" />";
    private readonly string example5CsharpCode = @"
private string? boundExpandedKey = ""users"";

private List<BitButtonGroupItem> bindingButtons =>
[
    new() { Text = ""General"", OnClick = _ => boundExpandedKey = ""general"" },
    new() { Text = ""Users"", OnClick = _ => boundExpandedKey = ""users"" },
    new() { Text = ""Advanced"", OnClick = _ => boundExpandedKey = ""advanced"" },
    new() { Text = ""None"", OnClick = _ => boundExpandedKey = null },
];";

    private readonly string example6RazorCode = @"
<BitAccordionList Items=""iconSections""
                  TItem=""Section""
                  NameSelectors=""@(new() { Key = { Selector = i => i.Id },
                                           Title = { Selector = i => i.Name },
                                           Body = { Selector = i => i.Content },
                                           ExpanderIconName = { Selector = i => i.Image } })"" />";
    private readonly string example6CsharpCode = @"
private readonly List<Section> iconSections =
[
    new() { Id = ""profile"", Name = ""Profile"", Image = BitIconName.Contact, Content = BodyFor(""..."") },
    new() { Id = ""settings"", Name = ""Settings"", Image = BitIconName.Settings, Content = BodyFor(""..."") },
    new() { Id = ""notifications"", Name = ""Notifications"", Image = BitIconName.Ringer, Content = BodyFor(""..."") },
];";

    private readonly string example7RazorCode = @"
<BitAccordionList Items=""styleClassSections""
                  TItem=""Section""
                  NameSelectors=""@(new() { Key = { Selector = i => i.Id },
                                           Title = { Selector = i => i.Name },
                                           Body = { Selector = i => i.Content },
                                           Style = { Selector = i => i.Style },
                                           Class = { Selector = i => i.Class } })"" />";
    private readonly string example7CsharpCode = @"
private readonly List<Section> styleClassSections =
[
    new() { Id = ""styled"", Name = ""Styled"", Style = ""color: tomato;"", Content = BodyFor(""..."") },
    new() { Id = ""classed"", Name = ""Classed"", Class = ""custom-item"", Content = BodyFor(""..."") },
];";
}
