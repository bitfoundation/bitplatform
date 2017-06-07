using System;
using System.Linq;
using System.Text.RegularExpressions;
using Bit.Core.Contracts;

namespace Bit.Core.Implementations
{
    public class DefaultIraninCodeValidator : IIraninCodeValidator
    {
        public virtual bool NationalCodeIsValid(string code)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            if (!Regex.IsMatch(code, @"^\d{10}$"))
                return false;

            int check = Convert.ToInt32(code.Substring(9, 1));
            int sum = Enumerable.Range(0, 9)
                .Select(x => Convert.ToInt32(code.Substring(x, 1)) * (10 - x))
                .Sum() % 11;

            return sum < 2 && check == sum || sum >= 2 && check + sum == 11;
        }
    }
}
