
namespace microStudioCompanion
{
    public class LoginRequest : RequestBase
    {
        public string nick { get; set; }
        public string password { get; set; }

        public LoginRequest()
        {
            name = "login";
        }

    }
}