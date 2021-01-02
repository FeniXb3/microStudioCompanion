using microStudioCompanion.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Websocket.Client;

namespace microStudioCompanion
{
    class TokenHandler
    {
        public static string tokenInfoFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "tokenInfo.JSON");

        public static void GetToken(Config config, WebsocketClient socket)
        {
            if (System.IO.File.Exists(tokenInfoFilePath))
            {
                GetSavedToken(socket);
            }
            else
            {
                Login(config, socket);
            }
        }

        public static void Login(Config config, WebsocketClient socket)
        {
            //while (token == null)
            {
                Console.Write(" (?) Your microStudio password: ");
                var password = Console.ReadLine();

                var loginRequest = new LoginRequest
                {
                    nick = config.nick,
                    password = password
                };
                socket.Send(loginRequest.Serialize());

                //response = socket.SendAndReceive<LoginRequest, LoginResponse>(loginRequest);

                //if (response.name == "error")
                //{
                //    Console.WriteLine($" <!> An error occured: {response.error}");
                //    if (response.error == ResponseErrors.unknown_user)
                //    {
                //        config.AskForNick();
                //        config.Save();
                //    }
                //}

                //token = response.token;
            }
        }

        public static void SaveToken(string token)
        {
            System.IO.File.WriteAllText(tokenInfoFilePath, token);
            Console.WriteLine($" [i] Token saved IN PLAIN TEXT, READABLE BY ANYONE, here: {tokenInfoFilePath}");
        }

        private static void GetSavedToken(WebsocketClient socket)
        {
            try
            {
                var tokenRequest = new TokenRequest
                {
                    token = System.IO.File.ReadAllText(tokenInfoFilePath)
                };

                socket.Send(tokenRequest.Serialize());
                //var tokenResponse = socket.SendAndReceive<TokenRequest, TokenResponse>(tokenRequest);
                //if (tokenResponse.name == "error")
                //{
                //    Console.WriteLine($" <!> An error occured: {tokenResponse.error}");
                //    token = null;
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
