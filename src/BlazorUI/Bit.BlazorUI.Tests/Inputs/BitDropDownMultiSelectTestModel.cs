﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    public class BitDropDownMultiSelectTestModel
    {
        [Required]
        [MaxLength(2)]
        [MinLength(2)]
        public List<string> Values { get; set; }
    }
}
