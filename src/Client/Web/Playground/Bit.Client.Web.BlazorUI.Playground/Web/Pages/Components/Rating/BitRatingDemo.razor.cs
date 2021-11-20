using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Rating
{
    public partial class BitRatingDemo
    {
        private string RatingChangedText = string.Empty;

        private double RatingBoundValue = 2;
        private double RatingLargeValue = 3;
        private double RatingSmallValue = 0;
        private double RatingReadOnlyValue = 2.5;
        private double RatingCustomIconValue = 2.5;
        private double RatingOutsideValue = 0;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "AllowZeroStars",
                Type = "bool",
                DefaultValue = "false",
                Description = "Allow the initial rating value be 0. Note that a value of 0 still won't be selectable by mouse or keyboard.",
            },
            new ComponentParameter()
            {
                Name = "AriaLabelFormat",
                Type = "string",
                DefaultValue = "",
                Description = "Optional label format for each individual rating star (not the rating control as a whole) that will be read by screen readers.",
            },
            new ComponentParameter()
            {
                Name = "DefaultRating",
                Type = "double",
                DefaultValue = "0",
                Description = "Default rating. Must be a number between min and max. Only provide this if the Rating is an uncontrolled component; otherwise, use the rating property.",
            },
            new ComponentParameter()
            {
                Name = "GetAriaLabel",
                Type = "Func<double, double, string>",
                DefaultValue = "",
                Description = "Optional callback to set the aria-label for rating control in readOnly mode. Also used as a fallback aria-label if ariaLabel prop is not provided.",
            },
            new ComponentParameter()
            {
                Name = "Icon",
                Type = "string",
                DefaultValue = "FavoriteStarFill",
                Description = "Custom icon name for unselected rating elements, If unset, default will be the FavoriteStar icon.",
            },
            new ComponentParameter()
            {
                Name = "IsReadOnly",
                Type = "bool",
                DefaultValue = "false",
                Description = "A flag to mark rating control as readOnly.",
            },
            new ComponentParameter()
            {
                Name = "Max",
                Type = "int",
                DefaultValue = "5",
                Description = "Maximum rating. Must be >= min (0 if AllowZeroStars is true, 1 otherwise).",
            },
            new ComponentParameter()
            {
                Name = "OnChange",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback that is called when the rating has changed.",
            },
            new ComponentParameter()
            {
                Name = "Rating",
                Type = "double",
                DefaultValue = "0",
                Description = "Current rating value. Must be a number between min (0 if AllowZeroStars is true, 1 otherwise) and max.",
            },
            new ComponentParameter()
            {
                Name = "RatingChanged",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback that is called when the rating value changed.",
            },
            new ComponentParameter()
            {
                Name = "Size",
                Type = "BitRatingSize",
                DefaultValue = "BitRatingSize.small",
                LinkType = LinkType.Link,
                Href = "#rating-size-enum",
                Description = "Size of rating.",
            }
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "rating-size-enum",
                Title = "BitRatingSize Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Small",
                        Description="Display rating icon using small size.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Large",
                        Description=" Display rating icon using large size.",
                        Value="1",
                    },
                }
            }
        };

        private readonly string ratingSampleCode = $"<BitRating Size='BitRatingSize.Large'{Environment.NewLine}" +
            $"@bind-Rating='RatingLargeValue'{Environment.NewLine}" +
            $"AriaLabelFormat='Select {{0}} of {{1}} stars'{Environment.NewLine}" +
            $"<BitRating AllowZeroStars='true'{Environment.NewLine}" +
            $"@bind-Rating='RatingSmallValue'/>{Environment.NewLine}" +
            $"<BitRating Max='10'{Environment.NewLine}" +
            $"OnChange='@(v => RatingChangedText = $'Rating value changed to {{v}}')'{Environment.NewLine}" +
            $"@bind-Rating='RatingBoundValue'/>{Environment.NewLine}" +
            $"<BitRating IsEnabled='false'{Environment.NewLine}" +
            $"@bind-Rating='RatingReadOnlyValue'/>{Environment.NewLine}" +
            $"<BitRating IsReadOnly='true'{Environment.NewLine}" +
            $"@bind-Rating='RatingReadOnlyValue'{Environment.NewLine}" +
            $"GetAriaLabel='@((value, max) => $'Half star in readOnly mode rating value is {{value.ToString()}} of {{max.ToString()}}')'/>{Environment.NewLine}" +
            $"<BitRating Icon='HeartFill'{Environment.NewLine}" +
            $"UnselectedIcon='Heart'{Environment.NewLine}" +
            $"AllowZeroStars='true'{Environment.NewLine}" +
            "@bind-Rating='RatingOutsideValue'/>" ;

        private readonly string controlledRatingSampleCode = $"<BitRating Icon='Emoji2'{Environment.NewLine}" +
          $"UnselectedIcon='EmojiNeutral'{Environment.NewLine}" +
          $"AllowZeroStars='true'{Environment.NewLine}" +
          $"<BitRating AllowZeroStars='true'{Environment.NewLine}" +
          $"@bind-Rating='RatingOutsideValue'/>{Environment.NewLine}" +
          $"<BitButton OnClick='() => RatingOutsideValue = 5'>Fully satisfied</BitButton>{Environment.NewLine}" +
          "<BitButton OnClick='() => RatingOutsideValue = 0'>Have no idea</BitButton>";
    }
}
