using System;
using System.Linq;

namespace IdentityServer3.Core.Models
{
    public static class SignInMessageExtensions
    {
        public static string GetValueFromAcr(this SignInMessage signInMessage, string key)
        {
            if (signInMessage == null)
                throw new ArgumentNullException(nameof(signInMessage));

            key += ":";

            string result = signInMessage.AcrValues.ExtendedSingle($"Finding {key} value in AcrValues", acrValue => acrValue.StartsWith(key, StringComparison.InvariantCulture))
                    .Replace(key, string.Empty, StringComparison.InvariantCulture);

            result = Uri.UnescapeDataString(result);

            return result;
        }

        public static bool TryGetValueFromAcr(this SignInMessage signInMessage, string key, out string? value)
        {
            if (signInMessage == null)
                throw new ArgumentNullException(nameof(signInMessage));

            if (signInMessage.AcrValues.Any(acr => acr.StartsWith(key, StringComparison.InvariantCulture)))
            {
                value = GetValueFromAcr(signInMessage, key);
                return true;
            }

            value = default;

            return false;
        }
    }
}
