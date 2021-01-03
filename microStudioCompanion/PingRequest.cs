using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class PingRequest : RequestBase
    {
        public PingRequest()
        {
            name = "ping";
        }

        public override string Message => "ping";

        public override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }
    }
}
