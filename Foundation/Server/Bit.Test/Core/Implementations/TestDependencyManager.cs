using Foundation.Api.Implementations;

namespace Foundation.Test.Core.Implementations
{
    public class TestDependencyManager
    {
        public static AutofacTestDependencyManager CurrentTestDependencyManager
        {
            get { return (AutofacTestDependencyManager)DefaultDependencyManager.Current; }
            set { DefaultDependencyManager.Current = value; }
        }
    }
}
