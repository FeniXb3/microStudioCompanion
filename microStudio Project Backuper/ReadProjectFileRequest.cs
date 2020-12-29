using System;
using System.Collections.Generic;
using System.Text;

namespace microStudio_Project_Backuper
{
    public class ReadProjectFileRequest : RequestBase
    {
        public int project { get; set; }
        public string file { get; set; }

        public ReadProjectFileRequest()
        {
            name = "read_project_file";
        }
    }

}
