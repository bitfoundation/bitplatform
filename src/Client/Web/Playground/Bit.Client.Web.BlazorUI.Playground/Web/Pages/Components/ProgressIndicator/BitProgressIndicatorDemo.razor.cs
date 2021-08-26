using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ProgressIndicator
{
    public partial class BitProgressIndicatorDemo
    {
        private string description = "Push button to start!";

        public int CompletedPercent { get; set; }

        private async Task StartProgress()
        {
            CompletedPercent = 0;

            while (CompletedPercent <= 100)
            {
                if (CompletedPercent == 100)
                {
                    description = $"Completed !";
                    break;
                }
                else
                {
                    CompletedPercent++;
                    description = $"{CompletedPercent}%";
                }

                StateHasChanged();
                await Task.Delay(100);
            }
        }
    }
}
