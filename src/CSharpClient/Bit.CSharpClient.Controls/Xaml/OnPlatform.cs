using System;

namespace Bit.View
{
    public class OnPlatform<T>
    {
        public T Value { get; set; }

        public static implicit operator T(OnPlatform<T> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return value.Value;
        }
    }
}
