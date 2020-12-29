using System;
using System.Collections.Generic;
using System.Text;

namespace microStudio_Project_Backuper
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
    }
}
