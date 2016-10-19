using System;
using Foundation.Core.Contracts;
using Humanizer;

namespace Foundation.Api.Implementations
{
    public class DefaultNameService : INameService
    {
        public virtual string Pluralize(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return name.Pluralize();
        }
    }
}
