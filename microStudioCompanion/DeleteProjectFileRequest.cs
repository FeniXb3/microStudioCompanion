using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    class DeleteProjectFileRequest : RequestBase
    {
        public int project { get; set; }
        public string file { get; set; }

        public DeleteProjectFileRequest()
        {
            name = "delete_project_file";
        }
    }
}
