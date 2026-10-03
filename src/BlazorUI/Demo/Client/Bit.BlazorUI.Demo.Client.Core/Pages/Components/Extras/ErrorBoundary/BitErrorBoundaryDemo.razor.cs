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
            Description = "Moves the browser focus to the error UI as it appears, once per error, which then announces it in place of the alert. An error UI drawn by ErrorTemplate or ErrorContent has no element of the boundary's to move it to.",
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
            Name = "CopiedText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text the Copy button carries while what it copied is still on the clipboard. Defaults to \"Copied\".",
        },
        new()
        {
            Name = "CopyText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Copy button. Defaults to \"Copy details\".",
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
            Name = "ExceptionLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the exception details block rendered by ShowException. Defaults to \"Exception details\", and an empty value drops the name and the region role with it.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The footer content of the boundary, replacing the default Refresh, Home and Recover buttons while keeping the footer element they are laid out in.",
        },
        new()
        {
            Name = "HeadingLevel",
            Type = "int?",
            DefaultValue = "null",
            Description = "The heading level (1 to 6) of the title, so it fits the outline of the page while its look stays the same. Defaults to 3.",
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
            Description = "The text of the Home button. Defaults to \"Home\".",
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
            Type = "EventCallback<BitErrorBoundaryRecoverReason>",
            DefaultValue = "",
            Description = "The callback for when the boundary leaves its errored state, receiving which of the routes back out of it was taken.",
            LinkType = LinkType.Link,
            Href = "#recover-reason-enum",
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
            Description = "The text of the Recover button. Defaults to \"Recover\".",
        },
        new()
        {
            Name = "RefreshText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Refresh button. Defaults to \"Refresh\".",
        },
        new()
        {
            Name = "ShowCopyButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a Copy button in the footer of the default error UI, putting the exception's full text on the clipboard.",
        },
        new()
        {
            Name = "ShowException",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the full text of the caught exception, stack trace included, in a scrolling block that takes the keyboard focus.",
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
            Description = "The title of the default error UI. Defaults to \"Oops, Something went wrong...\", and an empty value drops the heading.",
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
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitErrorBoundary, holding the icon, the title and the message, which is the part announced as the error appears.",
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
                    Description = "Custom CSS classes/styles for the footer of the BitErrorBoundary, which holds a replaced Footer exactly as it holds the default buttons.",
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
                new()
                {
                    Name = "CopyButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Copy button of the BitErrorBoundary.",
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



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-ErrorBoundary-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the error UI.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-border",
            DefaultValue = "none",
            Description = "Border of the error UI.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-radius",
            DefaultValue = "--bit-shp-radius-none",
            Description = "Corner radius of the error UI.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-padding",
            DefaultValue = "spacing(3)",
            Description = "Padding of the error UI.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-gap",
            DefaultValue = "spacing(2)",
            Description = "Room between the parts of the error UI.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-align",
            DefaultValue = "center",
            Description = "start, center or end: aligns the parts, their text and the footer buttons together.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-icon-color",
            DefaultValue = "--bit-clr-err",
            Description = "Color of the built-in illustration or of the icon.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-icon-size",
            DefaultValue = "64px (illustration) / --bit-siz-icon-lg (icon)",
            Description = "Size of the built-in illustration or of the icon.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-title-color",
            DefaultValue = "--bit-clr-err-fg",
            Description = "Color of the title.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-message-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the message.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-message-max-width",
            DefaultValue = "560px",
            Description = "Longest line of the message.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-exception-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Background of the exception details block.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-exception-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Text color of the exception details block.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-exception-font-family",
            DefaultValue = "--bit-tpg-font-family-mono",
            Description = "Font family of the exception details block.",
        },
        new()
        {
            Name = "--bit-ErrorBoundary-exception-max-height",
            DefaultValue = "320px",
            Description = "Tallest the exception details block gets before it scrolls.",
        },
    ];



    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "recover-reason-enum",
            Name = "BitErrorBoundaryRecoverReason",
            Description = "What took the boundary out of its errored state, handed to OnRecover.",
            Items =
            [
                new()
                {
                    Name = "Manual",
                    Description = "The Recover button of the default error UI, the Recover action of an ErrorTemplate's context, or a call to the Recover method.",
                    Value = "0",
                },
                new()
                {
                    Name = "Keys",
                    Description = "One of the values of RecoverKeys differed from what the boundary last saw.",
                    Value = "1",
                },
                new()
                {
                    Name = "Navigation",
                    Description = "The reader navigated to another location while RecoverOnNavigation was set.",
                    Value = "2",
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
    private BitErrorBoundaryRecoverReason? lastRecoverReason;
    private BitErrorBoundary? footerBoundary;
    private BitErrorBoundary? captureBoundary;

    private readonly BitErrorBoundaryParams[] errorBoundaryParams =
    [
        new()
        {
            HideIcon = true,
            HideHomeButton = true,
            HideRefreshButton = true,
            Title = "This widget could not be loaded",
            Message = "The rest of the page still works.",
            RecoverText = "Reload the widget",
        }
    ];



    private void ThrowException()
    {
        throw new Exception("This is an exception!");
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
    }

    private void CaptureFromReference()
    {
        captureBoundary?.Capture(new InvalidOperationException("Captured through a component reference."));
    }

    private void NavigateInPlace()
    {
        navigateCount++;
        NavManager.NavigateTo($"/components/errorboundary?nav={navigateCount}#example9");
    }
}
