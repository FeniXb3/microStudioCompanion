using System;
using System.Collections.Generic;
using System.Text;

namespace microStudio_Project_Backuper
{
    public class ListProjectFilesRequest : RequestBase
    {
        public int project { get; set; }
        public string folder { get; set; }

        public ListProjectFilesRequest()
        {
            name = "list_project_files";
        }
    }

}
