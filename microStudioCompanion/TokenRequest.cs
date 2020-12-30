using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class TokenRequest : RequestBase
    {
        public string token { get; set; }

        public TokenRequest()
        {
            name = "token";
        }
    }
}
