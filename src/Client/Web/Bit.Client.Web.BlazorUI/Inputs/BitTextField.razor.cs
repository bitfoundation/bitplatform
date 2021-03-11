using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Inputs
{
    public partial class BitTextField
    {
        [Parameter] public string Value { get; set; }
        [Parameter] public string Placeholder { get; set; }
        [Parameter] public bool IsReadonly { get; set; } = false;
        [Parameter] public TextFieldType Type { get; set; } = TextFieldType.Text;
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }
        [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }
        [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }
        [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }
        [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        public string ReadonlyClass => IsReadonly ? "Readonly" : "";
        public string FocusClass { get; set; } = "";

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
                FocusClass = "Focused";
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

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Value):
                        Value = (string)parameter.Value;
                        break;
                    case nameof(Placeholder):
                        Placeholder = (string)parameter.Value;
                        break;
                    case nameof(IsReadonly):
                        IsReadonly = (bool)parameter.Value;
                        break;
                    case nameof(Type):
                        Type = (TextFieldType)parameter.Value;
                        break;
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment)parameter.Value;
                        break;
                    case nameof(OnFocusIn):
                        OnFocusIn = (EventCallback<FocusEventArgs>)parameter.Value;
                        break;
                    case nameof(OnFocusOut):
                        OnFocusOut = (EventCallback<FocusEventArgs>)parameter.Value;
                        break;
                    case nameof(OnFocus):
                        OnFocus = (EventCallback<FocusEventArgs>)parameter.Value;
                        break;
                    case nameof(OnChange):
                        OnChange = (EventCallback<ChangeEventArgs>)parameter.Value;
                        break;
                    case nameof(OnKeyDown):
                        OnKeyDown = (EventCallback<KeyboardEventArgs>)parameter.Value;
                        break;
                    case nameof(OnKeyUp):
                        OnKeyUp = (EventCallback<KeyboardEventArgs>)parameter.Value;
                        break;
                    case nameof(OnClick):
                        OnClick = (EventCallback<MouseEventArgs>)parameter.Value;
                        break;
                }
            }
            return base.SetParametersAsync(parameters);
        }
    }

    public enum TextFieldType
    {
        Text,
        Password
    }

}
