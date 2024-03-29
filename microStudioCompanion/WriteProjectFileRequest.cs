﻿using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class WriteProjectFileRequest : RequestBase
    { 
        public int project { get; set; }
        public string file { get; set; }
        public string content { get; set; }
        public string thumbnail { get; set; }

        public override string Message => $"{file} Writing remote file";

        public WriteProjectFileRequest()
        {
            name = "write_project_file";
        }

        public override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }
    }

}
