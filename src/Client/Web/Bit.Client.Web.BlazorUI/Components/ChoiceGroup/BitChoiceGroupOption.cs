using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Components.ChoiceGroup
{
    public class BitChoiceGroupOption
    {
        public string? Key { get; set; }

        public string? Text { get; set; }

        public bool IsEnabled { get; set; } = true;

        public BitIconName? iconName { get; set; }

        public string? ImageAlt { get; set; }

        public Size? ImageSize { get; set; }

        public string? ImageSrc { get; set; }

        public string? SelectedImageSrc { get; set; }
    }
}
