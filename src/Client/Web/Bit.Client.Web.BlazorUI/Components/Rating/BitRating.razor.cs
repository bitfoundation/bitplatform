using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitRating
    {
        [Parameter] public short IconCount { get; set; } = 5;

        [Parameter] public IconType IconType { get; set; }

        [Parameter] public short IconWidth { get; set; } = 20;

        [Parameter] public short IconHeight { get; set; } = 20;

        [Parameter] public string ActiveColor { get; set; } = "#fed049";

        [Parameter] public string EmptyColor { get; set; } = "#dddddd";

        [Parameter] public string HoverColor { get; set; } = "#ffefa1";

        [Parameter] public double IntialValue { get; set; }

        [Parameter] public bool ShowIconCount { get; set; } = true;

        [Parameter] public bool IsReadonly { get; set; } = false;

        public double IconSelectedCount { get; set; }
        public string[] FirstCurrentColor { get; set; } = new string[21];
        public string[] SecondCurrentColor { get; set; } = new string[21];

        protected virtual void HandleClick(int index, IconSide side = IconSide.Right)
        {
            SetIconSelectedCount(index, side);
            FillRatingIcons(index, side, ActiveColor);
        }

        protected virtual void HandleMouseMove(int selectedIndex, IconSide side = IconSide.Right)
        {
            FillRatingIcons(selectedIndex, side, HoverColor);
        }

        protected virtual void HandelMouseOut()
        {
            FillRatingIconWithDefaulValue(IconSelectedCount);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                EmptyRatingIcons();
                FillRatingIconWithDefaulValue(IntialValue);
                SetIconSelectedCount((int)Math.Round(IntialValue, MidpointRounding.AwayFromZero), GetIconSideByValue(IntialValue));

                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        protected override string GetElementClass()
        {
            ElementClassContainer.Clear();
            ElementClassContainer.Add("bit-rating");

            if (IsReadonly)
            {
                ElementClassContainer.Add("read-only");
            }

            return base.GetElementClass();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(IconType):
                        IconType = (IconType)parameter.Value;
                        break;

                    case nameof(IconCount):
                        IconCount = (short)parameter.Value > 20 ? (short)20 : (short)parameter.Value;
                        break;

                    case nameof(IconWidth):
                        IconWidth = (short)parameter.Value;
                        break;

                    case nameof(IconHeight):
                        IconHeight = (short)parameter.Value;
                        break;

                    case nameof(ActiveColor):
                        ActiveColor = (string)parameter.Value;
                        break;

                    case nameof(EmptyColor):
                        EmptyColor = (string)parameter.Value;
                        break;

                    case nameof(HoverColor):
                        HoverColor = (string)parameter.Value;
                        break;

                    case nameof(IntialValue):
                        IntialValue = (double)parameter.Value > IconCount ? IconCount : (double)parameter.Value;
                        break;

                    case nameof(ShowIconCount):
                        ShowIconCount = (bool)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }

        void FillRatingIconWithDefaulValue(double value)
        {
            IconSide side = GetIconSideByValue(value);
            FillRatingIcons((int)Math.Round(value, MidpointRounding.AwayFromZero), side, ActiveColor);
        }

        void FillRatingIcons(int index, IconSide side, string color)
        {
            EmptyRatingIcons();

            for (var item = 1; item <= index; item++)
            {
                FirstCurrentColor[item] = color;

                if (IconType == IconType.Star)
                {
                    if (item == index && side == IconSide.Left)
                        break;
                    else
                        SecondCurrentColor[item] = color;
                }
            }
        }

        void EmptyRatingIcons()
        {
            Array.Fill(FirstCurrentColor, EmptyColor);
            Array.Fill(SecondCurrentColor, EmptyColor);
        }

        void SetIconSelectedCount(int index, IconSide side)
        {
            if (IconType == IconType.Star && side == IconSide.Left)
                IconSelectedCount = (double)(index - 0.5);
            else
                IconSelectedCount = index;
        }

        IconSide GetIconSideByValue(double value)
        {
            if ((value % 1) == 0) return IconSide.Right;
            else return IconSide.Left;
        }
    }
}
