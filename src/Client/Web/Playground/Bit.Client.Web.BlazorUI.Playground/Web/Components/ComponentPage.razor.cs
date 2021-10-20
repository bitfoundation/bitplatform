﻿using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Components
{
    public partial class ComponentPage
    {
        [Parameter] public string ComponentName { get; set; }
        [Parameter] public string OverviewDesc { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public List<EnumParameter> EnumParameters { get; set; }
        [Parameter] public List<ComponentParameter> ComponentParameters { get; set; }

    }
}
