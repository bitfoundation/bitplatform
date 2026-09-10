namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MessageBox;

public partial class BitMessageBoxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Moves the focus onto the default action button once the message box is rendered. The BitMessageBoxService defaults it to true for the message boxes it shows.",
        },
        new()
        {
            Name = "AutoLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the loading state of the action button that was pressed for as long as its callback runs.",
        },
        new()
        {
            Name = "Body",
            Type = "string?",
            DefaultValue = "null",
            Description = "The body of the message box. Line breaks in it are kept and long lines wrap.",
        },
        new()
        {
            Name = "BodyTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template of the body of the message box, which takes the place of Body.",
        },
        new()
        {
            Name = "Buttons",
            Type = "BitMessageBoxButtons",
            DefaultValue = "BitMessageBoxButtons.Ok",
            Description = "The set of buttons the message box renders in its footer.",
            LinkType = LinkType.Link,
            Href = "#buttons-enum",
        },
        new()
        {
            Name = "ButtonColor",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the action buttons of the message box. Tertiary by default.",
            LinkType = LinkType.Link,
            Href = "/components/message/#color-enum",
        },
        new()
        {
            Name = "CancelText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Cancel button.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The alias of BodyTemplate.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitMessageBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the message box.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "CloseButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title (and aria-label) of the close button, for accessibility and localization. Defaults to \"Close\".",
        },
        new()
        {
            Name = "CloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the close button, provided as custom CSS classes of an external icon library.",
        },
        new()
        {
            Name = "CloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the close button, from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the message box, which paints its leading icon and - unless IconName says otherwise - picks the glyph.",
            LinkType = LinkType.Link,
            Href = "/components/message/#color-enum",
        },
        new()
        {
            Name = "DefaultButton",
            Type = "BitMessageBoxResult?",
            DefaultValue = "null",
            Description = "The action button that AutoFocus moves the focus onto, or None for the close button. Defaults to the affirmative button of the set (Ok, or Yes), and to the close button for a message box that renders no action buttons.",
            LinkType = LinkType.Link,
            Href = "#result-enum",
        },
        new()
        {
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the footer of the message box, which takes the place of its action buttons. The controls in it are the page's own, so AnswerAsync is what ends the message box with an answer.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the header of the message box, which takes the place of its icon, title and close button.",
        },
        new()
        {
            Name = "HideIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the leading icon of the message box, which a Color would otherwise bring with it.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The leading icon of the message box, provided as custom CSS classes of an external icon library.",
        },
        new()
        {
            Name = "IconAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the leading icon, which turns it from decoration into an image that is announced.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the leading icon of the message box, from the built-in Fluent UI icons. If unset, the icon is selected automatically based on Color.",
        },
        new()
        {
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the leading icon of the message box.",
        },
        new()
        {
            Name = "NoText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the No button.",
        },
        new()
        {
            Name = "OkText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Ok button.",
        },
        new()
        {
            Name = "OnBeforeResult",
            Type = "EventCallback<BitMessageBoxBeforeResultArgs>",
            DefaultValue = "",
            Description = "The event callback asked before the message box hands over an answer. Setting Cancel on its arguments refuses the answer and keeps the message box open. It guards every button the message box draws, the close button included.",
            LinkType = LinkType.Link,
            Href = "#before-result-args",
        },
        new()
        {
            Name = "OnCancel",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The event callback for the Cancel button of the message box.",
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The event callback for closing the message box, raised by every button it renders of its own - after the callback of that button and after OnResult.",
        },
        new()
        {
            Name = "OnNo",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The event callback for the No button of the message box.",
        },
        new()
        {
            Name = "OnOk",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The event callback for the Ok button of the message box.",
        },
        new()
        {
            Name = "OnResult",
            Type = "EventCallback<BitMessageBoxResult>",
            DefaultValue = "",
            Description = "The event callback for the answer the message box was given. The close button answers with None, the others with the result they stand for.",
            LinkType = LinkType.Link,
            Href = "#result-enum",
        },
        new()
        {
            Name = "OnYes",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The event callback for the Yes button of the message box.",
        },
        new()
        {
            Name = "PrimaryButtonColor",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The color of the affirmative action button (Ok, or Yes), which falls back to ButtonColor where it is not set.",
            LinkType = LinkType.Link,
            Href = "/components/message/#color-enum",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the action buttons in the reverse order, which also reverses the order the keyboard reaches them in.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Renders the close button in the header of the message box.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the message box, which scales its inset, its body text and its leading icon together.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitMessageBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the message box.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title of the message box.",
        },
        new()
        {
            Name = "YesText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Yes button.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "AnswerAsync",
            Type = "Task",
            DefaultValue = "",
            Description = "Answers the message box as though the button standing for that result had been pressed, down the same road: the guard is asked first, then the callback of that answer, OnResult and OnClose. This is how a footer of your own ends the message box with a real answer.",
        },
        new()
        {
            Name = "Result",
            Type = "BitMessageBoxResult",
            DefaultValue = "BitMessageBoxResult.None",
            Description = "The answer the last showing of this message box was given, or None while it has not been answered.",
            LinkType = LinkType.Link,
            Href = "#result-enum",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Moves the focus onto the default action button of the message box, or onto its close button where it renders no action buttons of its own.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitMessageBoxClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitMessageBox."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the BitMessageBox."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitMessageBox."
                },
                new()
                {
                    Name = "IconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon container of the BitMessageBox."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the BitMessageBox."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitMessageBox."
                },
                new()
                {
                    Name = "Spacer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSpacer of the BitMessageBox."
                },
                new()
                {
                    Name = "CloseButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the CloseButton of the BitMessageBox.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#class-styles"
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitMessageBox."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer of the BitMessageBox."
                },
                new()
                {
                    Name = "ActionButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for every action button of the BitMessageBox that was given none of its own.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#class-styles"
                },
                new()
                {
                    Name = "OkButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the OkButton of the BitMessageBox.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#class-styles"
                },
                new()
                {
                    Name = "CancelButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the CancelButton of the BitMessageBox.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#class-styles"
                },
                new()
                {
                    Name = "YesButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the YesButton of the BitMessageBox.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#class-styles"
                },
                new()
                {
                    Name = "NoButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the NoButton of the BitMessageBox.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#class-styles"
                }
            ]
        },
        new()
        {
            Id = "before-result-args",
            Title = "BitMessageBoxBeforeResultArgs",
            Description = "The arguments of the OnBeforeResult callback, which is asked before a message box hands over the answer a button of its own was pressed for.",
            Parameters =
            [
                new()
                {
                    Name = "Result",
                    Type = "BitMessageBoxResult",
                    DefaultValue = "BitMessageBoxResult.None",
                    Description = "The answer that is about to be handed over: the result of the button that was pressed, or None for the close button.",
                    LinkType = LinkType.Link,
                    Href = "#result-enum"
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to keep the message box open and hand over no answer."
                }
            ]
        },
        new()
        {
            Id = "messagebox-parameters",
            Title = "BitMessageBoxParameters",
            Description = "The set of parameters a message box shown through the BitMessageBoxService is customized with. Every member is nullable and null means \"not set\", so the BitMessageBox default stands.",
            Parameters =
            [
                new()
                {
                    Name = "AutoFocus",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Moves the focus onto the default action button. Defaults to true for a message box shown through the service."
                },
                new()
                {
                    Name = "AutoLoading",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Enables the loading state of the action button that was pressed for as long as its callback runs."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The body of the message box, which is also what describes the dialog it is shown in."
                },
                new()
                {
                    Name = "BodyTemplate",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The template of the body of the message box."
                },
                new()
                {
                    Name = "Buttons",
                    Type = "BitMessageBoxButtons?",
                    DefaultValue = "null",
                    Description = "The set of buttons the message box renders in its footer.",
                    LinkType = LinkType.Link,
                    Href = "#buttons-enum"
                },
                new()
                {
                    Name = "ButtonColor",
                    Type = "BitColor?",
                    DefaultValue = "null",
                    Description = "The color of the action buttons of the message box."
                },
                new()
                {
                    Name = "CancelText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text of the Cancel button."
                },
                new()
                {
                    Name = "Classes",
                    Type = "BitMessageBoxClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes for different parts of the message box.",
                    LinkType = LinkType.Link,
                    Href = "#class-styles"
                },
                new()
                {
                    Name = "CloseButtonTitle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The title (and aria-label) of the close button."
                },
                new()
                {
                    Name = "CloseIcon",
                    Type = "BitIconInfo?",
                    DefaultValue = "null",
                    Description = "The icon of the close button, provided as custom CSS classes of an external icon library."
                },
                new()
                {
                    Name = "CloseIconName",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The name of the icon of the close button, from the built-in Fluent UI icons."
                },
                new()
                {
                    Name = "Color",
                    Type = "BitColor?",
                    DefaultValue = "null",
                    Description = "The general color of the message box, which is the severity of its message. Warning, SevereWarning and Error are announced as alerts."
                },
                new()
                {
                    Name = "DefaultButton",
                    Type = "BitMessageBoxResult?",
                    DefaultValue = "null",
                    Description = "The action button the focus is moved onto, or None for the close button.",
                    LinkType = LinkType.Link,
                    Href = "#result-enum"
                },
                new()
                {
                    Name = "Dir",
                    Type = "BitDir?",
                    DefaultValue = "null",
                    Description = "The general directionality of the message box and of the modal it is shown in."
                },
                new()
                {
                    Name = "FooterTemplate",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The template used to render the footer of the message box."
                },
                new()
                {
                    Name = "HeaderTemplate",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The template used to render the header of the message box."
                },
                new()
                {
                    Name = "HideIcon",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Removes the leading icon of the message box."
                },
                new()
                {
                    Name = "Icon",
                    Type = "BitIconInfo?",
                    DefaultValue = "null",
                    Description = "The leading icon of the message box, provided as custom CSS classes of an external icon library."
                },
                new()
                {
                    Name = "IconAriaLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The accessible name of the leading icon."
                },
                new()
                {
                    Name = "IconName",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The name of the leading icon of the message box, from the built-in Fluent UI icons."
                },
                new()
                {
                    Name = "IconTemplate",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The template used to render the leading icon of the message box."
                },
                new()
                {
                    Name = "Id",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The id of the rendered message box. The service generates one when none is given, since the ids of the title and the body are derived from it."
                },
                new()
                {
                    Name = "Modal",
                    Type = "BitModalParameters?",
                    DefaultValue = "null",
                    Description = "The parameters of the BitModal the message box is shown in. What is set here wins over the values the service works out on its own.",
                    LinkType = LinkType.Link,
                    Href = "/components/modalservice/#modal-parameters"
                },
                new()
                {
                    Name = "NoText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text of the No button."
                },
                new()
                {
                    Name = "OkText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text of the Ok button."
                },
                new()
                {
                    Name = "OnBeforeResult",
                    Type = "EventCallback<BitMessageBoxBeforeResultArgs>",
                    DefaultValue = "",
                    Description = "The event callback asked before the message box hands over an answer. Setting Cancel on its arguments keeps the message box open and leaves the caller of the service still waiting.",
                    LinkType = LinkType.Link,
                    Href = "#before-result-args"
                },
                new()
                {
                    Name = "Persistent",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Keeps the message box alive through the lifecycle of the application until it is closed, rather than only for as long as the modal container that renders it."
                },
                new()
                {
                    Name = "PrimaryButtonColor",
                    Type = "BitColor?",
                    DefaultValue = "null",
                    Description = "The color of the affirmative action button (Ok, or Yes), which falls back to ButtonColor."
                },
                new()
                {
                    Name = "Reversed",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Renders the action buttons in the reverse order."
                },
                new()
                {
                    Name = "ShowCloseButton",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Renders the close button in the header of the message box."
                },
                new()
                {
                    Name = "Size",
                    Type = "BitSize?",
                    DefaultValue = "null",
                    Description = "The size of the message box.",
                    LinkType = LinkType.Link,
                    Href = "#size-enum"
                },
                new()
                {
                    Name = "Styles",
                    Type = "BitMessageBoxClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS styles for different parts of the message box.",
                    LinkType = LinkType.Link,
                    Href = "#class-styles"
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The title of the message box, which is also what names the dialog it is shown in."
                },
                new()
                {
                    Name = "YesText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text of the Yes button."
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "buttons-enum",
            Name = "BitMessageBoxButtons",
            Description = "The set of buttons a BitMessageBox renders in its footer.",
            Items =
            [
                new() { Name = "Ok", Description = "A single Ok button, which answers with BitMessageBoxResult.Ok.", Value = "0" },
                new() { Name = "OkCancel", Description = "An Ok and a Cancel button.", Value = "1" },
                new() { Name = "YesNo", Description = "A Yes and a No button.", Value = "2" },
                new() { Name = "YesNoCancel", Description = "A Yes, a No and a Cancel button.", Value = "3" },
                new() { Name = "None", Description = "No action buttons at all, which leaves the footer off the message box entirely: it is dismissed rather than answered, so it answers with BitMessageBoxResult.None.", Value = "4" }
            ]
        },
        new()
        {
            Id = "result-enum",
            Name = "BitMessageBoxResult",
            Description = "How a showing of a BitMessageBox was answered.",
            Items =
            [
                new() { Name = "None", Description = "The message box was dismissed rather than answered: its close button, or - for one shown through the service - the Escape key, a click on the overlay, or the page closing the modal itself.", Value = "0" },
                new() { Name = "Ok", Description = "The Ok button ended the showing.", Value = "1" },
                new() { Name = "Cancel", Description = "The Cancel button ended the showing.", Value = "2" },
                new() { Name = "Yes", Description = "The Yes button ended the showing.", Value = "3" },
                new() { Name = "No", Description = "The No button ended the showing.", Value = "4" }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "The size of the message box.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" }
            ]
        }
    ];



    private bool isModalOpen;
    private BitMessageBox? templatesMessageBox;
    private BitMessageBoxResult templatesResult;
    private bool guardConfirmed;
    private bool guardRefused;
    private BitMessageBoxResult guardResult;
    private BitMessageBoxResult buttonsResult;
    private BitMessageBoxResult modalServiceResult;
    private bool? confirmResult;

    private async Task HandleBeforeResult(BitMessageBoxBeforeResultArgs args)
    {
        guardRefused = false;

        // Only the destructive answer is guarded: Keep and the close button end the box as they always would.
        if (args.Result is not BitMessageBoxResult.Yes) return;

        // The work the answer starts, which AutoLoading spins the pressed button through.
        await Task.Delay(1000);

        if (guardConfirmed) return;

        // Refused: nothing is reported, and a box shown through the service would stay open.
        args.Cancel = true;
        guardRefused = true;
    }

    [AutoInject] private BitModalService modalService { get; set; } = default!;
    private async Task ShowMessageBox()
    {
        var modalRef = await modalService.Show<BitMessageBox>(modalRef => new()
        {
            { nameof(BitMessageBox.Title), "This is a title" },
            { nameof(BitMessageBox.Body), "This is a body." },
            { nameof(BitMessageBox.AutoFocus), true },
            { nameof(BitMessageBox.Buttons), BitMessageBoxButtons.OkCancel },
            { nameof(BitMessageBox.OnResult), EventCallback.Factory.Create<BitMessageBoxResult>(this, r => modalRef.CloseWith(r)) }
        });

        modalServiceResult = (await modalRef.Result) as BitMessageBoxResult? ?? BitMessageBoxResult.None;
    }

    [AutoInject] private BitMessageBoxService messageBoxService { get; set; } = default!;
    private async Task ShowMessageBoxService()
    {
        await messageBoxService.Show("TITLE", "BODY");
    }

    private async Task ShowInfoMessageBox()
    {
        await messageBoxService.ShowInfo("Information", "The export finished in 4 seconds.");
    }

    private async Task ShowSuccessMessageBox()
    {
        await messageBoxService.ShowSuccess("Success", "Your changes are saved.");
    }

    private async Task ShowWarningMessageBox()
    {
        await messageBoxService.ShowWarning("Warning", "This workspace is almost out of space.");
    }

    private async Task ShowSevereWarningMessageBox()
    {
        await messageBoxService.ShowSevereWarning("Severe warning", "This workspace is out of space.");
    }

    private async Task ShowErrorMessageBox()
    {
        await messageBoxService.ShowError("Error", "The file could not be uploaded.");
    }

    private async Task ShowConfirm()
    {
        confirmResult = await messageBoxService.Confirm("Publish", "Publish these changes to production?");
    }

    private async Task ShowDangerousConfirm()
    {
        confirmResult = await messageBoxService.Confirm(new()
        {
            Title = "Delete the workspace?",
            Body = "Everything in it is removed. This cannot be undone.",
            Color = BitColor.Error,
            Buttons = BitMessageBoxButtons.YesNo,
            PrimaryButtonColor = BitColor.Error,
            YesText = "Delete forever",
            NoText = "Keep it",
            DefaultButton = BitMessageBoxResult.No,
            IconAriaLabel = "Error"
        });
    }
}
