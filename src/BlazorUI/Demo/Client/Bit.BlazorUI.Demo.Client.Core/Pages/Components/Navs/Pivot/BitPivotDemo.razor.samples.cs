namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Pivot;

public partial class BitPivotDemo
{
    private readonly string example1RazorCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""File"">
        <h3>Pivot #1: File</h3>
        <div>Everything that has been saved to this workspace, newest first.</div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        <h3>Pivot #2: Shared with me</h3>
        <div>The files other people have given you access to, grouped by who shared them.</div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h3>Pivot #3: Recent</h3>
        <div>The documents you have opened over the last few days.</div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example2RazorCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""Files"" IconName=""@BitIconName.FabricFolder""><div>Pivot #1: Files</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"" ItemCount=""32""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent"" IconName=""@BitIconName.Recent"" ItemCount=""12""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Stacked>
    <BitPivotItem HeaderText=""Home"" IconName=""@BitIconName.Home""><div>Pivot #1: Home</div></BitPivotItem>
    <BitPivotItem HeaderText=""Files"" IconName=""@BitIconName.FabricFolder"" ItemCount=""8""><div>Pivot #2: Files</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent"" IconName=""@BitIconName.Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    <BitPivotItem HeaderText=""Settings"" IconName=""@BitIconName.Settings""><div>Pivot #4: Settings</div></BitPivotItem>
</BitPivot>";

    private readonly string example3RazorCode = @"
<BitPivot HeaderType=""BitPivotHeaderType.Link"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot HeaderType=""BitPivotHeaderType.Tab"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example4RazorCode = @"
<BitPivot Alignment=""BitAlignment.Center"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Alignment=""BitAlignment.SpaceBetween"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot FullWidth Gap=""4px"" HeaderType=""BitPivotHeaderType.Tab"">
    <BitPivotItem HeaderText=""Overview""><div>Pivot #1: Overview</div></BitPivotItem>
    <BitPivotItem HeaderText=""Activity""><div>Pivot #2: Activity</div></BitPivotItem>
    <BitPivotItem HeaderText=""Settings""><div>Pivot #3: Settings</div></BitPivotItem>
</BitPivot>

<BitPivot Gap=""2rem"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example5RazorCode = @"
<BitPivot Position=""BitPivotPosition.Bottom"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Position=""BitPivotPosition.Start"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Position=""BitPivotPosition.End"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example6RazorCode = @"
<BitPivot OverflowBehavior=""BitPivotOverflowBehavior.Menu"">
    @foreach (var tab in overflowTabs)
    {
        <BitPivotItem HeaderText=""@tab"">Content of the @tab tab.</BitPivotItem>
    }
</BitPivot>

<BitPivot OverflowBehavior=""BitPivotOverflowBehavior.Slide"">
    @foreach (var tab in overflowTabs)
    {
        <BitPivotItem HeaderText=""@tab"">Content of the @tab tab.</BitPivotItem>
    }
</BitPivot>

<BitPivot OverflowBehavior=""BitPivotOverflowBehavior.Scroll"">
    @foreach (var tab in overflowTabs)
    {
        <BitPivotItem HeaderText=""@tab"">Content of the @tab tab.</BitPivotItem>
    }
</BitPivot>

<BitPivot OverflowBehavior=""BitPivotOverflowBehavior.Wrap"">
    @foreach (var tab in overflowTabs)
    {
        <BitPivotItem HeaderText=""@tab"">Content of the @tab tab.</BitPivotItem>
    }
</BitPivot>

<BitPivot AutoHideSlideButtons OverflowBehavior=""BitPivotOverflowBehavior.Slide"">
    <BitPivotItem HeaderText=""File"">Content of the File tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Shared"">Content of the Shared tab.</BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">Content of the Recent tab.</BitPivotItem>
</BitPivot>

<BitPivot Position=""BitPivotPosition.Start"" OverflowBehavior=""BitPivotOverflowBehavior.Menu"" Style=""height:200px"">
    @foreach (var tab in overflowTabs)
    {
        <BitPivotItem HeaderText=""@tab"">Content of the @tab tab.</BitPivotItem>
    }
</BitPivot>

<BitPivot Position=""BitPivotPosition.Start"" OverflowBehavior=""BitPivotOverflowBehavior.Slide"" Style=""height:200px"">
    @foreach (var tab in overflowTabs)
    {
        <BitPivotItem HeaderText=""@tab"">Content of the @tab tab.</BitPivotItem>
    }
</BitPivot>";
    private readonly string example6CsharpCode = @"
private readonly List<string> overflowTabs = [""File"", ""Shared with me"", ""Recent"", ""Favorites"", ""Documents"", ""Pictures"", ""Downloads""];";

    private readonly string example7RazorCode = @"
<BitPivot Alignment=""BitAlignment.Center"">
    <HeaderStart>
        <BitIcon IconName=""@BitIconName.FabricFolder"" />
    </HeaderStart>
    <HeaderEnd>
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.Refresh"" Title=""Refresh"" />
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.Settings"" Title=""Settings"" />
    </HeaderEnd>
    <ChildContent>
        <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
        <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
        <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    </ChildContent>
</BitPivot>";

    private readonly string example8RazorCode = @"
<BitPivot>
    <BitPivotItem>
        <Header>
            <span style=""color:red"">Header #1</span>
        </Header>
        <Body>
            <div>Pivot #1: given through the Body template.</div>
        </Body>
    </BitPivotItem>
    <BitPivotItem IconName=""@BitIconName.Inbox"" ItemCount=""99"">
        <Header>
            <span style=""color:blue"">Header #2</span>
            <i style=""color:green"" class=""bit-icon bit-icon--HeartFill""></i>
        </Header>
        <Body>
            <div>Pivot #2: a custom header keeps its icon and count.</div>
        </Body>
    </BitPivotItem>
</BitPivot>";

    private readonly string example9RazorCode = @"
<BitPivot @bind-SelectedKey=""selectedKey"">
    <BitPivotItem Key=""1"" HeaderText=""Samples""><div>Pivot #1: Samples</div></BitPivotItem>
    <BitPivotItem Key=""2"" HeaderText=""Files""><div>Pivot #2: Files</div></BitPivotItem>
    <BitPivotItem Key=""3"" HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    <BitPivotItem Key=""4"" HeaderText=""Last""><div>Pivot #4: Last</div></BitPivotItem>
</BitPivot>

<BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.CaretSolidLeft"" IsEnabled=""@(selectedKey != ""1"")""
           OnClick=""(() => selectedKey = (int.Parse(selectedKey) - 1).ToString())"">
    Prev
</BitButton>
<BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.CaretSolidRight"" IsEnabled=""@(selectedKey != ""4"")""
           OnClick=""(() => selectedKey = (int.Parse(selectedKey) + 1).ToString())"">
    Next
</BitButton>

<div>Selected key: <b>@selectedKey</b></div>


<BitPivot DefaultSelectedKey=""B"">
    <BitPivotItem Key=""A"" HeaderText=""A""><div>Pivot #1: A</div></BitPivotItem>
    <BitPivotItem Key=""B"" HeaderText=""B""><div>Pivot #2: B (starts here)</div></BitPivotItem>
    <BitPivotItem Key=""C"" HeaderText=""C""><div>Pivot #3: C</div></BitPivotItem>
</BitPivot>";
    private readonly string example9CsharpCode = @"
private string selectedKey = ""1"";";

    private readonly string example10RazorCode = @"
<BitPivot OnChange=""@(item => changedPivotItem = item)"" OnItemClick=""@(item => clickedPivotItem = item)"">
    <BitPivotItem HeaderText=""Foo"" Title=""The first tab""><div>Pivot #1: Foo</div></BitPivotItem>
    <BitPivotItem HeaderText=""Bar"" Title=""The second tab""><div>Pivot #2: Bar</div></BitPivotItem>
    <BitPivotItem HeaderText=""Biz"" OnClick=""@(() => itemClickCount++)""><div>Pivot #3: Biz (counts its own clicks)</div></BitPivotItem>
</BitPivot>

<div>Last changed to: <b>@changedPivotItem?.HeaderText</b></div>
<div>Last header clicked: <b>@clickedPivotItem?.HeaderText</b></div>
<div>Clicks on the Biz header: <b>@itemClickCount</b></div>


<BitToggle @bind-Value=""lockHistoryTab"" Label=""Keep the History tab from being selected"" />

<BitPivot OnChanging=""HandleChanging"">
    <BitPivotItem HeaderText=""Draft""><div>Pivot #1: Draft</div></BitPivotItem>
    <BitPivotItem HeaderText=""Preview""><div>Pivot #2: Preview</div></BitPivotItem>
    <BitPivotItem HeaderText=""History""><div>Pivot #3: History</div></BitPivotItem>
</BitPivot>

<div>Last refused: <b>@refusedPivotItem?.HeaderText</b></div>";
    private readonly string example10CsharpCode = @"
private BitPivotItem? changedPivotItem;
private BitPivotItem? clickedPivotItem;
private int itemClickCount;
private bool lockHistoryTab = true;
private BitPivotItem? refusedPivotItem;

private void HandleChanging(BitPivotChangeArgs args)
{
    refusedPivotItem = null;

    if (lockHistoryTab is false || args.Item.HeaderText != ""History"") return;

    args.Cancel = true;
    refusedPivotItem = args.Item;
}";

    private readonly string example11RazorCode = @"
<BitPivot HeaderOnly @bind-SelectedKey=""detachedSelectedKey"">
    <BitPivotItem HeaderText=""Foo"" Key=""Foo"" />
    <BitPivotItem HeaderText=""Bar"" Key=""Bar"" />
    <BitPivotItem HeaderText=""Biz"" Key=""Biz"" />
</BitPivot>

<div class=""box"">
    @switch (detachedSelectedKey)
    {
        case ""Foo"":
            <div>Hello, I am Foo.</div>
            break;
        case ""Bar"":
            <div>Hello, I am Bar.</div>
            break;
        case ""Biz"":
            <div>Hello, I am Biz.</div>
            break;
    }
</div>";
    private readonly string example11CsharpCode = @"
private string? detachedSelectedKey = ""Foo"";";

    private readonly string example12RazorCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""First""><input aria-label=""First"" placeholder=""Type here..."" /></BitPivotItem>
    <BitPivotItem HeaderText=""Second""><input aria-label=""Second"" placeholder=""Type here..."" /></BitPivotItem>
</BitPivot>

<BitPivot KeepMounted>
    <BitPivotItem HeaderText=""First""><input aria-label=""First"" placeholder=""Type here..."" /></BitPivotItem>
    <BitPivotItem HeaderText=""Second""><input aria-label=""Second"" placeholder=""Type here..."" /></BitPivotItem>
</BitPivot>

<BitPivot MountAll>
    <BitPivotItem HeaderText=""First""><input aria-label=""First"" placeholder=""Type here..."" /></BitPivotItem>
    <BitPivotItem HeaderText=""Second""><input aria-label=""Second"" placeholder=""Type here..."" /></BitPivotItem>
</BitPivot>";

    private readonly string example13RazorCode = @"
<BitPivot Addable Dismissible
          @bind-SelectedKey=""editableSelectedKey""
          OnAdd=""AddTab""
          OnItemDismiss=""@(item => editableTabs.Remove(item.Key!))"">
    <BitPivotItem Key=""Home"" HeaderText=""Home"" Dismissible=""false"">
        <div>Home stays: its own Dismissible is false.</div>
    </BitPivotItem>
    @foreach (var tab in editableTabs)
    {
        <BitPivotItem @key=""tab"" Key=""@tab"" HeaderText=""@tab"">
            <div>Content of @tab.</div>
        </BitPivotItem>
    }
</BitPivot>

<div>Selected key: <b>@editableSelectedKey</b></div>";
    private readonly string example13CsharpCode = @"
private int editableTabCount = 2;
private string? editableSelectedKey = ""Home"";
private readonly List<string> editableTabs = [""Tab 1"", ""Tab 2""];

private void AddTab()
{
    var key = $""Tab {++editableTabCount}"";

    editableTabs.Add(key);
    editableSelectedKey = key;
}";

    private readonly string example14RazorCode = @"
<BitPivot Reorderable OnItemReorder=""HandleReorder"">
    @foreach (var tab in reorderableTabs)
    {
        <BitPivotItem @key=""tab"" Key=""@tab"" HeaderText=""@tab"">
            <div>Content of the @tab tab.</div>
        </BitPivotItem>
    }
    <BitPivotItem Key=""Pinned"" HeaderText=""Pinned"" Reorderable=""false"" IconName=""@BitIconName.Pinned"">
        <div>This tab stays where it is.</div>
    </BitPivotItem>
</BitPivot>";
    private readonly string example14CsharpCode = @"
private readonly List<string> reorderableTabs = [""File"", ""Shared"", ""Recent"", ""Favorites""];

private void HandleReorder(BitPivotReorderEventArgs args)
{
    var oldIndex = reorderableTabs.IndexOf(args.Item.Key!);
    var newIndex = args.NewIndex;

    if (oldIndex < 0 || newIndex < 0 || newIndex >= reorderableTabs.Count) return;

    reorderableTabs.RemoveAt(oldIndex);
    reorderableTabs.Insert(newIndex, args.Item.Key!);
}";

    private readonly string example15RazorCode = @"
<div id=""pivot-settings-label""><b>Account settings</b></div>

<BitPivot AriaLabelledBy=""pivot-settings-label"" SelectOnFocus>
    <BitPivotItem HeaderText=""Profile""><div>Pivot #1: Profile</div></BitPivotItem>
    <BitPivotItem HeaderText=""Security""><div>Pivot #2: Security</div></BitPivotItem>
    <BitPivotItem HeaderText=""Billing""><div>Pivot #3: Billing</div></BitPivotItem>
</BitPivot>

<BitPivot AriaLabel=""Documents"" Loop=""false"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot AriaLabel=""Quick links"" Navigable=""false"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example16RazorCode = @"
<BitPivot IsEnabled=""false"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot>
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared"" IsEnabled=""false""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot HeaderType=""BitPivotHeaderType.Tab"" IsEnabled=""false"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example17RazorCode = @"
<BitParams Parameters=""@pivotParams"">
    <BitPivot>
        <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
        <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
        <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    </BitPivot>

    <BitPivot Color=""BitColor.Tertiary"">
        <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
        <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
        <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    </BitPivot>
</BitParams>";
    private readonly string example17CsharpCode = @"
private readonly BitPivotParams[] pivotParams =
[
    new()
    {
        HeaderType = BitPivotHeaderType.Tab,
        Color = BitColor.Success,
        Size = BitSize.Small,
    }
];";

    private readonly string example18RazorCode = @"
<BitPivot Color=""BitColor.Primary"">
    <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Color=""BitColor.Secondary"">...</BitPivot>
<BitPivot Color=""BitColor.Tertiary"">...</BitPivot>
<BitPivot Color=""BitColor.Info"">...</BitPivot>
<BitPivot Color=""BitColor.Success"">...</BitPivot>
<BitPivot Color=""BitColor.Warning"">...</BitPivot>
<BitPivot Color=""BitColor.SevereWarning"">...</BitPivot>
<BitPivot Color=""BitColor.Error"">...</BitPivot>

<div class=""inverted-panel"" style=""background:var(--bit-clr-fg-sec)"">
    <BitPivot Color=""BitColor.PrimaryBackground"">...</BitPivot>
</div>
<div class=""inverted-panel"" style=""background:var(--bit-clr-fg-sec)"">
    <BitPivot Color=""BitColor.SecondaryBackground"">...</BitPivot>
</div>
<div class=""inverted-panel"" style=""background:var(--bit-clr-fg-sec)"">
    <BitPivot Color=""BitColor.TertiaryBackground"">...</BitPivot>
</div>

<BitPivot Color=""BitColor.PrimaryForeground"">...</BitPivot>
<BitPivot Color=""BitColor.SecondaryForeground"">...</BitPivot>
<BitPivot Color=""BitColor.TertiaryForeground"">...</BitPivot>

<BitPivot Color=""BitColor.PrimaryBorder"">...</BitPivot>
<BitPivot Color=""BitColor.SecondaryBorder"">...</BitPivot>
<BitPivot Color=""BitColor.TertiaryBorder"">...</BitPivot>

<BitPivot HeaderType=""BitPivotHeaderType.Tab"" Color=""BitColor.Primary"">...</BitPivot>
<BitPivot HeaderType=""BitPivotHeaderType.Tab"" Color=""BitColor.Success"">...</BitPivot>
<BitPivot HeaderType=""BitPivotHeaderType.Tab"" Color=""BitColor.Error"">...</BitPivot>";

    private readonly string example19RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitPivot>
    <BitPivotItem HeaderText=""Home"" Icon=""@(""fa-solid fa-house"")""><div>Pivot #1: Home</div></BitPivotItem>
    <BitPivotItem HeaderText=""Heart"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")""><div>Pivot #2: Heart</div></BitPivotItem>
    <BitPivotItem HeaderText=""Rocket"" Icon=""@BitIconInfo.Fa(""solid rocket"")""><div>Pivot #3: Rocket</div></BitPivotItem>
</BitPivot>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitPivot>
    <BitPivotItem HeaderText=""Home"" Icon=""@(""bi bi-house-fill"")""><div>Pivot #1: Home</div></BitPivotItem>
    <BitPivotItem HeaderText=""Heart"" Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")""><div>Pivot #2: Heart</div></BitPivotItem>
    <BitPivotItem HeaderText=""Gear"" Icon=""@BitIconInfo.Bi(""gear-fill"")""><div>Pivot #3: Gear</div></BitPivotItem>
</BitPivot>";

    private readonly string example20RazorCode = @"
<BitPivot Size=""BitSize.Small"">
    <BitPivotItem HeaderText=""File"" IconName=""@BitIconName.FabricFolder""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared"" ItemCount=""32""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Size=""BitSize.Medium"">
    <BitPivotItem HeaderText=""File"" IconName=""@BitIconName.FabricFolder""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared"" ItemCount=""32""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>

<BitPivot Size=""BitSize.Large"">
    <BitPivotItem HeaderText=""File"" IconName=""@BitIconName.FabricFolder""><div>Pivot #1: File</div></BitPivotItem>
    <BitPivotItem HeaderText=""Shared"" ItemCount=""32""><div>Pivot #2: Shared</div></BitPivotItem>
    <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
</BitPivot>";

    private readonly string example21RazorCode = @"
<div class=""pivot-custom"">
    <BitPivot Style=""border: 1px solid tomato;"">
        <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
        <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
        <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    </BitPivot>

    <BitPivot Class=""custom-class"">
        <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
        <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
        <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    </BitPivot>


    <BitPivot Styles=""@(new() { HeaderIcon = ""color: tomato;"", HeaderText = ""color: purple;"", HeaderItemCount = ""color: gray;"" })"">
        <BitPivotItem HeaderText=""File"" IconName=""@BitIconName.FabricFolder""><div>Pivot #1: File</div></BitPivotItem>
        <BitPivotItem HeaderText=""Shared with me"" ItemCount=""32""><div>Pivot #2: Shared with me</div></BitPivotItem>
        <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    </BitPivot>

    <BitPivot Classes=""@(new() { Body = ""custom-body"", SelectedItem = ""custom-selected-item"", Header = ""custom-header"" })"">
        <BitPivotItem HeaderText=""File""><div>Pivot #1: File</div></BitPivotItem>
        <BitPivotItem HeaderText=""Shared with me""><div>Pivot #2: Shared with me</div></BitPivotItem>
        <BitPivotItem HeaderText=""Recent""><div>Pivot #3: Recent</div></BitPivotItem>
    </BitPivot>
</div>


<BitPivot HeaderType=""BitPivotHeaderType.Tab""
          Style=""--bit-Pivot-color: seagreen;
                 --bit-Pivot-focus-color: seagreen;
                 --bit-Pivot-item-radius: 999px;
                 --bit-Pivot-item-padding-inline: 1rem;
                 --bit-Pivot-gap: 0.25rem;
                 --bit-Pivot-body-padding: 1rem 0.5rem;"">
    <BitPivotItem HeaderText=""Day""><div>Pivot #1: Day</div></BitPivotItem>
    <BitPivotItem HeaderText=""Week""><div>Pivot #2: Week</div></BitPivotItem>
    <BitPivotItem HeaderText=""Month""><div>Pivot #3: Month</div></BitPivotItem>
</BitPivot>

<BitPivot Style=""--bit-Pivot-color: tomato;
                 --bit-Pivot-item-selected-color: tomato;
                 --bit-Pivot-indicator-thickness: 4px;
                 --bit-Pivot-indicator-inset: 0;
                 --bit-Pivot-indicator-radius: 4px 4px 0 0;
                 --bit-Pivot-item-hover-background: transparent;"">
    <BitPivotItem HeaderText=""Overview""><div>Pivot #1: Overview</div></BitPivotItem>
    <BitPivotItem HeaderText=""Activity""><div>Pivot #2: Activity</div></BitPivotItem>
    <BitPivotItem HeaderText=""Settings""><div>Pivot #3: Settings</div></BitPivotItem>
</BitPivot>";
    private const string example21ScssCode = @"
.pivot-custom ::deep {
    .custom-class {
        margin: 1rem;
        padding-left: 0.25rem;
        box-shadow: 0 0 1rem lightskyblue;
    }

    .custom-selected-item {
        background-color: goldenrod;
    }

    .custom-header {
        overflow: hidden;
        border-radius: 1rem;
        border: 1px solid gray;
    }

    .custom-body {
        padding: 0.5rem;
        background-color: deepskyblue;
    }
}";
    private readonly DemoCodeFile[] example21CodeFiles =
    [
        new("BitPivotDemo.razor.scss", example21ScssCode),
    ];

    private readonly string example22RazorCode = @"
<BitPivot Dir=""BitDir.Rtl"" OverflowBehavior=""BitPivotOverflowBehavior.Scroll"">
    <BitPivotItem HeaderText=""اسناد"" IconName=""@BitIconName.FabricFolder"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    </BitPivotItem>
    <BitPivotItem HeaderText=""آخرین ها"" ItemCount=""8"">
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""شخصی"" IconName=""@BitIconName.Info"" ItemCount=""6"">
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها به پایان رسد.
    </BitPivotItem>
</BitPivot>

<BitPivot Dir=""BitDir.Rtl"" Position=""BitPivotPosition.Start"">
    <BitPivotItem HeaderText=""اسناد"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    </BitPivotItem>
    <BitPivotItem HeaderText=""آخرین ها"">
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""شخصی"">
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها به پایان رسد.
    </BitPivotItem>
</BitPivot>";
}
