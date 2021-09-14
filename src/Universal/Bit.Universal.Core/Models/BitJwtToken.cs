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

            if (primary_sid is not null)
            {
                try
                {
                    JToken json = JToken.Parse(primary_sid.Value);
                    return new BitJwtToken
                    {
                        UserId = (string)json["UserId"],
                        Claims = json["CustomProps"].OfType<JProperty>().ToDictionary(prop => prop.Name, prop => (string)prop.Value)
                    };
                }
                catch (JsonReaderException)
                {
                    return new BitJwtToken { UserId = primary_sid.Value };
                }
            }

            BitJwtToken bitJwtToken = new BitJwtToken { };

            foreach (var claim in claims)
            {
                if (!bitJwtToken.Claims.ContainsKey(claim.Type))
                    bitJwtToken.Claims.Add(claim.Type, claim.Value);
                else
                    bitJwtToken.Claims[claim.Type] += $" {claim.Value}";
                if (claim.Type == "sub")
                    bitJwtToken.UserId = claim.Value;
            }

            return bitJwtToken;
        }

        public static implicit operator Claim[](BitJwtToken bitJwtToken)
        {
            return bitJwtToken.Claims.Select(c => new Claim(c.Key, c.Value ?? "NULL")).ToArray();
        }

        public virtual string UserId { get; set; } = default!;

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
