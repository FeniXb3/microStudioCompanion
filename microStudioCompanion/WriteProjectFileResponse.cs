using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class WriteProjectFileResponse : ResponseBase
    {
        public int version { get; set; }
        public int size { get; set; }
    }
}
