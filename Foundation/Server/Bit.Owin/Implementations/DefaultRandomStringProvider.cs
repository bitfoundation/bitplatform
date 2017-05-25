using System;
using Bit.Core.Contracts;

namespace Bit.Owin.Implementations
{
    public class DefaultRandomStringProvider : IRandomStringProvider
    {
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public virtual string GetRandomNonSecureString(int length)
        {
            char[] stringChars = new char[length];

            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = Chars[random.Next(Chars.Length)];
            }

            string finalString = new string(stringChars);

            return finalString;
        }
    }
}