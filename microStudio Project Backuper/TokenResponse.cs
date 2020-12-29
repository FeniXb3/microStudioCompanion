using System;
using System.Collections.Generic;
using System.Text;

namespace microStudio_Project_Backuper
{
    public class TokenResponse
    {
        public string name { get; set; }
        public string nick { get; set; }
        public string email { get; set; }
        public Flags flags { get; set; }
        public Info info { get; set; }
        public Settings settings { get; set; }
        public object[] notifications { get; set; }
        public int request_id { get; set; }
    }
}
