using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace microStudioCompanion
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(" ----------------------------------------------------------------------");
            Console.WriteLine("| Welcome to microStudio Companion! Let's backup your game! :)         |");
            Console.WriteLine("| If you want to contact me, write me an email: konrad@makegames.today |");
            Console.WriteLine("| You can donate me at https://fenix.itch.io/microstudio-companion     |");
            Console.WriteLine(" ----------------------------------------------------------------------");
            Console.WriteLine();

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
            var tokenInfoFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "tokenInfo.JSON");
            Config config = Config.Get();

            using (var socket = new ClientWebSocket())
            {
                using (var webClient = new WebClient())
                {
                    socket.ConnectAsync(new Uri("wss://microstudio.dev"), CancellationToken.None).Wait();
                    string token = GetToken(tokenInfoFilePath, config, socket);

                    //Console.WriteLine("Token is valid.");
                    if (mode == "pull")
                    {
                        if (!Directory.Exists(config.localDirectory))
                        {
                            Console.WriteLine($" <!> Parent directory for your projects ({config.localDirectory}) does not exist.");
                            config.AskForDirectory();
                            config.Save();
                        }
                        PullFiles(projectSlug, host, config, socket, webClient);
                    }
                }
            }
            Console.WriteLine();
            Console.WriteLine(" ----------------------------------------------------------------------");
            Console.WriteLine("| Done! Have a great day and Make Games Today!                         |");
            Console.WriteLine("| If you want to contact me, write me an email: konrad@makegames.today |");
            Console.WriteLine("| You can donate me at https://fenix.itch.io/microstudio-companion     |");
            Console.WriteLine(" ----------------------------------------------------------------------");
            Console.WriteLine();
            if (args.Length == 0)
            {
                Console.WriteLine("Press enter to close the app.");
                Console.ReadLine();
            }
        }

        private static void PullFiles(string projectSlug, string host, Config config, ClientWebSocket socket, WebClient webClient)
        {
            var projects = GetProjects(socket);
            Project selectedProject = SelectProject(ref projectSlug, projects);

            var remoteDirectories = new[] { "ms", "sprites", "maps", "doc" };
            var localDirectoryMapping = new Dictionary<string, string>
                        {
                            { "ms", "ms" },
                            { "sprites", "sprites" },
                            { "maps", "maps" },
                            { "doc", "doc" }
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



                var listProjectFilesResponse = SendAndReceive<ListProjectFilesRequest, ListProjectFilesResponse>(socket, listProjectFilesRequest);
                if (listProjectFilesResponse.files.Length == 0)
                {
                    continue;
                }

                var directoryCreated = false;
                while (!directoryCreated)
                {
                    try
                    {
                        Directory.CreateDirectory(localDirectoryPath);
                        directoryCreated = true;
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Console.WriteLine(e.Message);
                        config.AskForDirectory();
                        config.Save();
                        localDirectoryPath = Path.Combine(config.localDirectory, name, localDirectoryMapping[dir]);
                    }
                }

                int index = 0;
                int amount = listProjectFilesResponse.files.Length;
                foreach (var file in listProjectFilesResponse.files)
                {
                    var onlineFilePath = $"{host}/{nick}/{slug}/{code}/{dir}/{file.file}";
                    var localFilePath = Path.Combine(localDirectoryPath, file.file);
                    Console.WriteLine($" [i] Downloading ({++index}/{amount}) file from \"{dir}\" directory: {file.file}");
                    webClient.DownloadFile(onlineFilePath, localFilePath);
                }
            }
            Console.WriteLine($" [i] Project downloaded to: {Path.Combine(config.localDirectory, selectedProject.title)}");
        }

        private static Project SelectProject(ref string projectSlug, Dictionary<string, Project> projects)
        {
            Project selectedProject;

            while (true)
            {
                if (string.IsNullOrWhiteSpace(projectSlug))
                {
                    Console.Write(" (?) Project slug to backup (leave empty to see available projects): ");
                    projectSlug = Console.ReadLine();
                }

                if (projects.ContainsKey(projectSlug))
                {
                    selectedProject = projects[projectSlug];
                    break;
                }
                else
                {
                    Console.WriteLine($" <!> Pick project slug from the list: (Press any key to continue)");
                    Console.ReadKey(true);
                    foreach (var proj in projects.Values)
                    {
                        Console.WriteLine($"- Slug: {proj.slug}  Title: {proj.title}");
                    }
                    projectSlug = null;
                }
            }

            return selectedProject;
        }

        private static Dictionary<string, Project> GetProjects(ClientWebSocket socket)
        {
            var getProjectListRequest = new GetProjectListRequest();
            var getProjectListResponse = SendAndReceive<GetProjectListRequest, GetProjectListResponse>(socket, getProjectListRequest);
            var projects = new Dictionary<string, Project>();
            foreach (var element in getProjectListResponse.list)
            {
                projects[element.slug] = element;
            }

            return projects;
        }

        private static string GetToken(string tokenInfoFilePath, Config config, ClientWebSocket socket)
        {
            string token = null;
            if (System.IO.File.Exists(tokenInfoFilePath))
            {
                token = GetSavedToken(tokenInfoFilePath, socket);
            }

            if (token == null)
            {
                token = Login(tokenInfoFilePath, config, socket, token);
            }

            return token;
        }

        private static string Login(string tokenInfoFilePath, Config config, ClientWebSocket socket, string token)
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

                response = SendAndReceive<LoginRequest, LoginResponse>(socket, loginRequest);

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

        private static string GetSavedToken(string loginInfoFilePath, ClientWebSocket socket)
        {
            string token;
            try
            {
                var content = JsonSerializer.Deserialize<LoginResponse>(System.IO.File.ReadAllText(loginInfoFilePath));
                token = content.token;

                var tokenRequest = new TokenRequest
                {
                    token = token
                };
                var tokenResponse = SendAndReceive<TokenRequest, TokenResponse>(socket, tokenRequest);
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

        static ResponseType SendAndReceive<RequestType, ResponseType>(ClientWebSocket socket, RequestType requestData)
            where RequestType : RequestBase
            where ResponseType : ResponseBase
        {
            var requestText = JsonSerializer.Serialize(requestData);

            //Console.WriteLine($"Sending {requestData.name} request");
            socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestText)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

            var buffer = new byte[99999999];
            //Console.WriteLine($"Receiving {requestData.name} response");

            var result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;
            var resultText = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var response = JsonSerializer.Deserialize<ResponseType>(resultText);

            return response;
        }
    }
}
