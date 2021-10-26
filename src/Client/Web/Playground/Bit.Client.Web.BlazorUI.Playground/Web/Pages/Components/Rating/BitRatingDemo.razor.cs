﻿using System.Collections.Generic;
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
                Name = "allowZeroStars",
                Type = "bool",
                DefaultValue = "false",
                Description = "Allow the initial rating value be 0. Note that a value of 0 still won't be selectable by mouse or keyboard.",
            },
            new ComponentParameter()
            {
                Name = "ariaLabelFormat",
                Type = "string",
                DefaultValue = "",
                Description = "Optional label format for each individual rating star (not the rating control as a whole) that will be read by screen readers.",
            },
            new ComponentParameter()
            {
                Name = "defaultRating",
                Type = "double",
                DefaultValue = "0",
                Description = "Default rating. Must be a number between min and max. Only provide this if the Rating is an uncontrolled component; otherwise, use the rating property.",
            },
            new ComponentParameter()
            {
                Name = "getAriaLabel",
                Type = "Func<double, double, string>",
                DefaultValue = "",
                Description = "Optional callback to set the aria-label for rating control in readOnly mode. Also used as a fallback aria-label if ariaLabel prop is not provided.",
            },
            new ComponentParameter()
            {
                Name = "icon",
                Type = "string",
                DefaultValue = "FavoriteStarFill",
                Description = "Custom icon name for unselected rating elements, If unset, default will be the FavoriteStar icon.",
            },
            new ComponentParameter()
            {
                Name = "isReadOnly",
                Type = "bool",
                DefaultValue = "false",
                Description = "A flag to mark rating control as readOnly.",
            },
            new ComponentParameter()
            {
                Name = "max",
                Type = "int",
                DefaultValue = "5",
                Description = "Maximum rating. Must be >= min (0 if AllowZeroStars is true, 1 otherwise).",
            },
            new ComponentParameter()
            {
                Name = "onChange",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback that is called when the rating has changed.",
            },
            new ComponentParameter()
            {
                Name = "rating",
                Type = "double",
                DefaultValue = "0",
                Description = "Current rating value. Must be a number between min (0 if AllowZeroStars is true, 1 otherwise) and max.",
            },
            new ComponentParameter()
            {
                Name = "ratingChanged",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback that is called when the rating value changed.",
            },
            new ComponentParameter()
            {
                Name = "size",
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
                Title = "Ratingsize enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "small",
                        Description="Display rating icon using small size.",
                        Value="small = 0",
                    },
                    new EnumItem()
                    {
                        Name= "large",
                        Description=" Display rating icon using large size.",
                        Value="large = 1",
                    },
                }
            }
        };
    }
}
