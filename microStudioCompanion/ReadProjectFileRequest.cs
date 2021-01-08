using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class ReadProjectFileRequest : RequestBase
    {
        public int project { get; set; }
        public string file { get; set; }

        public ReadProjectFileRequest()
        {
            name = "read_project_file";
        }

        public override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        public override string Message => $"{file} Reading remote file";
    }

}
