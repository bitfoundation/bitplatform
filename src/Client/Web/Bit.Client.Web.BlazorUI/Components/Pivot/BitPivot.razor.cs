using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitPivot
    {
        protected override string RootElementClass => "bit-pvt";

        private string? selectedKey;
        private OverflowBehavior overflowBehavior = OverflowBehavior.None;
        private LinkFormat linkFormat = LinkFormat.Links;
        private LinkSize linkSize = LinkSize.Normal;
        private bool hasSetSelectedKey;

        [Parameter]
        public string DefaultSelectedKey { get; set; } = "0";

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public OverflowBehavior OverflowBehavior
        {
            get => overflowBehavior;
            set
            {
                overflowBehavior = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public LinkFormat LinkFormat
        {
            get => linkFormat;
            set
            {
                linkFormat = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public LinkSize LinkSize
        {
            get => linkSize;
            set
            {
                linkSize = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public bool HeadersOnly { get; set; } = false;

        [Parameter]
        public EventCallback<BitPivotItem> OnLinkClick { get; set; }

        [Parameter]
        public string SelectedKey
        {
            get => selectedKey;
            set
            {
                if (value == selectedKey) return;
                selectedKey = value;
                _ = SelectedKeyChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<string> SelectedKeyChanged { get; set; }

        internal IDictionary<string, BitPivotItem> Items = new Dictionary<string, BitPivotItem>();

        protected override void OnInitialized()
        {
            selectedKey = selectedKey ?? DefaultSelectedKey;
            base.OnInitialized();
        }

        protected override Task OnParametersSetAsync()
        {
            return base.OnParametersSetAsync();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;

            foreach (var parameter in parametersDictionary!)
            {
                switch (parameter.Key)
                {
                    case nameof(DefaultSelectedKey):
                        DefaultSelectedKey = (string)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(ChildContent):
                        ChildContent = (RenderFragment)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(OverflowBehavior):
                        OverflowBehavior = (OverflowBehavior)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(LinkFormat):
                        LinkFormat = (LinkFormat)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(LinkSize):
                        LinkSize = (LinkSize)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(HeadersOnly):
                        HeadersOnly = (bool)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(OnLinkClick):
                        OnLinkClick = (EventCallback<BitPivotItem>)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(SelectedKey):
                        SelectedKey = (string)parameter.Value;
                        hasSetSelectedKey = true;
                        parametersDictionary.Remove(parameter.Key);
                        break;

                    case nameof(SelectedKeyChanged):
                        SelectedKeyChanged = (EventCallback<string>)parameter.Value;
                        parametersDictionary.Remove(parameter.Key);
                        break;
                }
            }
            return base.SetParametersAsync(ParameterView.FromDictionary(parametersDictionary));
        }

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => LinkSize == LinkSize.Large ? $"{RootElementClass}-large-{VisualClassRegistrar()}"
                                      : LinkSize == LinkSize.Normal ? $"{RootElementClass}-normal-{VisualClassRegistrar()}"
                                      : string.Empty);

            ClassBuilder.Register(() => LinkFormat == LinkFormat.Links ? $"{RootElementClass}-links-{VisualClassRegistrar()}"
                                      : LinkFormat == LinkFormat.Tabs ? $"{RootElementClass}-tabs-{VisualClassRegistrar()}"
                                      : string.Empty);

            ClassBuilder.Register(() => OverflowBehavior == OverflowBehavior.Menu ? $"{RootElementClass}-menu-{VisualClassRegistrar()}"
                                      : OverflowBehavior == OverflowBehavior.Scroll ? $"{RootElementClass}-scroll-{VisualClassRegistrar()}"
                                      : OverflowBehavior == OverflowBehavior.None ? $"{RootElementClass}-none-{VisualClassRegistrar()}"
                                      : string.Empty);
        }

        internal async Task ItemClicked(KeyValuePair<string, BitPivotItem> item)
        {
            if (item.Value.IsEnabled is false) return;

            await OnLinkClick.InvokeAsync(item.Value);

            if (hasSetSelectedKey && SelectedKeyChanged.HasDelegate is false) return;
            SelectedKey = item.Key;
        }

        internal void RegisterOption(BitPivotItem item)
        {
            if (IsEnabled is false)
            {
                item.IsEnabled = false;
            }

            if (item.ItemKey is null)
            {
                item.ItemKey = GenerateUniqueKey();
            }

            Items.Add(item.ItemKey, item);
            StateHasChanged();
        }

        internal void UnregisterOption(BitPivotItem item)
        {
            Items.Remove(item.ItemKey);
        }

        private string GenerateUniqueKey()
        {
            var key = 0;

            while (Items.Keys.Contains(key.ToString()))
            {
                key++;
            }
            return key.ToString();
        }

        private string GetItemClass(BitPivotItem item)
        {
            return Items[SelectedKey] == item ? "selected-item" : string.Empty;
        }

        private string GetItemStyle(BitPivotItem item)
        {
            return item.Visibility == ComponentVisibility.Collapsed ? "display:none" : item.Visibility == ComponentVisibility.Hidden ? "visibility:hidden" : string.Empty;
        }

        internal void NotifyStateChanged() => StateHasChanged();
    }
}
