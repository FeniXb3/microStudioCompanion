﻿using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class LockProjectFileRequest : RequestBase
    {
        public int project { get; set; }
        public string file { get; set; }
        public LockProjectFileRequest()
        {
            Handled = true;
            name = "lock_project_file";
        }

        public override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        public override string Message => $"{file} Locking remote file";
    }
}
