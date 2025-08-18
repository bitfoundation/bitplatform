using Bit.Core.Implementations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Bit.Core.Models
{
    public class BitJwtToken
    {
        public static implicit operator BitJwtToken(Claim[] claims)
        {
            Claim? primary_sid = claims.SingleOrDefault(c => c.Type == "primary_sid");

            return new BitJwtToken
            {
                UserId = primary_sid?.Value ?? claims.SingleOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                Claims = claims
                    .Where(c => c.Type != "primary_sid" && c.Type != "sub")
                    .GroupBy(c => c.Type)
                    .ToDictionary(g => g.Key, g => string.Join(" ", g.Select(c => c.Value)))
            };
        }

        public virtual string UserId { get; set; } = default!;

        public virtual TimeSpan ExpiresIn { get; set; } = TimeSpan.FromDays(1);

        public virtual Dictionary<string, string?> Claims { get; set; } = new Dictionary<string, string?> { };

        public static BitJwtToken FromJson(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            return DefaultJsonContentFormatter.Current.Deserialize<BitJwtToken>(json);
        }

        public static string ToJson(BitJwtToken bitJwtToken)
        {
            if (bitJwtToken == null)
                throw new ArgumentNullException(nameof(bitJwtToken));

            return DefaultJsonContentFormatter.Current.Serialize(bitJwtToken);
        }
    }
}
