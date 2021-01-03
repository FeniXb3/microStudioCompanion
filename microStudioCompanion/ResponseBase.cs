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
        project_list,
        write_project_file,
        delete_project_file,
        project_file_locked,
        project_file_update,
        project_file_deleted,
    }

    public enum ResponseErrors
    {
        unknown_user,
        invalid_token,
        not_connected
    }
}
