using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitTextField
    {
        private bool isMultiLine;
        private bool isReadonly;
        private bool isRequired;
        private string focusClass = "";
        private TextFieldType type = TextFieldType.Text;
        private Guid InputId = Guid.NewGuid();

        /// <summary>
        /// Specifies the maximum number of characters allowed in the input
        /// </summary>
        [Parameter] public int MaxLength { get; set; } = -1;

        /// <summary>
        /// The icon name for the icon shown in the far right end of the text field
        /// </summary>
        [Parameter] public string? IconName { get; set; }

        /// <summary>
        /// Current value of the text field
        /// </summary>
        [Parameter] public string? Value { get; set; }

        /// <summary>
        /// Input placeholder text, displayed until input value change
        /// </summary>
        [Parameter] public string? Placeholder { get; set; }

        /// <summary>
        /// Label displayed above the text field and read by screen readers
        /// </summary>
        [Parameter] public string? Label { get; set; }

        /// <summary>
        /// Whether to show the reveal password button for input type 'password'. This will be ignored if the type is not set to 'password', or if the browser is known to have a built-in reveal button for password inputs (Edge, IE)
        /// </summary>
        [Parameter]
        public bool CanRevealPassword { get; set; }

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

        public TextFieldType ElementType { get; set; }

        /// <summary>
        /// Whether or not the text field is a multiline text field
        /// </summary>
        [Parameter]
        public bool IsMultiLine
        {
            get => isMultiLine;
            set
            {
                isMultiLine = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Callback for when focus moves into the input
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

        /// <summary>
        /// Callback for when focus moves into the input
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

        /// <summary>
        /// Callback for when focus moves out of the input
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

        /// <summary>
        /// Callback for when the input value changes
        /// </summary>
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

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
            ClassBuilder.Register(() => IsMultiLine && Type == TextFieldType.Text
                                        ? $"{RootElementClass}-multiline-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled is false
                                        ? $"{RootElementClass}-disabled-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled && IsReadonly
                                        ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled && IsRequired
                                        ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => FocusClass.HasValue()
                                        ? $"{RootElementClass}-{FocusClass}-{VisualClassRegistrar()}" : string.Empty);
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
            if (IsEnabled)
            {
                await OnChange.InvokeAsync(e);
            }
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
    }
}
