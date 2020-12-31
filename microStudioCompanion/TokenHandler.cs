using microStudioCompanion.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace microStudioCompanion
{
    class TokenHandler
    {
        public static string tokenInfoFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "tokenInfo.JSON");

        public static string GetToken(Config config, ClientWebSocket socket)
        {
            string token = null;
            if (System.IO.File.Exists(tokenInfoFilePath))
            {
                token = GetSavedToken(socket);
            }

            if (token == null)
            {
                token = Login(config, socket, token);
            }

            return token;
        }

        private static string Login(Config config, ClientWebSocket socket, string token)
        {
            LoginResponse response = null;
            while (token == null)
            {
                Console.Write(" (?) Your microStudio password: ");
                var password = Console.ReadLine();

                var loginRequest = new LoginRequest
                {
                    nick = config.nick,
                    password = password
                };

                response = socket.SendAndReceive<LoginRequest, LoginResponse>(loginRequest);

                if (response.name == "error")
                {
                    Console.WriteLine($" <!> An error occured: {response.error}");
                    if (response.error == ResponseErrors.unknown_user)
                    {
                        config.AskForNick();
                        config.Save();
                    }
                }

                token = response.token;
            }

            System.IO.File.WriteAllText(tokenInfoFilePath, JsonSerializer.Serialize(response));
            Console.WriteLine($" [i] Token data saved IN PLAIN TEXT, READABLE BY ANYONE, here: {tokenInfoFilePath}");
            return token;
        }

        private static string GetSavedToken(ClientWebSocket socket)
        {
            string token;
            try
            {
                var content = JsonSerializer.Deserialize<LoginResponse>(System.IO.File.ReadAllText(tokenInfoFilePath));
                token = content.token;

                var tokenRequest = new TokenRequest
                {
                    token = token
                };
                var tokenResponse = socket.SendAndReceive<TokenRequest, TokenResponse>(tokenRequest);
                if (tokenResponse.name == "error")
                {
                    Console.WriteLine($" <!> An error occured: {tokenResponse.error}");
                    token = null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                token = null;
            }

            return token;
        }
    }
}
