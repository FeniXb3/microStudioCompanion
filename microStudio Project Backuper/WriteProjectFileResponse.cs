using System;
using System.Collections.Generic;
using System.Text;

namespace microStudio_Project_Backuper
{
    public class WriteProjectFileResponse : ResponseBase
    {
        public int version { get; set; }
        public int size { get; set; }
    }
}
