using System;
using System.Collections.Generic;
using System.Text;

namespace microStudio_Project_Backuper
{
    class ListProjectFilesResponse
    {
        public string name { get; set; }
        public File[] files { get; set; }
        public int request_id { get; set; }
    }

    public class File
    {
        public string file { get; set; }
        public int version { get; set; }
        public int size { get; set; }
        public Properties properties { get; set; }
    }

    public class Properties
    {
    }
}
