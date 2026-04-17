namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TagsInput;

public partial class BitTagsInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the input should receive focus on first render.",
        },
        new()
        {
            Name = "CancelConfirmKeysOnEmpty",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, pressing Enter while the input is empty does not suppress the event, allowing it to propagate (e.g., to submit a parent form).",
        },
        new()
        {
            Name = "Classes",
            Type = "BitTagsInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the tags input.",
            LinkType = LinkType.Link,
            Href = "#tagsinput-class-styles",
        },
        new()
        {
            Name = "DismissIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon for the dismiss button using custom CSS classes for external icon libraries. Takes precedence over DismissIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string?",
            DefaultValue = "Cancel",
            Description = "Gets or sets the name of the icon for the dismiss button from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "Duplicates",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether duplicate tags are allowed.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The label displayed above the input.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A custom template for the label.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int",
            DefaultValue = "0",
            Description = "The maximum number of characters allowed for each individual tag. 0 means no limit.",
        },
        new()
        {
            Name = "MaxTags",
            Type = "int",
            DefaultValue = "0",
            Description = "The maximum number of tags allowed. 0 means no limit.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default border of the tags input.",
        },
        new()
        {
            Name = "OnBeforeAdd",
            Type = "EventCallback<BitTagsInputBeforeArgs>",
            Description = "Callback invoked before a tag is added. Set args.Cancel = true to cancel the add.",
            LinkType = LinkType.Link,
            Href = "#before-args",
        },
        new()
        {
            Name = "OnBeforeRemove",
            Type = "EventCallback<BitTagsInputBeforeArgs>",
            Description = "Callback invoked before a tag is removed. Set args.Cancel = true to cancel the remove.",
            LinkType = LinkType.Link,
            Href = "#before-args",
        },
        new()
        {
            Name = "OnAdd",
            Type = "EventCallback<IReadOnlyList<string>>",
            Description = "Callback for when one or more tags are added. Receives the list of all newly added tags.",
        },
        new()
        {
            Name = "OnTagExists",
            Type = "EventCallback<string>",
            Description = "Callback fired when a duplicate tag is attempted and Duplicates is false.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the input receives focus.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the input loses focus.",
        },
        new()
        {
            Name = "OnRemove",
            Type = "EventCallback<string>",
            Description = "Callback for when a tag is removed.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Placeholder text for the input. Hidden when tags are present.",
        },
        new()
        {
            Name = "Separators",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The character(s) used to separate tags when typing. Also used to split pasted text into multiple tags. Defaults to Enter key only.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitTagsInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the tags input.",
            LinkType = LinkType.Link,
            Href = "#tagsinput-class-styles",
        },
        new()
        {
            Name = "TagTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "A custom template for rendering each tag.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "before-args",
            Title = "BitTagsInputBeforeArgs",
            Description = "Arguments passed to the OnBeforeAdd and OnBeforeRemove callbacks.",
            Parameters =
            [
                new()
                {
                    Name = "Tag",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "The tag text being added or removed.",
                },
                new()
                {
                    Name = "Cancel",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Set to true to cancel the add or remove operation.",
                },
            ]
        },
        new()
        {
            Id = "tagsinput-class-styles",
            Title = "BitTagsInputClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the tags input.",
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focus state of the tags input.",
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the tags input.",
                },
                new()
                {
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input container of the tags input.",
                },
                new()
                {
                    Name = "Tag",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each tag element.",
                },
                new()
                {
                    Name = "TagText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the tag text.",
                },
                new()
                {
                    Name = "DismissButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss button of each tag.",
                },
                new()
                {
                    Name = "DismissIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss icon of each tag.",
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text input element.",
                },
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the input element of the BitTagsInput.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the input element of the BitTagsInput.",
        },
        new()
        {
            Name = "Clear",
            Type = "Task",
            Description = "Removes all tags.",
        }
    ];

    private ICollection<string>? boundTags;
    private ICollection<string>? eventTags;
    private string? addedTag;
    private string? removedTag;

    private string? eventsStatus;
    private string? tagExistsMsg;

    private bool cancelFormSubmitted;
    private readonly ValidationTagsInputModel cancelModel = new();

    private readonly ValidationTagsInputModel validationModel = new();

    private void HandleValidSubmit() { }

    private void HandleBeforeAdd(BitTagsInputBeforeArgs args)
    {
        if (args.Tag.Equals("block", StringComparison.OrdinalIgnoreCase))
        {
            args.Cancel = true;
            eventsStatus = $"Adding '{args.Tag}' was blocked by OnBeforeAdd.";
        }
        else
        {
            eventsStatus = $"Tag '{args.Tag}' added.";
        }
        tagExistsMsg = null;
    }

    private void HandleTagExists(string tag)
    {
        tagExistsMsg = $"Tag '{tag}' already exists!";
    }

    private void HandleBeforeRemove(BitTagsInputBeforeArgs args)
    {
        eventsStatus = $"Removing '{args.Tag}'.";
        tagExistsMsg = null;
    }
}
