namespace microStudioCompanion
{
    class ListenToProjectRequest : RequestBase
    {
        public string user { get; set; }
        public string project { get; set; }

        public ListenToProjectRequest()
        {
            name = "listen_to_project";
        }
        public override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        public override string Message => $"Listen to project {project}";
    }
}
