using Bit.Core.Implementations;
using System;
using System.Collections.Generic;

namespace Bit.Core.Contracts
{
    public class BitJwtToken
    {
        public virtual string? UserId { get; set; }

        public virtual Dictionary<string, string?> CustomProps { get; set; } = new Dictionary<string, string?> { };

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
