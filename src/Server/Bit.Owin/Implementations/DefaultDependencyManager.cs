using Bit.Core.Contracts;

namespace Bit.Owin.Implementations
{
    public static class DefaultDependencyManager
    {
        private static IDependencyManager _current;

        public static IDependencyManager Current
        {
            get => _current ?? (_current = new AutofacDependencyManager());
            set => _current = value;
        }
    }
}
