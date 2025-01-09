namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MessageBox;

public partial class BitMessageBoxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "Body",
            Type = "string?",
            DefaultValue = "null",
            Description = "The body of the message box.",
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
            Name = "OkText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Ok button.",
         },
         new()
         {
            Name = "OnClose",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The event callback for closing the message box.",
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
                    Name = "OkButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the OkButton of the BitMessageBox.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#class-styles"
                }
            ]
        }
    ];


    private bool isModalOpen;

    [AutoInject] private BitModalService modalService { get; set; } = default!;
    private async Task ShowMessageBox()
    {
        BitModalReference modalRef = default!;
        Dictionary<string, object> parameters = new()
        {
            { nameof(BitMessageBox.Title), "This is a title" },
            { nameof(BitMessageBox.Body), "This is a body." },
            { nameof(BitMessageBox.OnClose), EventCallback.Factory.Create(this, () => modalRef.Close()) }
        };
        modalRef = await modalService.Show<BitMessageBox>(parameters);
    }

    [AutoInject] private BitMessageBoxService messageBoxService { get; set; } = default!;
    private async Task ShowMessageBoxService()
    {
        await messageBoxService.Show("TITLE", "BODY");
    }
}
