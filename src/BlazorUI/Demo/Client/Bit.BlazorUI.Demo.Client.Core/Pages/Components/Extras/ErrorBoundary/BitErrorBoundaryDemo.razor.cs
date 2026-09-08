namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ErrorBoundary;

public partial class BitErrorBoundaryDemo
{
    [Inject] private NavigationManager NavManager { get; set; } = default!;

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AdditionalButtons",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The extra content of the footer of the boundary's default error UI, rendered after the Refresh, Home and Recover buttons. A boundary that sets Footer never renders it.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Moves the browser focus to the error UI as it appears.",
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of the ChildContent.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content the boundary renders and watches over while it has caught nothing.",
        },
        new()
        {
            Name = "Class",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS class of the root element of the boundary's error UI.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitErrorBoundaryClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the boundary's default error UI.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Dir",
            Type = "BitDir?",
            DefaultValue = "null",
            Description = "The text directionality of the boundary's error UI.",
            LinkType = LinkType.Link,
            Href = "#component-dir",
        },
        new()
        {
            Name = "ErrorContent",
            Type = "RenderFragment<Exception>?",
            DefaultValue = "null",
            Description = "The inherited template of the error UI, receiving the caught exception alone. ErrorTemplate takes precedence over it.",
        },
        new()
        {
            Name = "ErrorTemplate",
            Type = "RenderFragment<BitErrorBoundaryContext>?",
            DefaultValue = "null",
            Description = "The template of the error UI, receiving the caught exception along with the boundary's own Recover, Refresh and GoHome actions.",
            LinkType = LinkType.Link,
            Href = "#context",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The footer content of the boundary, replacing the default Refresh, Home and Recover buttons.",
        },
        new()
        {
            Name = "HideHomeButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents rendering the Home button of the default error UI.",
        },
        new()
        {
            Name = "HideIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents rendering the icon of the default error UI.",
        },
        new()
        {
            Name = "HideRecoverButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents rendering the Recover button of the default error UI.",
        },
        new()
        {
            Name = "HideRefreshButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents rendering the Refresh button of the default error UI.",
        },
        new()
        {
            Name = "HomeText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Home button.",
        },
        new()
        {
            Name = "HomeUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "The url of the home page for the Home button. Defaults to the site root.",
        },
        new()
        {
            Name = "HtmlAttributes",
            Type = "Dictionary<string, object>?",
            DefaultValue = "null",
            Description = "The HTML attributes to be applied to the root element of the boundary's error UI.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon to render in place of the built-in illustration.",
        },
        new()
        {
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template of the icon, replacing both the built-in illustration and IconName.",
        },
        new()
        {
            Name = "Id",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the root element of the boundary's error UI.",
        },
        new()
        {
            Name = "MaximumErrorCount",
            Type = "int",
            DefaultValue = "100",
            Description = "The number of errors this boundary handles before it stops absorbing them and lets the next one through as fatal. Recovering resets the count.",
        },
        new()
        {
            Name = "Message",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message rendered under the title of the default error UI. Nothing is rendered while it has no value.",
        },
        new()
        {
            Name = "NoLogging",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the boundary from logging the caught exception through the app's IErrorBoundaryLogger.",
        },
        new()
        {
            Name = "OnError",
            Type = "EventCallback<Exception>",
            DefaultValue = "",
            Description = "The callback for when an error gets caught by the boundary, called before the error UI is rendered.",
        },
        new()
        {
            Name = "OnRecover",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the boundary leaves its errored state, by any of the routes back out of it.",
        },
        new()
        {
            Name = "RecoverKeys",
            Type = "IEnumerable<object?>?",
            DefaultValue = "null",
            Description = "The values that recover the boundary as they change.",
        },
        new()
        {
            Name = "RecoverOnNavigation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Recovers the boundary when the reader navigates to another location.",
        },
        new()
        {
            Name = "RecoverText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Recover button.",
        },
        new()
        {
            Name = "RefreshText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Refresh button.",
        },
        new()
        {
            Name = "ShowException",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the actual exception information should be shown or not.",
        },
        new()
        {
            Name = "Style",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS style of the root element of the boundary's error UI.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitErrorBoundaryClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the boundary's default error UI.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The header title of the boundary. Defaults to \"Oops, Something went wrong...\".",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "CaughtException",
            Type = "Exception?",
            DefaultValue = "null",
            Description = "The exception the boundary is currently showing, or null while it has caught nothing.",
        },
        new()
        {
            Name = "Capture",
            Type = "void Capture(Exception exception)",
            Description = "Hands the boundary an exception that never passed through the renderer, putting it into exactly the state a caught one would. Call it on the renderer's synchronization context.",
        },
        new()
        {
            Name = "CaptureAsync",
            Type = "Task CaptureAsync(Exception exception)",
            Description = "Capture from a thread that is not the renderer's.",
        },
        new()
        {
            Name = "Recover",
            Type = "void Recover()",
            Description = "Clears the error and renders the boundary's content again, raising OnRecover. Never call it from rendering logic.",
        },
        new()
        {
            Name = "Refresh",
            Type = "void Refresh()",
            Description = "Reloads the current page in the browser, which is what the default UI's Refresh button does.",
        },
        new()
        {
            Name = "GoHome",
            Type = "void GoHome()",
            Description = "Navigates to HomeUrl, which is what the default UI's Home button does.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitErrorBoundaryClassStyles",
            Description = "Nothing here reaches the boundary's own content: while no error has been caught the boundary renders its children and no element of its own.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitErrorBoundary's error UI.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the BitErrorBoundary.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitErrorBoundary.",
                },
                new()
                {
                    Name = "Message",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the message of the BitErrorBoundary.",
                },
                new()
                {
                    Name = "Exception",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the exception details block of the BitErrorBoundary.",
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer of the BitErrorBoundary.",
                },
                new()
                {
                    Name = "RefreshButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Refresh button of the BitErrorBoundary.",
                },
                new()
                {
                    Name = "HomeButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Home button of the BitErrorBoundary.",
                },
                new()
                {
                    Name = "RecoverButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Recover button of the BitErrorBoundary.",
                },
            ]
        },
        new()
        {
            Id = "context",
            Title = "BitErrorBoundaryContext",
            Description = "What an ErrorTemplate is handed: the exception that was caught and the three ways out of it that the boundary's own error UI offers.",
            Parameters =
            [
                new()
                {
                    Name = "Exception",
                    Type = "Exception",
                    Description = "The exception the boundary caught.",
                },
                new()
                {
                    Name = "Recover",
                    Type = "Action",
                    Description = "Clears the error and renders the boundary's content again, exactly like the default UI's Recover button.",
                },
                new()
                {
                    Name = "Refresh",
                    Type = "Action",
                    Description = "Reloads the current page in the browser, exactly like the default UI's Refresh button.",
                },
                new()
                {
                    Name = "GoHome",
                    Type = "Action",
                    Description = "Navigates to HomeUrl, exactly like the default UI's Home button.",
                },
            ]
        },
    ];



    private int errorCount;
    private int navigateCount;
    private int recoverCount;
    private int reportedCount;
    private int selectedRecord = 1;
    private string? lastError;
    private BitErrorBoundary? footerBoundary;
    private BitErrorBoundary? captureBoundary;



    private void ThrowException()
    {
        throw new Exception("This is an exception!");
    }

    private void HandleError(Exception exception)
    {
        errorCount++;
        lastError = exception.Message;
    }

    private void HandleRecover()
    {
        recoverCount++;
    }

    private void CaptureFromReference()
    {
        captureBoundary?.Capture(new InvalidOperationException("Captured through a component reference."));
    }

    private void NavigateInPlace()
    {
        navigateCount++;
        NavManager.NavigateTo($"/components/errorboundary?nav={navigateCount}#example11");
    }



    private readonly string example1RazorCode = @"
<BitErrorBoundary>
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example1CsharpCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example2RazorCode = @"
<BitErrorBoundary Title=""This report could not be built""
                  Message=""The numbers behind it are still being imported. Try again in a minute, or head back to the dashboard."">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example2CsharpCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example3RazorCode = @"
<BitErrorBoundary ShowException>
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example3CsharpCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example4RazorCode = @"
<BitErrorBoundary IconName=""@BitIconName.ErrorBadge"" Title=""Something went wrong"">
    <BitButton OnClick=""ThrowException"">Throw (IconName)</BitButton>
</BitErrorBoundary>

<BitErrorBoundary Title=""Something went wrong"">
    <ChildContent>
        <BitButton OnClick=""ThrowException"">Throw (IconTemplate)</BitButton>
    </ChildContent>
    <IconTemplate>
        <BitText Typography=""BitTypography.H2"">🛠️</BitText>
    </IconTemplate>
</BitErrorBoundary>

<BitErrorBoundary HideIcon Title=""Something went wrong"">
    <BitButton OnClick=""ThrowException"">Throw (HideIcon)</BitButton>
</BitErrorBoundary>";
    private readonly string example4CsharpCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example5RazorCode = @"
<BitErrorBoundary HideRefreshButton
                  HomeUrl=""/components/errorboundary""
                  HomeText=""Back to the docs""
                  RecoverText=""Try again"">
    <ChildContent>
        <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
    </ChildContent>
    <AdditionalButtons>
        <BitButton Variant=""BitVariant.Text"" OnClick=""() => reportedCount++"">
            Report (@reportedCount)
        </BitButton>
    </AdditionalButtons>
</BitErrorBoundary>";
    private readonly string example5CsharpCode = @"
private int reportedCount;

private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example6RazorCode = @"
<BitErrorBoundary @ref=""footerBoundary"" Title=""The upload failed"">
    <ChildContent>
        <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
    </ChildContent>
    <Footer>
        <BitButton Size=""BitSize.Small"" OnClick=""() => footerBoundary?.Recover()"">Try again</BitButton>
        <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""() => reportedCount++"">
            Report (@reportedCount)
        </BitButton>
    </Footer>
</BitErrorBoundary>";
    private readonly string example6CsharpCode = @"
private int reportedCount;
private BitErrorBoundary? footerBoundary;

private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example7RazorCode = @"
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
    private readonly string example7CsharpCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example8RazorCode = @"
<BitErrorBoundary OnError=""HandleError"" OnRecover=""HandleRecover"">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>

<div>Caught: @errorCount &nbsp;|&nbsp; Recovered: @recoverCount &nbsp;|&nbsp; Last: @(lastError ?? ""-"")</div>";
    private readonly string example8CsharpCode = @"
private int errorCount;
private int recoverCount;
private string? lastError;

private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}

private void HandleError(Exception exception)
{
    errorCount++;
    lastError = exception.Message;
}

private void HandleRecover()
{
    recoverCount++;
}";

    private readonly string example9RazorCode = @"
<BitErrorBoundary @ref=""captureBoundary"" ShowException>
    <BitButton OnClick=""CaptureFromReference"">Capture through @ref</BitButton>
    <_BitErrorBoundaryCaptureDemo />
</BitErrorBoundary>";
    private readonly string example9CsharpCode = @"
private BitErrorBoundary? captureBoundary;

private void CaptureFromReference()
{
    captureBoundary?.Capture(new InvalidOperationException(""Captured through a component reference.""));
}";
    private const string example9ChildCode = @"@* A component sitting inside a BitErrorBoundary. It never holds a reference to the boundary: the
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
    private readonly DemoCodeFile[] example9CodeFiles =
    [
        new("_BitErrorBoundaryCaptureDemo.razor", example9ChildCode),
    ];

    private readonly string example10RazorCode = @"
<BitErrorBoundary RecoverKeys=""@(new object?[] { selectedRecord })"" HideRefreshButton HideHomeButton HideRecoverButton
                  Title=""This record could not be shown""
                  Message=""Pick another one and the boundary clears itself."">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => selectedRecord = 1"">Record 1</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => selectedRecord = 2"">Record 2</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => selectedRecord = 3"">Record 3</BitButton>

<div>Selected record: @selectedRecord</div>";
    private readonly string example10CsharpCode = @"
private int selectedRecord = 1;

private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example11RazorCode = @"
<BitErrorBoundary RecoverOnNavigation HideRefreshButton HideHomeButton HideRecoverButton
                  Title=""Something went wrong""
                  Message=""Navigate anywhere and this clears itself."">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>

<BitButton Variant=""BitVariant.Outline"" OnClick=""NavigateInPlace"">Navigate</BitButton>";
    private readonly string example11CsharpCode = @"
[Inject] private NavigationManager NavManager { get; set; } = default!;

private int navigateCount;

private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}

private void NavigateInPlace()
{
    navigateCount++;
    NavManager.NavigateTo($""/components/errorboundary?nav={navigateCount}#example11"");
}";

    private readonly string example12RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitErrorBoundary Icon=""@BitIconInfo.Fa(""solid triangle-exclamation"")"" Title=""FontAwesome"">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>

<BitErrorBoundary Icon=""@BitIconInfo.Bi(""exclamation-octagon-fill"")"" Title=""Bootstrap Icons"">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example12CsharpCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";

    private readonly string example13RazorCode = @"
<BitErrorBoundary Title=""Styles""
                  Message=""Every part reached by name.""
                  Styles=""@(new() { Root = ""background: linear-gradient(180deg, #7c1d1d, transparent) #240a0a; border-radius: 0.5rem"",
                                    Title = ""color: #ffd9d9"",
                                    Message = ""color: #ffb4b4"",
                                    RecoverButton = new() { Root = ""border-radius: 1rem"" } })"">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>

<BitErrorBoundary Title=""Classes""
                  Message=""Every part reached by name.""
                  Classes=""@(new() { Root = ""custom-erb"", Title = ""custom-erb-ttl"", RecoverButton = new() { Root = ""custom-erb-btn"" } })"">
    <BitButton OnClick=""ThrowException"">Throw an exception</BitButton>
</BitErrorBoundary>";
    private readonly string example13CsharpCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";
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
    private readonly string example14CsharpCode = @"
private void ThrowException()
{
    throw new Exception(""This is an exception!"");
}";
}
