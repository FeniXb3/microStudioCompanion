using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class CreateProjectRequest : RequestBase
    {
        public string title { get; set; }
        public string slug { get; set; }
        public CreateProjectRequest()
        {
            name = "create_project";
        }
    }

}
