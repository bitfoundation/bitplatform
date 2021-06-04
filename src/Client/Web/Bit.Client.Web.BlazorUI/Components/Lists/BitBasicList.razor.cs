using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
//using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitBasicList<TItem>
    {
        //[Inject] public IJSRuntime JSRuntime { get; set; }
        [Parameter] public ICollection<TItem> Items { get; set; } = Array.Empty<TItem>();
        [Parameter] public string Role { get; set; } = "list";
        [Parameter] public bool Virtualize { get; set; } = true;
        [Parameter] public string? LoadingText { get; set; }
        [Parameter] public int OverscanCount { get; set; } = 3;
        [Parameter] public int ItemSize { get; set; } = 50;
        [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }
        //[Parameter] public string PageHeight { get; set; }        
        //[Parameter] public int ItemPerPage { get; set; }
        
        protected override string RootElementClass => "bit-bsc-lst";
        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Items):
                        Items = (System.Collections.Generic.ICollection<TItem>)parameter.Value;
                        break;
                    //case nameof(ItemPerPage):
                    //    ItemPerPage = (int)parameter.Value;
                        break;
                    case nameof(Role):
                        Role = (string)parameter.Value;
                        break;
                    case nameof(Virtualize):
                        Virtualize = (bool)parameter.Value;
                        break;
                    case nameof(LoadingText):
                        LoadingText = (string)parameter.Value;
                        break;
                    case nameof(OverscanCount):
                        OverscanCount = (int)parameter.Value;
                        break;
                    case nameof(ItemSize):
                        ItemSize = (int)parameter.Value;
                        break;
                    case nameof(RowTemplate):
                        RowTemplate = (Microsoft.AspNetCore.Components.RenderFragment<TItem>)parameter.Value;
                        break;
                    //case nameof(PageHeight):
                    //    PageHeight = (string)parameter.Value;
                    //    break;
                }
            }
            return base.SetParametersAsync(parameters);
        }

        #region autoPageHeight
        //protected override void RegisterComponentStyles() => StyleBuilder.Register(() => PageHeight!.HasValue() ? $"height: {PageHeight}" : string.Empty);

        //protected override async Task OnAfterRenderAsync(bool firstRender)
        //{
        //    //add classes to all renderFragment elements
        //    await JSRuntime.AddClass(".bit-bsc-lst", "lst-item");
        //    if (firstRender)
        //    {
        //        //calculet page height base on ItemPerPAge and first child element in list
        //        if (PageHeight is null)
        //        {
        //            decimal height = await JSRuntime.GetHeight(".bit-bsc-lst .lst-item:nth-child(2)");
        //            PageHeight = (ItemPerPage * (int)height).ToString() + "px";
        //            StyleBuilder.Reset();
        //            StateHasChanged();
        //        }
        //    }

        //    await base.OnAfterRenderAsync(firstRender);
        //}
        #endregion




    }
}
