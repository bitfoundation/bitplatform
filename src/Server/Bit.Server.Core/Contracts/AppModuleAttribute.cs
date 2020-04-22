using System;

namespace Bit.Core.Contracts
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class AppModuleAttribute : Attribute
    {
        public AppModuleAttribute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Type = type;
        }

        public Type Type { get; }
    }
}
