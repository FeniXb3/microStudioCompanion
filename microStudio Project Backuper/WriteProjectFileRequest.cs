using System;
using System.Collections.Generic;
using System.Text;

namespace microStudio_Project_Backuper
{
    public class WriteProjectFileRequest : RequestBase
    { 
        public int project { get; set; }
        public string file { get; set; }
        public string content { get; set; }
        
        public WriteProjectFileRequest()
        {
            name = "write_project_file";
        }
    }

}
