using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            var mode = "pull";
            var projectSlug = "";
            if (args.Length > 0)
            {
                mode = args[0];
                if (args.Length > 1)
                {
                    projectSlug = args[1];
                }
            }

            var host = "https://microstudio.dev";
            var loginInfoFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "loginInfo.JSON");
            var configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "config.JSON");
            Config config = GetConfig(configFilePath);

            using (var socket = new System.Net.WebSockets.ClientWebSocket())
            {
                using (var webClient = new WebClient())
                {
                    socket.ConnectAsync(new Uri("wss://microstudio.dev"), CancellationToken.None).Wait();
                    string token = GetToken(loginInfoFilePath, config, socket);

                    Console.WriteLine($"Recieved: {token}");
                    if (mode == "pull")
                    {
                        projectSlug = PullFiles(projectSlug, host, config, socket, webClient);
                    }
                }
            }
        }

        private static string PullFiles(string projectSlug, string host, Config config, ClientWebSocket socket, WebClient webClient)
        {
            var projects = GetProjects(socket);

            if (string.IsNullOrWhiteSpace(projectSlug))
            {
                Console.Write("Project slug to backup: ");
                projectSlug = Console.ReadLine();
            }
            var selectedProject = projects[projectSlug];

            var remoteDirectories = new[] { "ms", "sprites", "maps", "doc" };
            var localDirectoryMapping = new Dictionary<string, string>
                        {
                            { "ms", "code" },
                            { "sprites", "sprites" },
                            { "maps", "maps" },
                            { "doc", "docs" }
                        };

            foreach (var dir in remoteDirectories)
            {
                var listProjectFilesRequest = new ListProjectFilesRequest
                {
                    folder = dir,
                    project = selectedProject.id
                };

                var nick = selectedProject.owner.nick;
                var slug = selectedProject.slug;
                var code = selectedProject.code;
                var name = selectedProject.title;
                var localDirectoryPath = Path.Combine(config.localDirectory, name, localDirectoryMapping[dir]);
                if (Directory.Exists(localDirectoryPath))
                {
                    Directory.Delete(localDirectoryPath, true);
                }

                Directory.CreateDirectory(localDirectoryPath);

                var listProjectFilesResponse = SendAndReceive<ListProjectFilesRequest, ListProjectFilesResponse>(socket, listProjectFilesRequest);
                foreach (var file in listProjectFilesResponse.files)
                {


                    var onlineFilePath = $"{host}/{nick}/{slug}/{code}/{dir}/{file.file}";
                    var localFilePath = Path.Combine(localDirectoryPath, file.file);
                    Console.WriteLine($"Downloading file: {file.file}");
                    webClient.DownloadFile(onlineFilePath, localFilePath);
                }
            }

            return projectSlug;
        }

        private static Dictionary<string, List> GetProjects(ClientWebSocket socket)
        {
            var getProjectListRequest = new GetProjectListRequest();
            var getProjectListResponse = SendAndReceive<GetProjectListRequest, GetProjectListResponse>(socket, getProjectListRequest);
            var projects = new Dictionary<string, List>();
            foreach (var element in getProjectListResponse.list)
            {
                projects[element.slug] = element;
            }

            return projects;
        }

        private static string GetToken(string loginInfoFilePath, Config config, ClientWebSocket socket)
        {
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
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    token = null;
                }
            }

            if (token == null)
            {
                Console.Write("Your microStudio password: ");
                var password = Console.ReadLine();

                var loginRequest = new LoginRequest
                {
                    nick = config.nick,
                    password = password
                };

                var response = SendAndReceive<LoginRequest, LoginResponse>(socket, loginRequest);
                token = response.token;

                System.IO.File.WriteAllText(loginInfoFilePath, JsonSerializer.Serialize(response));
                Console.WriteLine($"Login data saved IN PLAIN TEXT, READABLE BY ANYONE, here: ${loginInfoFilePath}");
            }

            return token;
        }

        private static Config GetConfig(string configFilePath)
        {
            Config config;
            if (!System.IO.File.Exists(configFilePath))
            {
                Console.Write("Your microStudio nick: ");
                var nick = Console.ReadLine();
                Console.Write("Local projects parent directory: ");
                var localDirectory = Console.ReadLine();

                config = new Config
                {
                    nick = nick,
                    localDirectory = localDirectory
                };

                System.IO.File.WriteAllText(configFilePath, JsonSerializer.Serialize(config));
                Console.WriteLine($"Login and projects root dir are saved here: ${configFilePath}");
            }
            else
            {
                config = JsonSerializer.Deserialize<Config>(System.IO.File.ReadAllText(configFilePath));
            }

            return config;
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
