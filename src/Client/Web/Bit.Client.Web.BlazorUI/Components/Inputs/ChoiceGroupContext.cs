using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI.Components.Inputs
{
    public class ChoiceGroupContext
    {
        private readonly ChoiceGroupContext? _parentContext;
        public string Name { get; }
        public EventCallback<ChangeEventArgs> ChangeEventCallback { get; }
        public ChoiceGroupContext(ChoiceGroupContext? parentContext,
            string name,
            EventCallback<ChangeEventArgs> changeEventCallback)
        {
            _parentContext = parentContext;

            Name = name;
            ChangeEventCallback = changeEventCallback;
        }
        public ChoiceGroupContext? FindContextInAncestors(string name)
            => string.Equals(Name, name) ? this : _parentContext?.FindContextInAncestors(name);
    }
}
