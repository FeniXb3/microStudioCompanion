using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public abstract class ResponseBase
    {
        public string name { get; set; }
        public int request_id { get; set; }
        public string error { get; set; }
    }

    public class ResponseErrors
    {
        public static string unknown_user = "unknown user";
    }
}
