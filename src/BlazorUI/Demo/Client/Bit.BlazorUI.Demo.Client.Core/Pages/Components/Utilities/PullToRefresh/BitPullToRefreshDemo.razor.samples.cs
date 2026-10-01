namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.PullToRefresh;

public partial class BitPullToRefreshDemo
{
    private readonly string example1RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<BitPullToRefresh OnRefresh=""HandleOnRefreshBasic"">
    <div class=""anchor"">
        @foreach (var (idx, i) in basicItems)
        {
            <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
        }
    </div>
</BitPullToRefresh>";
    private readonly string example1CsharpCode = @"
private (int, int)[] basicItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshBasic()
{
    await Task.Delay(2000);
    basicItems = GenerateRandomNumbers(1, 51);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example2RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<div style=""display:flex; gap:1rem;"">
    <BitPullToRefresh OnRefresh=""HandleOnRefreshBehavior""
                      Trigger=""(int)trigger""
                      Factor=""(decimal)factor""
                      Margin=""(int)margin""
                      Threshold=""(int)threshold""
                      MaxPull=""(int)maxPull"">
        <div class=""anchor"">
            @foreach (var (idx, i) in behaviorItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
            }
        </div>
    </BitPullToRefresh>
    <div>
        <BitSlider Label=""Trigger"" Min=""40"" Max=""160"" @bind-Value=""trigger"" />
        <br />
        <BitSlider Label=""Factor"" Min=""1"" Max=""4"" Step=""0.1"" @bind-Value=""factor"" />
        <br />
        <BitSlider Label=""Margin"" Min=""0"" Max=""60"" @bind-Value=""margin"" />
        <br />
        <BitSlider Label=""Threshold"" Min=""0"" Max=""60"" @bind-Value=""threshold"" />
        <br />
        <BitSlider Label=""MaxPull"" Min=""0"" Max=""200"" @bind-Value=""maxPull"" />
    </div>
</div>";
    private readonly string example2CsharpCode = @"
private double trigger = 80;
private double factor = 1.5;
private double margin = 30;
private double threshold = 0;
private double maxPull = 0;
private (int, int)[] behaviorItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshBehavior()
{
    await Task.Delay(2000);
    behaviorItems = GenerateRandomNumbers(1, 51);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example3RazorCode = @"
<style>
    .mobile-frame {
        width: 300px;
        height: 600px;
        overflow: hidden;
        position: relative;
        border-radius: 36px;
        background-color: #fff;
        border: 16px solid #333;
        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
    }

    .mobile-frame .screen {
        width: 100%;
        height: 100%;
    }

    .scroller {
        cursor: grab;
        height: 490px;
        overflow: auto;
        user-select: none;
    }

    .row {
        color: black;
        padding: 10px;
        border-bottom: 1px solid rgba(0, 0, 0, 0.1);
    }
</style>

<div class=""mobile-frame"">
    <div class=""screen"">
        <BitLayout>
            <Header>
                <BitCard FullWidth>
                    <BitStack Horizontal HorizontalAlign=""BitAlignment.Center"" VerticalAlign=""BitAlignment.Center"">
                        <BitImage Src=""/images/bit-logo.svg"" Width=""50"" />
                        <BitText Typography=""BitTypography.H4"" Color=""BitColor.Info"">
                            BlazorUI
                        </BitText>
                    </BitStack>
                </BitCard>
            </Header>
            <Main>
                <BitPullToRefresh OnRefresh=""HandleOnRefreshScroller"" ScrollerSelector="".scroller"" FullWidth>
                    <div class=""scroller"">
                        @foreach (var (idx, i) in scrollerItems)
                        {
                            <div class=""row"" @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
                        }
                    </div>
                </BitPullToRefresh>
            </Main>
        </BitLayout>
    </div>
</div>";
    private readonly string example3CsharpCode = @"
private (int, int)[] scrollerItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshScroller()
{
    await Task.Delay(2000);
    scrollerItems = GenerateRandomNumbers(1, 51);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example4RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<div style=""display:flex; gap:1rem;"">
    <BitPullToRefresh OnRefresh=""HandleOnRefreshComplete"" CompleteDelay=""1000"">
        <div class=""anchor"">
            @foreach (var (idx, i) in completeItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
            }
        </div>
    </BitPullToRefresh>

    <BitPullToRefresh OnRefresh=""HandleOnRefreshTemplates"" CompleteDelay=""1500"">
        <Anchor>
            <div class=""anchor"">
                @foreach (var (idx, i) in templateItems)
                {
                    <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
                }
            </div>
        </Anchor>
        <Loading>
            <svg viewBox=""0 0 490 490"" fill=""currentColor"" width=""100%"" height=""100%"">
                <path d=""M112.156,97.111c72.3-65.4,180.5-66.4,253.8-6.7l-58.1,2.2c-7.5,0.3-13.3,6.5-13,14c0.3,7.3,6.3,13,13.5,13 c0.2,0,0.3,0,0.5,0l89.2-3.3c7.3-0.3,13-6.2,13-13.5v-1c0-0.2,0-0.3,0-0.5v-0.1l0,0l-3.3-88.2c-0.3-7.5-6.6-13.3-14-13 c-7.5,0.3-13.3,6.5-13,14l2.1,55.3c-36.3-29.7-81-46.9-128.8-49.3c-59.2-3-116.1,17.3-160,57.1c-60.4,54.7-86,137.9-66.8,217.1 c1.5,6.2,7,10.3,13.1,10.3c1.1,0,2.1-0.1,3.2-0.4c7.2-1.8,11.7-9.1,9.9-16.3C36.656,218.211,59.056,145.111,112.156,97.111z"" />
                <path d=""M462.456,195.511c-1.8-7.2-9.1-11.7-16.3-9.9c-7.2,1.8-11.7,9.1-9.9,16.3c16.9,69.6-5.6,142.7-58.7,190.7 c-37.3,33.7-84.1,50.3-130.7,50.3c-44.5,0-88.9-15.1-124.7-44.9l58.8-5.3c7.4-0.7,12.9-7.2,12.2-14.7s-7.2-12.9-14.7-12.2l-88.9,8 c-7.4,0.7-12.9,7.2-12.2,14.7l8,88.9c0.6,7,6.5,12.3,13.4,12.3c0.4,0,0.8,0,1.2-0.1c7.4-0.7,12.9-7.2,12.2-14.7l-4.8-54.1 c36.3,29.4,80.8,46.5,128.3,48.9c3.8,0.2,7.6,0.3,11.3,0.3c55.1,0,107.5-20.2,148.7-57.4 C456.056,357.911,481.656,274.811,462.456,195.511z"" />
            </svg>
        </Loading>
        <Release>
            <svg viewBox=""0 0 24 24"" fill=""currentColor"" width=""100%"" height=""100%"">
                <path d=""M12 3a1 1 0 0 1 1 1v11.59l4.3-4.3a1 1 0 1 1 1.4 1.42l-6 6a1 1 0 0 1-1.4 0l-6-6a1 1 0 1 1 1.4-1.42l4.3 4.3V4a1 1 0 0 1 1-1z"" />
            </svg>
        </Release>
        <Complete>🎉</Complete>
    </BitPullToRefresh>
</div>";
    private readonly string example4CsharpCode = @"
private (int, int)[] completeItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshComplete()
{
    await Task.Delay(2000);
    completeItems = GenerateRandomNumbers(1, 51);
}

private (int, int)[] templateItems = GenerateRandomNumbers(51, 101);
private async Task HandleOnRefreshTemplates()
{
    await Task.Delay(2000);
    templateItems = GenerateRandomNumbers(51, 101);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example5RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<BitButton AutoLoading OnClick=""RefreshProgrammatically"">Refresh messages</BitButton>
<BitPullToRefresh @ref=""programmaticRef""
                  OnRefresh=""HandleOnRefreshProgrammatic""
                  CompleteDelay=""1000""
                  AriaLabel=""Messages""
                  ReleaseLabel=""Release to load new messages""
                  RefreshingLabel=""Loading new messages""
                  CompleteLabel=""Messages are up to date"">
    <div class=""anchor"">
        @foreach (var (idx, i) in programmaticItems)
        {
            <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Message @i</div>
        }
    </div>
</BitPullToRefresh>";
    private readonly string example5CsharpCode = @"
private BitPullToRefresh? programmaticRef;
private async Task RefreshProgrammatically()
{
    await programmaticRef!.RefreshAsync();
}

private (int, int)[] programmaticItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshProgrammatic()
{
    await Task.Delay(2000);
    programmaticItems = GenerateRandomNumbers(1, 51);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example6RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<div style=""display:flex; gap:1rem;"">
    <BitPullToRefresh @ref=""eventsRef""
                      CompleteDelay=""1000""
                      OnRefresh=""HandleOnRefreshEvents""
                      OnStateChange=""HandleOnStateChange""
                      OnPullStart=""HandleOnPullStart""
                      OnPullMove=""HandleOnPullMove""
                      OnPullEnd=""HandleOnPullEnd""
                      OnPullCancel=""HandleOnPullCancel"">
        <div class=""anchor"">
            @foreach (var (idx, i) in eventsItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
            }
        </div>
    </BitPullToRefresh>
    <div>
        <div>State: <b>@pullState</b></div>
        <div>PullStart: @(pullStartArgs is null ? ""-"" : $""top:{pullStartArgs.Top:F0}, left:{pullStartArgs.Left:F0}, width:{pullStartArgs.Width:F0}"")</div>
        <div>PullMove: @pullMoveDiff.ToString(""F1"") (@((eventsRef?.PullProgress ?? 0).ToString(""P0"")))</div>
        <div>PullEnd: @pullEndDiff.ToString(""F1"")</div>
        <div>PullCancel: @pullCancelDiff.ToString(""F1"")</div>
        <div>Refresh count: @refreshCount</div>
    </div>
</div>";
    private readonly string example6CsharpCode = @"
private BitPullToRefresh? eventsRef;
private BitPullToRefreshState pullState;
private int refreshCount;
private decimal pullMoveDiff;
private decimal pullEndDiff;
private decimal pullCancelDiff;
private BitPullToRefreshPullStartArgs? pullStartArgs;
private void HandleOnStateChange(BitPullToRefreshState state)
{
    pullState = state;
}
private void HandleOnPullStart(BitPullToRefreshPullStartArgs args)
{
    pullStartArgs = args;
}
private void HandleOnPullMove(decimal diff)
{
    pullMoveDiff = diff;
}
private void HandleOnPullEnd(decimal diff)
{
    pullEndDiff = diff;
}
private void HandleOnPullCancel(decimal diff)
{
    pullCancelDiff = diff;
}
private (int, int)[] eventsItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshEvents()
{
    refreshCount++;
    await Task.Delay(2000);
    eventsItems = GenerateRandomNumbers(1, 51);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example7RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<BitToggle @bind-Value=""isEnabled"" Label=""Enabled"" />
<BitPullToRefresh IsEnabled=""isEnabled"" OnRefresh=""HandleOnRefreshDisabled"">
    <div class=""anchor"">
        @foreach (var (idx, i) in disabledItems)
        {
            <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
        }
    </div>
</BitPullToRefresh>";
    private readonly string example7CsharpCode = @"
private bool isEnabled = true;
private (int, int)[] disabledItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshDisabled()
{
    await Task.Delay(2000);
    disabledItems = GenerateRandomNumbers(1, 51);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example8RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<BitParams Parameters=""@pullToRefreshParams"">
    <div style=""display:flex; gap:1rem;"">
        <BitPullToRefresh OnRefresh=""HandleOnRefreshCascaded"">
            <div class=""anchor"">
                @foreach (var (idx, i) in cascadedItems)
                {
                    <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
                }
            </div>
        </BitPullToRefresh>

        <BitPullToRefresh OnRefresh=""HandleOnRefreshOverriding"" Trigger=""60"">
            <div class=""anchor"">
                @foreach (var (idx, i) in overridingItems)
                {
                    <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
                }
            </div>
        </BitPullToRefresh>
    </div>
</BitParams>";
    private readonly string example8CsharpCode = @"
private readonly BitPullToRefreshParams[] pullToRefreshParams =
[
    new()
    {
        Trigger = 120,
        CompleteDelay = 1000,
        MaxPull = 160,
        ReleaseLabel = ""Let go to refresh"",
    }
];

private (int, int)[] cascadedItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshCascaded()
{
    await Task.Delay(2000);
    cascadedItems = GenerateRandomNumbers(1, 51);
}

private (int, int)[] overridingItems = GenerateRandomNumbers(51, 101);
private async Task HandleOnRefreshOverriding()
{
    await Task.Delay(2000);
    overridingItems = GenerateRandomNumbers(51, 101);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example9RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<div style=""display:flex; gap:1rem;"">
    <BitPullToRefresh OnRefresh=""HandleOnRefreshColor"" Color=""BitColor.Info"">
        <div class=""anchor"">
            @foreach (var (idx, i) in colorItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
            }
        </div>
    </BitPullToRefresh>

    <BitPullToRefresh OnRefresh=""HandleOnRefreshCustomColor"" CustomColor=""#b400ff"">
        <div class=""anchor"">
            @foreach (var (idx, i) in customColorItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
            }
        </div>
    </BitPullToRefresh>
</div>";
    private readonly string example9CsharpCode = @"
private (int, int)[] colorItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshColor()
{
    await Task.Delay(2000);
    colorItems = GenerateRandomNumbers(1, 51);
}

private (int, int)[] customColorItems = GenerateRandomNumbers(51, 101);
private async Task HandleOnRefreshCustomColor()
{
    await Task.Delay(2000);
    customColorItems = GenerateRandomNumbers(51, 101);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example10RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }

    .custom-loading {
        background-color: rgb(255, 106, 0, 0.1);
    }

    .custom-spinner {
        padding: 5px;
        border-radius: 50%;
        background-color: #ff6a00;
    }

    .custom-can-release {
        background-color: #ffd800;
    }
</style>

<div style=""display:flex; gap:1rem; flex-wrap:wrap;"">
    <BitPullToRefresh OnRefresh=""HandleOnRefreshStyle""
                      Styles=""@(new() { Loading = ""background-color: rgb(76, 255, 0, 0.1)"", Spinner = ""padding: 5px;border-radius: 50%;background-color: #4cff00;"", SpinnerCanRelease = ""background-color: #ffd800;"" })"">
        <div class=""anchor"">
            @foreach (var (idx, i) in styleItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
            }
        </div>
    </BitPullToRefresh>

    <BitPullToRefresh OnRefresh=""HandleOnRefreshClass""
                      Classes=""@(new() { Loading = ""custom-loading"", Spinner = ""custom-spinner"", SpinnerCanRelease = ""custom-can-release"" })"">
        <div class=""anchor"">
            @foreach (var (idx, i) in classItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
            }
        </div>
    </BitPullToRefresh>

    <BitPullToRefresh OnRefresh=""HandleOnRefreshCssVariables""
                      Style=""--bit-PullToRefresh-indicator-size: 3rem; --bit-PullToRefresh-glyph-size: 2rem; --bit-PullToRefresh-indicator-background: var(--bit-clr-pri); --bit-PullToRefresh-refreshing-background: var(--bit-clr-pri-hover); --bit-PullToRefresh-color: var(--bit-clr-pri-text); --bit-PullToRefresh-strip-background: var(--bit-clr-bg-sec);"">
        <div class=""anchor"">
            @foreach (var (idx, i) in cssVariablesItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
            }
        </div>
    </BitPullToRefresh>
</div>";
    private readonly string example10CsharpCode = @"
private (int, int)[] styleItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshStyle()
{
    await Task.Delay(2000);
    styleItems = GenerateRandomNumbers(1, 51);
}

private (int, int)[] classItems = GenerateRandomNumbers(51, 101);
private async Task HandleOnRefreshClass()
{
    await Task.Delay(2000);
    classItems = GenerateRandomNumbers(51, 101);
}

private (int, int)[] cssVariablesItems = GenerateRandomNumbers(101, 151);
private async Task HandleOnRefreshCssVariables()
{
    await Task.Delay(2000);
    cssVariablesItems = GenerateRandomNumbers(101, 151);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example11RazorCode = @"
<style>
    .anchor {
        width: 150px;
        padding: 4px;
        cursor: grab;
        height: 300px;
        overflow: auto;
        user-select: none;
        border: 1px gray solid;
    }
</style>

<div dir=""rtl"">
    <BitPullToRefresh Dir=""BitDir.Rtl"" OnRefresh=""HandleOnRefreshRtl"">
        <div class=""anchor"">
            @foreach (var (idx, i) in rtlItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')) .مورد @i</div>
            }
        </div>
    </BitPullToRefresh>
</div>";
    private readonly string example11CsharpCode = @"
private (int, int)[] rtlItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshRtl()
{
    await Task.Delay(2000);
    rtlItems = GenerateRandomNumbers(1, 51);
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";
}
