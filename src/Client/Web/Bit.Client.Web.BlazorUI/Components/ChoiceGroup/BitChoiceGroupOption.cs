using System.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Components.ChoiceGroup
{
    public class BitChoiceGroupOption
    {
        public string? Value { get; set; }

        public string? Text { get; set; }

        public bool IsEnabled { get; set; } = true;

        public BitIconName? IconName { get; set; }

        public string? ImageAlt { get; set; }

        public Size? ImageSize { get; set; }

        public string? ImageSrc { get; set; }

        public string? SelectedImageSrc { get; set; }

        public string? AriaLabel { get; set; }

        public string? LabelId { get; set; }

        public string? Id { get; set; }
    }
}
