using System;
using System.Collections.Generic;
using System.Text;

namespace microStudio_Project_Backuper
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
