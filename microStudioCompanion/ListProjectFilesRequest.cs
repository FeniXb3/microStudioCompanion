using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class ListProjectFilesRequest : RequestBase
    {
        public int project { get; set; }
        public string folder { get; set; }

        public ListProjectFilesRequest()
        {
            name = "list_project_files";
        }

        public override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        public override string Message => $"  <-  [i] Listing project files from folder {folder}";
    }
}
