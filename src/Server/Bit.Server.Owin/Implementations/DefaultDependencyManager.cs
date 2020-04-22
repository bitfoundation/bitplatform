using Bit.Core.Contracts;

namespace Bit.Owin.Implementations
{
    public class DefaultDependencyManager
    {
        private static IDependencyManager? _current;

        public static IDependencyManager Current
        {
            get
            {
                if (_current == null)
                    _current = new AutofacDependencyManager();

                return _current;
            }
            set => _current = value;
        }
    }
}