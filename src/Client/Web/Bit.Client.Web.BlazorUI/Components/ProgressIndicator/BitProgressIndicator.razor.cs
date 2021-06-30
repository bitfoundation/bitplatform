using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitProgressIndicator
    {
        private double percentComplete;
        [Parameter] public string Label { get; set; } = string.Empty;
        [Parameter] public string Description { get; set; } = string.Empty;
        [Parameter]
        public double PercentComplete
        {
            get => percentComplete;
            set
            {
                if (value == percentComplete) return;
                percentComplete = value;
                _ = PercentCompleteChanged.InvokeAsync(value);
            }
        }

        [Parameter] public EventCallback<double> PercentCompleteChanged { get; set; }
        protected override string RootElementClass => "bit-pi";
        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => PercentCompleteChanged.HasDelegate
                                                ? string.Empty
                                                : $"{RootElementClass}-indeterminate-{VisualClassRegistrar()}");
        }
    }
}
