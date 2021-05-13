using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitPivotItem : IDisposable
    {
        protected override string RootElementClass => "bit-pvt-itm";

        [CascadingParameter] protected BitPivot Pivot { get; set; }

        protected override Task OnInitializedAsync()
        {
            if (Pivot is not null)
            {
                //Pivot.RegisterOption(this);
                //if (string.IsNullOrEmpty(Name))
                //{
                //    Name = ChoiceGroup.Name;
                //}
            }

            return base.OnInitializedAsync();
        }

        public void Dispose()
        {
            if (Pivot is null) return;

            Pivot.UnregisterOption(this);
        }
    }
}
