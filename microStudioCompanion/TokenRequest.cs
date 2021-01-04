using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class TokenRequest : RequestBase
    {
        public string token { get; set; }

        public override string Message => "Checking token";

        public TokenRequest()
        {
            name = "token";
        }

        public override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }
    }
}
