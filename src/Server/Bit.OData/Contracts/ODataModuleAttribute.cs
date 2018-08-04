using System;

namespace Bit.OData.Contracts
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ODataModuleAttribute : Attribute
    {
        public ODataModuleAttribute(string odataRouteName)
        {
            ODataRouteName = odataRouteName ?? throw new ArgumentNullException(odataRouteName);
        }

        public string ODataRouteName { get; }
    }
}
