using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace microStudioCompanion
{
    public abstract class RequestBase
    {
        static int lastRequestId = 0;
        public int request_id { get; set; }
        public string name { get; set; }

        public RequestBase()
        {
            request_id = lastRequestId++;
        }

        public abstract string Serialize();
    }
}
