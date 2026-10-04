namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ErrorBoundary;

public partial class BitErrorBoundaryDemo
{
    private const string throwExceptionCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example1RazorCode = @"
<BitErrorBoundary>
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example1CsharpCode = throwExceptionCode;

    private readonly string example2RazorCode = @"
<BitErrorBoundary Title=""This report could not be built""
                  Message=""The numbers behind it are still being imported. Try again in a minute, or head back to the dashboard."">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example2CsharpCode = throwExceptionCode;

    private readonly string example3RazorCode = @"
<BitErrorBoundary IconName=""@BitIconName.ErrorBadge"" Title=""Something went wrong"">
    <BitButton OnClick=""ThrowException"">Throw (IconName)</BitButton>
</BitErrorBoundary>

<BitErrorBoundary Title=""Something went wrong"">
    <ChildContent>
        <BitButton OnClick=""ThrowException"">Throw (IconTemplate)</BitButton>
    </ChildContent>
    <IconTemplate>
        <BitText Typography=""BitTypography.H2"" aria-hidden=""true"">🛠️</BitText>
    </IconTemplate>
</BitErrorBoundary>

<BitErrorBoundary HideIcon Title=""Something went wrong"">
    <BitButton OnClick=""ThrowException"">Throw (HideIcon)</BitButton>
</BitErrorBoundary>";
    private readonly string example3CsharpCode = throwExceptionCode;

    private readonly string example4RazorCode = @"
<BitErrorBoundary ShowException ShowCopyButton>
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example4CsharpCode = throwExceptionCode;

    private readonly string example5RazorCode = @"
<BitErrorBoundary HideRefreshButton
                  HomeUrl=""/components/errorboundary""
                  HomeText=""Back to the docs""
                  RecoverText=""Try again"">
    <ChildContent>
        <BitButton OnClick=""ThrowException"">Throw (AdditionalButtons)</BitButton>
    </ChildContent>
    <AdditionalButtons>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => reportedCount++"">
            Report (@reportedCount)
        </BitButton>
    </AdditionalButtons>
</BitErrorBoundary>

<BitErrorBoundary @ref=""footerBoundary"" Title=""The upload failed"">
    <ChildContent>
        <BitButton OnClick=""ThrowException"">Throw (Footer)</BitButton>
    </ChildContent>
    <Footer>
        <BitButton Size=""BitSize.Small"" OnClick=""() => footerBoundary?.Recover()"">Try again</BitButton>
        <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""() => reportedCount++"">
            Report (@reportedCount)
        </BitButton>
    </Footer>
</BitErrorBoundary>";
    private readonly string example5CsharpCode = @"
private int reportedCount;
private BitErrorBoundary? footerBoundary;
" + throwExceptionCode;

    private readonly string example6RazorCode = @"
<BitErrorBoundary>
    <ChildContent>
        <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
    </ChildContent>
    <ErrorTemplate Context=""error"">
        <BitMessage Color=""BitColor.Error"" Multiline>
            <b>@error.Exception.Message</b>
            <br />
            <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""error.Recover"">Try again</BitButton>
        </BitMessage>
    </ErrorTemplate>
</BitErrorBoundary>";
    private readonly string example6CsharpCode = throwExceptionCode;

    private readonly string example7RazorCode = @"
<BitErrorBoundary OnError=""HandleError"" OnRecover=""HandleRecover"">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>

<div>
    Caught: @errorCount &nbsp;|&nbsp; Recovered: @recoverCount &nbsp;|&nbsp;
    Last error: @(lastError ?? ""-"") &nbsp;|&nbsp; Last recovery: @(lastRecoverReason?.ToString() ?? ""-"")
</div>";
    private readonly string example7CsharpCode = @"
private int errorCount;
private int recoverCount;
private string? lastError;
private BitErrorBoundaryRecoverReason? lastRecoverReason;

private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}

private void HandleError(Exception exception)
{
    errorCount++;
    lastError = exception.Message;
}

private void HandleRecover(BitErrorBoundaryRecoverReason reason)
{
    recoverCount++;
    lastRecoverReason = reason;
}";

    private readonly string example8RazorCode = @"
<BitErrorBoundary @ref=""captureBoundary"" ShowException>
    <BitButton OnClick=""CaptureFromReference"">Capture through @@ref</BitButton>
    <_BitErrorBoundaryCaptureDemo />
</BitErrorBoundary>";
    private readonly string example8CsharpCode = @"
private BitErrorBoundary? captureBoundary;

private void CaptureFromReference()
{
    captureBoundary?.Capture(new InvalidOperationException(""Captured through a component reference.""));
}";
    private const string example8ChildCode = @"@* A component sitting inside a BitErrorBoundary. It never holds a reference to the boundary: the
   boundary cascades itself to its content, so a cascading parameter is all it takes to hand it an
   exception the renderer would never have seen. *@

<BitButton Variant=""BitVariant.Outline"" OnClick=""Load"">Capture through the cascade</BitButton>

@code {
    [CascadingParameter] private BitErrorBoundary? errorBoundary { get; set; }

    private void Load()
    {
        // Nothing awaits this task, so nothing routes what it throws to the boundary above - which is
        // exactly the case Capture exists for.
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);

            try
            {
                throw new InvalidOperationException(""The data could not be loaded in the background."");
            }
            catch (Exception ex)
            {
                if (errorBoundary is not null)
                {
                    // Off the renderer's thread, so CaptureAsync rather than Capture.
                    await errorBoundary.CaptureAsync(ex);
                }
            }
        });
    }
}";
    private readonly DemoCodeFile[] example8CodeFiles =
    [
        new("_BitErrorBoundaryCaptureDemo.razor", example8ChildCode),
    ];

    private readonly string example9RazorCode = @"
<BitErrorBoundary RecoverKeys=""@(new object?[] { selectedRecord })""
                  HideRefreshButton HideHomeButton HideRecoverButton
                  Title=""@($""Record {selectedRecord} could not be shown"")""
                  Message=""Pick another one and this clears itself."">
    <BitButton OnClick=""ThrowException"">Throw (RecoverKeys)</BitButton>
</BitErrorBoundary>

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => selectedRecord = 1"">Record 1</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => selectedRecord = 2"">Record 2</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => selectedRecord = 3"">Record 3</BitButton>


<BitErrorBoundary RecoverOnNavigation
                  HideRefreshButton HideHomeButton HideRecoverButton
                  Title=""Something went wrong""
                  Message=""Navigate anywhere and this clears itself."">
    <BitButton OnClick=""ThrowException"">Throw (RecoverOnNavigation)</BitButton>
</BitErrorBoundary>

<BitButton Variant=""BitVariant.Outline"" OnClick=""NavigateInPlace"">Navigate</BitButton>";
    private readonly string example9CsharpCode = @"
[Inject] private NavigationManager NavManager { get; set; } = default!;

private int navigateCount;
private int selectedRecord = 1;

private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}

private void NavigateInPlace()
{
    navigateCount++;
    NavManager.NavigateTo($""/components/errorboundary?nav={navigateCount}#example9"");
}";

    private readonly string example10RazorCode = @"
<BitErrorBoundary AutoFocus
                  ShowException
                  HeadingLevel=""2""
                  ExceptionLabel=""Server response""
                  Title=""This page could not be shown""
                  Message=""The focus moves here, so the keyboard is already next to the way out."">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example10CsharpCode = throwExceptionCode;

    private readonly string example11RazorCode = @"
<BitParams Parameters=""@errorBoundaryParams"">
    <BitErrorBoundary>
        <BitButton OnClick=""ThrowException"">Throw (cascaded)</BitButton>
    </BitErrorBoundary>
    <BitErrorBoundary Title=""Its own title, the cascaded rest"">
        <BitButton OnClick=""ThrowException"">Throw (own Title)</BitButton>
    </BitErrorBoundary>
</BitParams>";
    private readonly string example11CsharpCode = @"
private readonly BitErrorBoundaryParams[] errorBoundaryParams =
[
    new()
    {
        HideIcon = true,
        HideHomeButton = true,
        HideRefreshButton = true,
        Title = ""This widget could not be loaded"",
        Message = ""The rest of the page still works."",
        RecoverText = ""Reload the widget"",
    }
];
" + throwExceptionCode;

    private readonly string example12RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitErrorBoundary Icon=""@BitIconInfo.Fa(""solid triangle-exclamation"")"" Title=""FontAwesome"">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>

<BitErrorBoundary Icon=""@BitIconInfo.Bi(""exclamation-octagon-fill"")"" Title=""Bootstrap Icons"">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example12CsharpCode = throwExceptionCode;

    private readonly string example13RazorCode = @"
<BitErrorBoundary Title=""Styles""
                  Message=""Every part reached by name.""
                  Styles=""@(new() { Root = ""background: linear-gradient(180deg, #7c1d1d, transparent) #240a0a; border-radius: 0.5rem"",
                                    Title = ""color: #ffd9d9"",
                                    Message = ""color: #ffb4b4"",
                                    RecoverButton = new() { Root = ""border-radius: 1rem"" } })"">
    <BitButton OnClick=""ThrowException"">Throw (Styles)</BitButton>
</BitErrorBoundary>

<BitErrorBoundary Title=""Classes""
                  Message=""Every part reached by name.""
                  Classes=""@(new() { Root = ""custom-erb"", Title = ""custom-erb-ttl"", RecoverButton = new() { Root = ""custom-erb-btn"" } })"">
    <BitButton OnClick=""ThrowException"">Throw (Classes)</BitButton>
</BitErrorBoundary>

<BitErrorBoundary Title=""CSS variables""
                  Message=""Aligned to the start, boxed and tinted.""
                  HideRefreshButton HideHomeButton
                  Style=""--bit-ErrorBoundary-align: start; --bit-ErrorBoundary-icon-size: 2.5rem; --bit-ErrorBoundary-radius: 0.5rem; --bit-ErrorBoundary-border: 1px solid var(--bit-clr-err); --bit-ErrorBoundary-background: var(--bit-clr-err-tint);"">
    <BitButton OnClick=""ThrowException"">Throw (CSS variables)</BitButton>
</BitErrorBoundary>";
    private readonly string example13CsharpCode = throwExceptionCode;
    private const string example13ScssCode = @"::deep {
    .custom-erb {
        border-radius: 0.5rem;
        background: linear-gradient(180deg, #3e0f0f, transparent) #000;
    }

    .custom-erb-ttl {
        color: #ff9d9d;
        letter-spacing: 0.05rem;
    }

    .custom-erb-btn {
        color: #fff;
        border-radius: 1rem;
        border-color: #8f0101;
        transition: background-color 1s;
        background: linear-gradient(90deg, #d10000, transparent) #8f0101;
    }

    .custom-erb-btn:hover {
        color: #fff;
        border-color: #8f0101;
        background-color: #8f0101;
    }
}";
    private readonly DemoCodeFile[] example13CodeFiles =
    [
        new("BitErrorBoundaryDemo.razor.scss", example13ScssCode),
    ];

    private readonly string example14RazorCode = @"
<div dir=""rtl"">
    <BitErrorBoundary Dir=""BitDir.Rtl""
                      Title=""اوه، مشکلی پیش آمد...""
                      Message=""لطفاً دوباره تلاش کنید یا به صفحه اصلی بازگردید.""
                      HomeText=""خانه""
                      RefreshText=""بارگذاری مجدد""
                      RecoverText=""تلاش دوباره""
                      HomeUrl=""/components/errorboundary"">
        <BitButton OnClick=""ThrowException"">ایجاد خطا</BitButton>
    </BitErrorBoundary>
</div>";
    private readonly string example14CsharpCode = throwExceptionCode;
}
