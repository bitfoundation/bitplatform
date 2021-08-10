using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitTextField
    {
        private bool isMultiline;
        private bool isReadonly;
        private bool isRequired;
        private bool isUnderlined;
        private bool hasBorder = true;
        private string focusClass = "";
        private TextFieldType type = TextFieldType.Text;
        private Guid InputId = Guid.NewGuid();
        private string? textValue;
        private bool ValueHasBeenSet;

        /// <summary>
        /// Whether or not the text field is a Multiline text field
        /// </summary>
        [Parameter]
        public bool IsMultiline
        {
            get => isMultiline;
            set
            {
                isMultiline = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// If true, the text field is readonly
        /// </summary>
        [Parameter]
        public bool IsReadonly
        {
            get => isReadonly;
            set
            {
                isReadonly = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Whether the associated input is required or not, add an asterisk "*" to its label
        /// </summary>
        [Parameter]
        public bool IsRequired
        {
            get => isRequired;
            set
            {
                isRequired = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Whether or not the text field is underlined
        /// </summary>
        [Parameter]
        public bool IsUnderlined
        {
            get => isUnderlined;
            set
            {
                isUnderlined = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Whether or not the text field is borderless
        /// </summary>
        [Parameter]
        public bool HasBorder
        {
            get => hasBorder;
            set
            {
                hasBorder = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Current value of the text field
        /// </summary>
        [Parameter]
        public string? Value
        {
            get => textValue;
            set
            {
                if (value == textValue) return;
                textValue = value;
                
                _ = ValueChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        /// Callback for when the input value changes
        /// </summary>
        [Parameter] public EventCallback<string?> ValueChanged { get; set; }
        [Parameter] public EventCallback<string?> OnChange { get; set; }
        /// <summary>
        /// Default value of the text field. Only provide this if the text field is an uncontrolled component; otherwise, use the value property
        /// </summary>
        [Parameter] public string? DefaultValue { get; set; }

        /// <summary>
        /// Input placeholder text
        /// </summary>
        [Parameter] public string? Placeholder { get; set; }

        /// <summary>
        /// Label displayed above the text field and read by screen readers
        /// </summary>
        [Parameter] public string? Label { get; set; }

        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Description displayed below the text field to provide additional details about what text to enter.
        /// </summary>
        [Parameter] public string? Description { get; set; }

        [Parameter] public RenderFragment? RenderDescription { get; set; }

        /// <summary>
        /// Specifies the maximum number of characters allowed in the input
        /// </summary>
        [Parameter] public int MaxLength { get; set; } = -1;

        /// <summary>
        /// The icon name for the icon shown in the far right end of the text field
        /// </summary>
        [Parameter] public string? IconName { get; set; }

        /// <summary>
        /// Prefix displayed before the text field contents. This is not included in the value.
        /// Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.
        /// </summary>
        [Parameter] public string? Prefix { get; set; }

        /// <summary>
        /// Suffix displayed after the text field contents. This is not included in the value. 
        /// Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.
        /// </summary>
        [Parameter] public string? Suffix { get; set; }

        /// <summary>
        /// Whether to show the reveal password button for input type 'password'
        /// </summary>
        [Parameter] public bool CanRevealPassword { get; set; }

        /// <summary>
        /// Input type
        /// </summary>
        [Parameter]
        public TextFieldType Type
        {
            get => type;
            set
            {
                type = value;
                ElementType = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Callback for when focus moves into the input
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

        /// <summary>
        /// Callback for when focus moves out of the input
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

        /// <summary>
        /// Callback for when focus moves into the input
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }


        /// <summary>
        /// Callback for when a keyboard key is pressed
        /// </summary>
        [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

        /// <summary>
        /// Callback for When a keyboard key is released
        /// </summary>
        [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

        /// <summary>
        /// Callback for when the input clicked
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        public TextFieldType ElementType { get; set; }

        public string FocusClass
        {
            get => focusClass;
            set
            {
                focusClass = value;
                ClassBuilder.Reset();
            }
        }

        protected override string RootElementClass => "bit-txt";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsMultiline && Type == TextFieldType.Text
                                        ? $"{RootElementClass}-multiline-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled && IsReadonly
                                        ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled && IsRequired
                                        ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsUnderlined
                                       ? $"{RootElementClass}-underlined-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => HasBorder is false
                                       ? $"{RootElementClass}-no-border-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled is false
                                        ? $"{RootElementClass}-{(IsUnderlined ? "underlined-" : "")}disabled-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => FocusClass.HasValue()
                                        ? $"{RootElementClass}-{(IsUnderlined ? "underlined-" : "")}{FocusClass}-{VisualClassRegistrar()}" : string.Empty);
        }

        protected virtual async Task HandleFocusIn(FocusEventArgs e)
        {
            if (IsEnabled)
            {
                FocusClass = "focused";
                await OnFocusIn.InvokeAsync(e);
            }
        }

        protected virtual async Task HandleFocusOut(FocusEventArgs e)
        {
            if (IsEnabled)
            {
                FocusClass = "";
                await OnFocusOut.InvokeAsync(e);
            }
        }

        protected virtual async Task HandleFocus(FocusEventArgs e)
        {
            if (IsEnabled)
            {
                FocusClass = "focused";
                await OnFocus.InvokeAsync(e);
            }
        }

        protected virtual async Task HandleChange(ChangeEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
            Value = e.Value?.ToString();
            await OnChange.InvokeAsync(Value);
        }

        protected virtual async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (IsEnabled)
            {
                await OnKeyDown.InvokeAsync(e);
            }
        }

        protected virtual async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (IsEnabled)
            {
                await OnKeyUp.InvokeAsync(e);
            }
        }

        protected virtual async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
            }
        }

        public void TogglePasswordRevealIcon()
        {
            ElementType = ElementType == TextFieldType.Text ? TextFieldType.Password : TextFieldType.Text;
        }

        protected override Task OnInitializedAsync()
        {
            if (DefaultValue.HasValue())
            {
                Value = DefaultValue;
            }
            return base.OnInitializedAsync();
        }
    }
}
