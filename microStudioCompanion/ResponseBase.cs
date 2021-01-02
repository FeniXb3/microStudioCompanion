using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public abstract class ResponseBase
    {
        public string name { get; set; }
        public int request_id { get; set; }
        public string error { get; set; }
    }

    public enum ResponseTypes
    {
        error,
        pong,
        get_language,
        logged_in,
        token_valid,
        project_list
    }

    public enum ResponseErrors
    {
        unknown_user,
        invalid_token,
        not_connected
    }
}
