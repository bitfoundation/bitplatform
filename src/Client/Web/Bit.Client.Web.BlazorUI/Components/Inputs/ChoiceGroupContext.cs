using Microsoft.AspNetCore.Components;
using System;

namespace Bit.Client.Web.BlazorUI
{
    public class ChoiceGroupContext
    {
        public string Name { get; }
        public EventHandler<ChangeEventArgs> ChangeEventHandler { get; }

        public ChoiceGroupContext(string name,
            EventHandler<ChangeEventArgs> changeEventHandler)
        {
            Name = name;
            ChangeEventHandler = changeEventHandler;
        }

    }
}
