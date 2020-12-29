using System;
using System.Collections.Generic;
using System.Text;

namespace microStudio_Project_Backuper
{
    public class WriteProjectFileResponse
    {
        public string name { get; set; }
        public int version { get; set; }
        public int size { get; set; }
        public int request_id { get; set; }
    }
}
