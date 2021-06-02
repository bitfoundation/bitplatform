using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Client.Web.BlazorUI.Utils
{
    public class ElementStyleBuilder : ElementAttributeBuilder
    {
        protected override char Separator { get => ';'; }
    }

    public class ElementClassBuilder : ElementAttributeBuilder
    {
        protected override char Separator { get => ' '; }
    }

    public abstract class ElementAttributeBuilder
    {
        private bool _dirty = true;
        private string _value = string.Empty;
        private List<Func<string?>> _registrars = new();

        protected abstract char Separator { get; }

        public string Value
        {
            get
            {
                if (_dirty)
                {
                    Build();
                }
                return _value;
            }
        }

        public ElementAttributeBuilder Register(Func<string?> registrar)
        {
            _registrars.Add(registrar);
            return this;
        }

        public void Reset()
        {
            _dirty = true;
        }

        private void Build()
        {
            _value = string.Join(Separator, _registrars.Select(g => g()).Where(s => s.HasValue()));
            _dirty = false;
        }
    }
}
