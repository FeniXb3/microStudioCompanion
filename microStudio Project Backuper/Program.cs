using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace microStudio_Project_Backuper
{
    class Program
    {
        static void Main(string[] args)
        {

            var loginInfoFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "loginInfo.JSON");
            var configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "config.JSON");
            Config config;


            if (!System.IO.File.Exists(configFilePath))
            {
                Console.Write("Your microStudio nick: ");
                var nick = Console.ReadLine();
                Console.Write("Your microStudio password: ");
                var password = Console.ReadLine();
                config = new Config
                {
                    nick = nick,
                    password = password
                };

                System.IO.File.WriteAllText(configFilePath, JsonSerializer.Serialize(config));
                Console.WriteLine($"Password and login saved IN PLAIN TEXT, READABLE BY ANYONE, here: ${configFilePath}");
            }
            else
            {
                config = JsonSerializer.Deserialize<Config>(System.IO.File.ReadAllText(configFilePath));
            }

            using(var socket = new System.Net.WebSockets.ClientWebSocket())
            {
                socket.ConnectAsync(new Uri("wss://microstudio.dev"), CancellationToken.None).Wait();
                string token = null;
 

                if (System.IO.File.Exists(loginInfoFilePath))
                {
                    var content = JsonSerializer.Deserialize<LoginResponse>(System.IO.File.ReadAllText(loginInfoFilePath));
                    token = content.token;

                    var tokenRequest = new TokenRequest
                    {
                        token = token
                    };

                    try
                    {
                        var tokenResponse = SendAndReceive<TokenRequest, TokenResponse>(socket, tokenRequest);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                        token = null;
                    }
                }

                if(token == null)
                {
                    var loginRequest = new LoginRequest
                    {
                        nick = config.nick,
                        password = config.password
                    };

                    var response = SendAndReceive<LoginRequest, LoginResponse>(socket, loginRequest);
                    token = response.token;

                    System.IO.File.WriteAllText(loginInfoFilePath, JsonSerializer.Serialize(response));
                    Console.WriteLine($"Login data saved IN PLAIN TEXT, READABLE BY ANYONE, here: ${loginInfoFilePath}");
                }
                
                Console.WriteLine($"Recieved: {token}");

                var getProjectListRequest = new GetProjectListRequest();
                var getProjectListResponse = SendAndReceive<GetProjectListRequest, GetProjectListResponse>(socket, getProjectListRequest);
                foreach (var element in getProjectListResponse.list)
                {
                    Console.WriteLine($"ID: {element.id}; Titile: {element.title}");
                }
            }
        }

        static ResponseType SendAndReceive<RequestType, ResponseType>(ClientWebSocket socket, RequestType requestData)
            where RequestType : RequestBase
        {
            var requestText = JsonSerializer.Serialize(requestData);

            Console.WriteLine($"Sending {requestData.name} request");
            socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestText)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

            var buffer = new byte[99999999];
            Console.WriteLine($"Receiving {requestData.name} response");
            var result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;
            var resultText = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var response = JsonSerializer.Deserialize<ResponseType>(resultText);

            return response;
        }
    }
}
