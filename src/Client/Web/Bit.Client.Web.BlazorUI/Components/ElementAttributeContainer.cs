using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI
{
    public class ElementStyleContainer : ElementAttributeContainer
    {
        protected override char Separator { get => ';'; }
    }

    public class ElementClassContainer : ElementAttributeContainer
    {
        protected override char Separator { get => ' '; }
    }

    public abstract class ElementAttributeContainer
    {
        protected abstract char Separator { get; }

        private List<string> _attributes = new List<string>();

        public void Clear()
        {
            _attributes.Clear();
        }

        public void Add(string style)
        {
            _attributes.Add(style);
        }

        public bool Remove(string style)
        {
            return _attributes.Remove(style);
        }

        public string Value => string.Join(Separator, _attributes);
    }
}
