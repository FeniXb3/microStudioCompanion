
namespace microStudio_Project_Backuper
{
    public class LoginResponse
    {
        public string name { get; set; }
        public string token { get; set; }
        public string nick { get; set; }
        public string email { get; set; }
        public Flags flags { get; set; }
        public Info info { get; set; }
        public Settings settings { get; set; }
        public object[] notifications { get; set; }
        public int request_id { get; set; }
    }

    public class Flags
    {
        public bool newsletter { get; set; }
        public bool validated { get; set; }
    }

    public class Info
    {
        public int size { get; set; }
        public bool early_access { get; set; }
        public int max_storage { get; set; }
    }

    public class Settings
    {
        public Tutorial_Progress tutorial_progress { get; set; }
        public Project_Tutorial project_tutorial { get; set; }
    }

    public class Tutorial_Progress
    {
        public int httpsmicrostudiodevtutorialsdrawing7transparencytransformstutodocdocmd { get; set; }
        public int httpsmicrostudiodevtutorialscreateagame3jumptutodocdocmd { get; set; }
        public int httpsmicrostudiodevtutorialscreateagame2walltutodocdocmd { get; set; }
        public int httpsmicrostudiodevtutorialsprogramming1introductiontutodocdocmd { get; set; }
        public int httpsmicrostudiodevtutorialsprogramming2variablestutodocdocmd { get; set; }
        public int httpsmicrostudiodevtutorialsprogramming3functionstutodocdocmd { get; set; }
        public int httpsmicrostudiodevtutorialstour3firstprojecttutodocdocmd { get; set; }
        public int httpsmicrostudiodevtutorialsprogramming7objectstutodocdocmd { get; set; }
    }

    public class Project_Tutorial
    {
        public string tutorialdrawing { get; set; }
        public string tutorialcreateagame { get; set; }
        public string programming { get; set; }
        public string tutorialfirstproject { get; set; }
    }
}