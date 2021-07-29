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

        [Parameter] public string? Value { get; set; }

        [Parameter] public string? DefaultValue { get; set; }

        [Parameter] public string? Placeholder { get; set; }

        [Parameter] public string? Label { get; set; }

        [Parameter] public string? Description { get; set; }

        [Parameter] public int MaxLength { get; set; } = -1;

        [Parameter] public string? IconName { get; set; }

        [Parameter] public string? Prefix { get; set; }

        [Parameter] public string? Suffix { get; set; }

        [Parameter] public string? ErrorMessage { get; set; }

        [Parameter] public bool CanRevealPassword { get; set; }

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

        [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

        [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

        [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

        [Parameter] public EventCallback<ChangeEventArgs> OnInput { get; set; }

        [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

        [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

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
