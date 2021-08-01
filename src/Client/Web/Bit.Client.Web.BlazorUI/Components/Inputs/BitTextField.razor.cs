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
        private bool isMultiLine;
        private bool isReadonly;
        private bool isRequired;
        private bool isUnderlined;
        private bool isBorderless;
        private string focusClass = "";
        private TextFieldType type = TextFieldType.Text;
        private Guid InputId = Guid.NewGuid();

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
        public bool IsUnderlined
        {
            get => isUnderlined;
            set
            {
                isUnderlined = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public bool IsBorderless
        {
            get => isBorderless;
            set
            {
                isBorderless = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Current value of the text field
        /// </summary>
        [Parameter] public string? Value { get; set; }

        [Parameter] public string? DefaultValue { get; set; }

        /// <summary>
        /// Input placeholder text
        /// </summary>
        [Parameter] public string? Placeholder { get; set; }

        /// <summary>
        /// Label displayed above the text field and read by screen readers
        /// </summary>
        [Parameter] public string? Label { get; set; }

        [Parameter] public RenderFragment? RenderLabel { get; set; }

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

        [Parameter] public string? Prefix { get; set; }

        [Parameter] public string? Suffix { get; set; }

        [Parameter] public string? ErrorMessage { get; set; }

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

        [Parameter] public int DeferredValidationTime { get; set; } = 200;

        [Parameter] public bool ValidateOnLoad { get; set; } = true;

        [Parameter] public bool ValidateOnFocusOut { get; set; }

        [Parameter] public bool ValidateOnFocusIn { get; set; }

        [Parameter] public Func<string, string>? OnGetErrorMessage { get; set; }

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
        /// Callback for when the input value changes
        /// </summary>
        [Parameter] public EventCallback<ChangeEventArgs> OnInput { get; set; }

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
            ClassBuilder.Register(() => IsMultiLine && Type == TextFieldType.Text
                                        ? $"{RootElementClass}-multiline-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled && IsReadonly
                                        ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled && IsRequired
                                        ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsUnderlined
                                       ? $"{RootElementClass}-underlined-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsBorderless
                                       ? $"{RootElementClass}-borderless-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled is false
                                        ? $"{RootElementClass}-{(IsUnderlined ? "underlined-" : "")}disabled-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => FocusClass.HasValue()
                                        ? $"{RootElementClass}-{(IsUnderlined ? "underlined-" : "")}{(ErrorMessage.HasValue() ? "haserror-" : "")}{FocusClass}-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => ErrorMessage.HasValue()
                           ? $"{RootElementClass}-haserror-{VisualClassRegistrar()}" : string.Empty);
        }

        protected virtual async Task HandleFocusIn(FocusEventArgs e)
        {
            if (IsEnabled)
            {
                FocusClass = "focused";
                await OnFocusIn.InvokeAsync(e);

                if (ValidateOnFocusIn)
                {
                    Validate(Value);
                }
            }
        }

        protected virtual async Task HandleFocusOut(FocusEventArgs e)
        {
            if (IsEnabled)
            {
                FocusClass = "";
                await OnFocusOut.InvokeAsync(e);

                if (ValidateOnFocusOut)
                {
                    Validate(Value);
                }
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

        protected virtual async Task HandleInput(ChangeEventArgs e)
        {
            if (IsEnabled)
            {
                await OnInput.InvokeAsync(e);
            }

            if (!ValidateOnFocusIn && !ValidateOnFocusOut)
            {
                await DeferredValidation(e.Value.ToString()).ConfigureAwait(false);
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

        private readonly ICollection<Task> DeferredValidationTasks = new List<Task>();

        private void Validate(string? value)
        {
            if (value != null)
            {
                string? errorMessage = OnGetErrorMessage?.Invoke(value!);
                if (ErrorMessage.HasNoValue() && OnGetErrorMessage != null && errorMessage.HasNoValue())
                {
                    ErrorMessage = "";
                }
                else
                {
                    ErrorMessage = errorMessage;
                    StateHasChanged();
                }
            }
        }

        private async Task DeferredValidation(string value)
        {
            if (DeferredValidationTime == 0)
            {
                Validate(value);
            }
            else
            {
                DeferredValidationTasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(DeferredValidationTime);
                }));
                int TaskCount = DeferredValidationTasks.Count;
                await Task.WhenAll(DeferredValidationTasks.ToArray());
                if (TaskCount == DeferredValidationTasks.Count)
                {
                    _ = Task.Run(() =>
                    {
                        InvokeAsync(() =>
                        {
                            Validate(value);
                            StateHasChanged();
                        });
                        //invokeasync required for serverside
                    }).ConfigureAwait(false);
                }
            }
        }

        protected override Task OnParametersSetAsync()
        {
            if (DefaultValue.HasValue())
            {
                Value = DefaultValue;
                DefaultValue = default;
            }

            return base.OnParametersSetAsync();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender & ValidateOnLoad)
            {
                Validate(Value);
            }
            base.OnAfterRenderAsync(firstRender);
        }


    }
}
