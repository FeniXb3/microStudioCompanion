using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class GetProjectListRequest : RequestBase
    {
        public GetProjectListRequest()
        {
            name = "get_project_list";
        }

        public override string Message => " [<-] [i] Getting project list";

        public override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }
    }
}
