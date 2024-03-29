﻿using System;
using System.IO;
using Websocket.Client;

namespace microStudioCompanion
{
    class TokenHandler
    {
        public static string tokenInfoFilePath = Path.Combine(System.AppContext.BaseDirectory, "tokenInfo.JSON");

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
            Logger.LogLocalQuery("Your microStudio password: ");
            var password = Console.ReadLine();
            new LoginRequest
            {
                nick = config.nick,
                password = password
            }.SendVia(socket);
        }

        public static void SaveToken(string token)
        {
            System.IO.File.WriteAllText(tokenInfoFilePath, token);
            Logger.LogLocalInfo($"Token saved IN PLAIN TEXT, READABLE BY ANYONE, here: {tokenInfoFilePath}");
        }

        private static void GetSavedToken(WebsocketClient socket)
        {
            try
            {
                new TokenRequest
                {
                    token = System.IO.File.ReadAllText(tokenInfoFilePath)
                }.SendVia(socket);
            }
            catch (Exception e)
            {
                Logger.LogLocalError(e.Message);
            }
        }
    }
}
