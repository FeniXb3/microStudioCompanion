namespace microStudioCompanion
{
    public class LoginRequest : RequestBase
    {
        public string nick { get; set; }
        public string password { get; set; }

        public override string Message => $" [<-] [i] Attempting to log in as {nick}";

        public LoginRequest()
        {
            name = "login";
        }

        public override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }
    }
}