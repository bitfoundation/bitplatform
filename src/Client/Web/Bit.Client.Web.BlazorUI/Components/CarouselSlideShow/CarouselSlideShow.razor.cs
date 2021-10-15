using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class CarouselSlideShow
    {
        int currentPosition;
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;
        [Parameter] public Type[] Components { get; set; }

        protected override async Task OnInitializedAsync()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            Walkthrough();
        }

        protected override string RootElementClass => "bit-sldshw";

        public void Manualy(bool backwards)
        {
            if (!cancellationTokenSource.IsCancellationRequested)
                cancellationTokenSource.Cancel();
            if (backwards)
                currentPosition--;
            else
                currentPosition++;
        }

        public async Task Walkthrough()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(2500, cancellationToken);
                currentPosition++;
                await InvokeAsync(() => this.StateHasChanged());
            }
        }
        public RenderFragment Render(int position)
        {
            return RenderTreeBuilder =>
            {
                RenderTreeBuilder.OpenComponent(0, Components[Math.Abs(position % Components.Length)]);
                RenderTreeBuilder.CloseComponent();
            };
        }
    }
}
