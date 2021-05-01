using System;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Utils;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public abstract class BitComponentBase : ComponentBase
    {
        private string style;
        private Visual visual;
        private string @class;
        private bool isEnabled = true;
        private ComponentVisibility visibility;

        protected bool Rendered { get; private set; } = false;

        private Guid _uniqueId = Guid.NewGuid();

        public Guid UniqueId => _uniqueId;

        public ElementReference RootElement { get; internal set; }

        [CascadingParameter] public Theme Theme { get; set; }

        [CascadingParameter]
        public Visual Visual
        {
            get => visual;
            set
            {
                visual = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public string Class
        {
            get => @class;
            set
            {
                @class = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public string Style
        {
            get => style;
            set
            {
                style = value;
                StyleBuilder.Reset();
            }
        }

        [Parameter]
        public ComponentVisibility Visibility
        {
            get => visibility;
            set
            {
                visibility = value;
                StyleBuilder.Reset();
            }
        }

        protected override void OnInitialized()
        {
            RegisterComponentStyles();
            StyleBuilder
                .Register(() => Style)
                .Register(() => Visibility == ComponentVisibility.Hidden ? "visibility:hidden" :
                                Visibility == ComponentVisibility.Collapsed ? "display:none" :
                                string.Empty);

            ClassBuilder
                .Register(() => RootElementClass)
                .Register(() => $"{RootElementClass}-{VisualClassRegistrar()}")
                .Register(() => $"{RootElementClass}-{(IsEnabled ? "enabled" : "disabled")}-{VisualClassRegistrar()}");
            RegisterComponentClasses();
            ClassBuilder.Register(() => Class);

            base.OnInitialized();
        }

        protected virtual string VisualClassRegistrar()
        {
            return Visual == Visual.Cupertino ? "cupertino" : Visual == Visual.Material ? "material" : "fluent";
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Visual):
                        Visual = (Visual)parameter.Value;
                        break;

                    case nameof(Theme):
                        Theme = (Theme)parameter.Value;
                        break;

                    case nameof(IsEnabled):
                        IsEnabled = (bool)parameter.Value;
                        break;

                    case nameof(Style):
                        Style = (string)parameter.Value;
                        break;

                    case nameof(Class):
                        Class = (string)parameter.Value;
                        break;

                    case nameof(Visibility):
                        Visibility = (ComponentVisibility)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(ParameterView.Empty);
        }

        protected override void OnAfterRender(bool firstRender)
        {
            Rendered = true;
            base.OnAfterRender(firstRender);
        }

        protected abstract string RootElementClass { get; }

        protected ElementClassBuilder ClassBuilder { get; private set; } = new ElementClassBuilder();

        protected ElementStyleBuilder StyleBuilder { get; private set; } = new ElementStyleBuilder();

        protected virtual void RegisterComponentStyles()
        {
        }

        protected virtual void RegisterComponentClasses()
        {
        }
    }
}
