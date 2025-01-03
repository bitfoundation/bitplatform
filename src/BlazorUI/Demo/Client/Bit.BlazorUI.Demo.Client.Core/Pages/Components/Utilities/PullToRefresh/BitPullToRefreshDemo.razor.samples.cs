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
    _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
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

<BitPullToRefresh OnRefresh=""HandleOnRefreshCustom"">
    <Anchor>
        <div class=""anchor"">
            @foreach (var (idx, i) in customItems)
            {
                <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
            }
        </div>
    </Anchor>
    <Loading>
        <svg viewBox=""0 0 490 490"" fill=""currentColor"">
            <path d=""M112.156,97.111c72.3-65.4,180.5-66.4,253.8-6.7l-58.1,2.2c-7.5,0.3-13.3,6.5-13,14c0.3,7.3,6.3,13,13.5,13 c0.2,0,0.3,0,0.5,0l89.2-3.3c7.3-0.3,13-6.2,13-13.5v-1c0-0.2,0-0.3,0-0.5v-0.1l0,0l-3.3-88.2c-0.3-7.5-6.6-13.3-14-13 c-7.5,0.3-13.3,6.5-13,14l2.1,55.3c-36.3-29.7-81-46.9-128.8-49.3c-59.2-3-116.1,17.3-160,57.1c-60.4,54.7-86,137.9-66.8,217.1 c1.5,6.2,7,10.3,13.1,10.3c1.1,0,2.1-0.1,3.2-0.4c7.2-1.8,11.7-9.1,9.9-16.3C36.656,218.211,59.056,145.111,112.156,97.111z"">
            </path>
            <path d=""M462.456,195.511c-1.8-7.2-9.1-11.7-16.3-9.9c-7.2,1.8-11.7,9.1-9.9,16.3c16.9,69.6-5.6,142.7-58.7,190.7 c-37.3,33.7-84.1,50.3-130.7,50.3c-44.5,0-88.9-15.1-124.7-44.9l58.8-5.3c7.4-0.7,12.9-7.2,12.2-14.7s-7.2-12.9-14.7-12.2l-88.9,8 c-7.4,0.7-12.9,7.2-12.2,14.7l8,88.9c0.6,7,6.5,12.3,13.4,12.3c0.4,0,0.8,0,1.2-0.1c7.4-0.7,12.9-7.2,12.2-14.7l-4.8-54.1 c36.3,29.4,80.8,46.5,128.3,48.9c3.8,0.2,7.6,0.3,11.3,0.3c55.1,0,107.5-20.2,148.7-57.4 C456.056,357.911,481.656,274.811,462.456,195.511z"">
            </path>
        </svg>
    </Loading>
</BitPullToRefresh>";
    private readonly string example2CsharpCode = @"
private (int, int)[] customItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshCustom()
{
    await Task.Delay(2000);
    customItems = GenerateRandomNumbers(1, 51);
    _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example3RazorCode = @"
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
    <div>
        <BitPullToRefresh OnRefresh=""HandleOnRefresh1"">
            <div class=""anchor"">
                @foreach (var (idx, i) in multiItems1)
                {
                    <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
                }
            </div>
        </BitPullToRefresh>
    </div>

    <div>
        <BitPullToRefresh OnRefresh=""HandleOnRefresh2"">
            <div class=""anchor"">
                @foreach (var (idx, i) in multiItems2)
                {
                    <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
                }
            </div>
        </BitPullToRefresh>
    </div>
</div>";
    private readonly string example3CsharpCode = @"
private (int, int)[] multiItems1 = GenerateRandomNumbers(0, 50);
private async Task HandleOnRefresh1()
{
    await Task.Delay(2000);
    multiItems1 = GenerateRandomNumbers(1, 51);
    _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
}
private (int, int)[] multiItems2 = GenerateRandomNumbers(51, 101);
private async Task HandleOnRefresh2()
{
    await Task.Delay(2000);
    multiItems2 = GenerateRandomNumbers(51, 101);
    _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

    private readonly string example4RazorCode = @"
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

    .advanced-anchor {
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
                        <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Width=""50"" />
                        <BitText Typography=""BitTypography.H4"" Color=""BitColor.Info"">
                            BlazorUI
                        </BitText>
                    </BitStack>
                </BitCard>
            </Header>
            <Main>
                <BitPullToRefresh OnRefresh=""HandleOnRefreshAdvanced"" ScrollerSelector="".advanced-anchor"" Style=""width:100%"">
                    <div class=""advanced-anchor"">
                        @foreach (var (idx, i) in advancedItems)
                        {
                            <div class=""row"" @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
                        }
                    </div>
                </BitPullToRefresh>
            </Main>
        </BitLayout>
    </div>
</div>";
    private readonly string example4CsharpCode = @"
private (int, int)[] advancedItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshAdvanced()
{
    await Task.Delay(2000);
    advancedItems = GenerateRandomNumbers(1, 51);
    _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
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
    
    .custom-loading {
        background-color: rgb(255, 106, 0, 0.1);
    }
    
    .custom-spinner {
        padding: 5px;
        border-radius: 50%;
        background-color: #ff6a00;
    }
</style>

<div style=""display:flex; gap:1rem;"">
    <BitPullToRefresh OnRefresh=""HandleOnRefreshStyle""
                      Styles=""@(new() { Loading = ""background-color: rgb(76, 255, 0, 0.1)"", Spinner = ""padding: 5px;border-radius: 50%;background-color: #4cff00;"" })"">
    <div class=""anchor"">
        @foreach (var (idx, i) in styleItems)
        {
            <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
        }
    </div>
</BitPullToRefresh>

<BitPullToRefresh OnRefresh=""HandleOnRefreshClass""
                  Classes=""@(new() { Loading = ""custom-loading"", Spinner = ""custom-spinner"" })"">
    <div class=""anchor"">
        @foreach (var (idx, i) in classItems)
        {
            <div @key=""idx"">@(idx.ToString().PadLeft(2, '0')). Item @i</div>
        }
    </div>
</BitPullToRefresh>
</div>";
    private readonly string example5CsharpCode = @"
private (int, int)[] styleItems = GenerateRandomNumbers(1, 51);
private async Task HandleOnRefreshStyle()
{
    await Task.Delay(2000);
    styleItems = GenerateRandomNumbers(1, 51);
    _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
}

private (int, int)[] classItems = GenerateRandomNumbers(51, 101);
private async Task HandleOnRefreshClass()
{
    await Task.Delay(2000);
    classItems = GenerateRandomNumbers(51, 101);
    _ = Task.Delay(1000).ContinueWith(_ => InvokeAsync(StateHasChanged));
}

private static (int, int)[] GenerateRandomNumbers(int min, int max)
{
    var random = new Random();
    return Enumerable.Range(min, max - min).Select(i => (i, random.Next(min, max))).ToArray();
}";

}
