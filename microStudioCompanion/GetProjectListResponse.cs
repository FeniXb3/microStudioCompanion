using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    public class GetProjectListResponse : ResponseBase
    {
        public Project[] list { get; set; }
    }

    public class Project
    {
        public int id { get; set; }
        public Owner owner { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string code { get; set; }
        public string description { get; set; }
        public string[] tags { get; set; }
        public string[] platforms { get; set; }
        public string[] controls { get; set; }
        public string orientation { get; set; }
        public string aspect { get; set; }
        public string graphics { get; set; }
        public long date_created { get; set; }
        public long last_modified { get; set; }
        public bool _public { get; set; }
        public int size { get; set; }
        public User[] users { get; set; }
    }

    public class Owner
    {
        public int id { get; set; }
        public string nick { get; set; }
    }

    public class User
    {
        public int id { get; set; }
        public string nick { get; set; }
        public bool accepted { get; set; }
    }

}
