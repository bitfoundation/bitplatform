using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Modal
{
    public partial class BitModalDemo
    {
        private bool IsOpen = false;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of Modal, It can be Any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "IsAlert",
                Type = "bool?",
                DefaultValue = "",
                Description = "Determines the ARIA role of the dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by IsBlocking and IsModeless.",
            },
            new ComponentParameter()
            {
                Name = "IsBlocking",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the dialog can be light dismissed by clicking outside the dialog (on the overlay).",
            },
            new ComponentParameter()
            {
                Name = "IsModeless",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the dialog). if true: IsBlocking is ignored, there will be no overlay.",
            },
            new ComponentParameter()
            {
                Name = "IsOpen",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the dialog is displayed.",
            },
            new ComponentParameter()
            {
                Name = "OnDismiss",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "A callback function for when the Modal is dismissed light dismiss, before the animation completes.",
            },
            new ComponentParameter()
            {
                Name = "SubtitleAriaId",
                Type = "string",
                DefaultValue = "",
                Description = "ARIA id for the subtitle of the Modal, if any.",
            },
            new ComponentParameter()
            {
                Name = "TitleAriaId",
                Type = "string",
                DefaultValue = "",
                Description = "ARIA id for the title of the Modal, if any.",
            },
        };

        private readonly string modalSampleCode = $"<BitButton OnClick=@(()=>IsOpen=true)>Open Modal</BitButton>{Environment.NewLine}" +
              $"<BitModal @bind-IsOpen=IsOpen>{Environment.NewLine}" +
              $"<div class='modal-header'>{Environment.NewLine}" +
              $"<span>Lorem Ipsum</span>{Environment.NewLine}" +
              $"<BitIconButton OnClick=@(()=>IsOpen=false) IconName='BitIcon.ChromeClose' Title='Close' />{Environment.NewLine}" +
              $"</div>{Environment.NewLine}" +
              $"<div class='modal-body'>{Environment.NewLine}" +
              $"<p>{Environment.NewLine}" +
              $"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sitamet, vulputate in leo.Maecenas vulputate congue sapien eu tincidunt.Etiam eu sem turpis.Fusce temporsagittis nunc, ut interdum ipsum vestibulum non." +
              $"Proin dolor elit, aliquam eget tincidunt non, vestibulum ut turpis.In hac habitasse platea dictumst.In a odio eget enim porttitor maximus.Aliquam nulla nibh,ullamcorper aliquam placerat eu, viverra et dui.Phasellus ex lectus, maximus in mollis ac, luctus vel eros." +
              $"Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante. Quisque ultricies mi nec leo ultricies mollis.Vivamus egestas volutpat lacinia. Quisque pharetra eleifend fficitur.{ Environment.NewLine}" +
              $"</p>{Environment.NewLine}" +
              $"<p>{Environment.NewLine}" +
              $"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sitamet, vulputate in leo.Maecenas vulputate congue sapien eu tincidunt.Etiam eu sem turpis.Fusce temporsagittis nunc, ut interdum ipsum vestibulum non." +
              $"Proin dolor elit, aliquam eget tincidunt non, vestibulum ut turpis.In hac habitasse platea dictumst.In a odio eget enim porttitor maximus.Aliquam nulla nibh,ullamcorper aliquam placerat eu, viverra et dui.Phasellus ex lectus, maximus in mollis ac, luctus vel eros." +
              $"Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante. Quisque ultricies mi nec leo ultricies mollis.Vivamus egestas volutpat lacinia. Quisque pharetra eleifend fficitur.{ Environment.NewLine}" +
              $"</p>{Environment.NewLine}" +
              $"<p>{Environment.NewLine}" +
              $"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sitamet, vulputate in leo.Maecenas vulputate congue sapien eu tincidunt.Etiam eu sem turpis.Fusce temporsagittis nunc, ut interdum ipsum vestibulum non." +
              $"Proin dolor elit, aliquam eget tincidunt non, vestibulum ut turpis.In hac habitasse platea dictumst.In a odio eget enim porttitor maximus.Aliquam nulla nibh,ullamcorper aliquam placerat eu, viverra et dui.Phasellus ex lectus, maximus in mollis ac, luctus vel eros." +
              $"Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante. Quisque ultricies mi nec leo ultricies mollis.Vivamus egestas volutpat lacinia. Quisque pharetra eleifend fficitur.{ Environment.NewLine}" +
              $"</p>{Environment.NewLine}" +
              $"<p>{Environment.NewLine}" +
              $"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sitamet, vulputate in leo.Maecenas vulputate congue sapien eu tincidunt.Etiam eu sem turpis.Fusce temporsagittis nunc, ut interdum ipsum vestibulum non." +
              $"Proin dolor elit, aliquam eget tincidunt non, vestibulum ut turpis.In hac habitasse platea dictumst.In a odio eget enim porttitor maximus.Aliquam nulla nibh,ullamcorper aliquam placerat eu, viverra et dui.Phasellus ex lectus, maximus in mollis ac, luctus vel eros." +
              $"Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante. Quisque ultricies mi nec leo ultricies mollis.Vivamus egestas volutpat lacinia. Quisque pharetra eleifend fficitur.{ Environment.NewLine}" +
              $"</p>{Environment.NewLine}" +
              $"<p>{Environment.NewLine}" +
              $"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sitamet, vulputate in leo.Maecenas vulputate congue sapien eu tincidunt.Etiam eu sem turpis.Fusce temporsagittis nunc, ut interdum ipsum vestibulum non." +
              $"Proin dolor elit, aliquam eget tincidunt non, vestibulum ut turpis.In hac habitasse platea dictumst.In a odio eget enim porttitor maximus.Aliquam nulla nibh,ullamcorper aliquam placerat eu, viverra et dui.Phasellus ex lectus, maximus in mollis ac, luctus vel eros." +
              $"Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante. Quisque ultricies mi nec leo ultricies mollis.Vivamus egestas volutpat lacinia. Quisque pharetra eleifend fficitur.{ Environment.NewLine}" +
              $"</p>{Environment.NewLine}" +
              $"</div>{Environment.NewLine}" +
              $"</BitModal>{Environment.NewLine}" +
              $"<style>{Environment.NewLine}" +
              $".modal-header {{ {Environment.NewLine}" +
              $"display: flex;{Environment.NewLine}" +
              $"align-items: center;{Environment.NewLine}" +
              $"font-size: 24px;{Environment.NewLine}" +
              $"font-weight: 600;{Environment.NewLine}" +
              $"border-top: 4px solid #5C2D91;{Environment.NewLine}" +
              $"justify-content: space-between;{Environment.NewLine}" +
              $"padding: 12px 12px 14px 24px;{Environment.NewLine}" +
              $"}} {Environment.NewLine}" +
              $".modal-body {{ {Environment.NewLine}" +
              $"overflow-y: hidden;{Environment.NewLine}" +
              $"line-height: 20px;{Environment.NewLine}" +
              $"padding: 0 24px 24px;{Environment.NewLine}" +
              $"}} {Environment.NewLine}" +
              $"</style>{Environment.NewLine}";
    }
}
