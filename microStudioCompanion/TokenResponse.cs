using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class TokenResponse : ResponseBase
    {
        public string nick { get; set; }
        public string email { get; set; }
        public Flags flags { get; set; }
        public Info info { get; set; }
        public Settings settings { get; set; }
        public object[] notifications { get; set; }
    }
}
