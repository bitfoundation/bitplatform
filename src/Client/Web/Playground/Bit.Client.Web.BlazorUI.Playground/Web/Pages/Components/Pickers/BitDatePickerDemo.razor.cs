namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Pickers
{
    public partial class BitDatePickerDemo
    {
        private string selectedDate = "";

        private string OnDateFormat(BitDate date)
        {
            return $"{date.GetDate()}/{date.GetMonth()}/{date.GetYear()}";
        }
    }
}
