using System;

namespace Bit.OData.Contracts
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ODataModuleAttribute : Attribute
    {
        public ODataModuleAttribute(string odataRouteName)
        {
            if (odataRouteName == null)
                throw new ArgumentNullException(odataRouteName);

            ODataRouteName = odataRouteName;
        }

        public string ODataRouteName { get; }
    }
}