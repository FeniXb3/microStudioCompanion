using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    class ListProjectFilesResponse : ResponseBase
    {
        public File[] files { get; set; }
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
