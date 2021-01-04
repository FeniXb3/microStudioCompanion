using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Websocket.Client;

namespace microStudioCompanion
{
    class Program
    {
        static string projectSlug;
        static dynamic selectedProject;
        static Dictionary<string, dynamic> projects = new Dictionary<string, dynamic>();
        static Config config;
        static WebsocketClient socket;
        static int stepNumber = 0;
        static Dictionary<string, List<Action>> modes;
        static bool ChangeStep { get; 
            set; } = false;
        static string currentMode = "";
        static bool finished = false;
        private static bool changingFile = false;
        static string host = "https://microstudio.dev";
        static bool shouldDownloadFiles = true;
        static List<string> subDirectories = new List<string> { "ms", "sprites", "maps", "doc" };
        static Dictionary<string, bool> subDirHandled = new Dictionary<string, bool>();
        private static Dictionary<string, FileStream> lockStreams = new Dictionary<string, FileStream>();
        private static bool isWatching;
        static FileSystemWatcher fileSystemWatcher;

        static void Main(string[] args)
        {

            Console.WriteLine(" ----------------------------------------------------------------------");
            Console.WriteLine("| Welcome to microStudio Companion! Let's backup your game! :)         |");
            Console.WriteLine("| If you want to contact me, write me an email: konrad@makegames.today |");
            Console.WriteLine("| You can donate me at https://fenix.itch.io/microstudio-companion     |");
            Console.WriteLine(" ----------------------------------------------------------------------");
            Console.WriteLine();

            foreach (var item in subDirectories)
            {
                subDirHandled.Add(item, false);
            }

            currentMode = "pull";
            projectSlug = "";
            string filePath = null;
            if (args.Length > 0)
            {
                currentMode = args[0];
                if (args.Length > 1)
                {
                    projectSlug = args[1];
                }

                if (args.Length > 2)
                {
                    filePath = args[2];
                }
            }
            PrepareModesSteps();

            config = Config.Get();


            using (socket = new WebsocketClient(new Uri("wss://microstudio.dev")))
            {
                socket.MessageReceived.Subscribe(data => HandleResponse(data));
                Console.WriteLine("      [i] Starting...");
                socket.Start().Wait();
                Console.WriteLine("      [i] Started!");
                Task.Run(() => StartSendingPing(socket));
                switch (currentMode)
                {
                    case "pull":
                        shouldDownloadFiles = true;
                        break;
                    case "watch":
                        break;
                }

                stepNumber = 0;
                modes[currentMode][stepNumber]();

                while (!finished)
                {
                    if (ChangeStep)
                    {
                        if (stepNumber + 1 < modes[currentMode].Count)
                        {
                            stepNumber++;
                            ChangeStep = false;
                            modes[currentMode][stepNumber]();
                        }
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

        private static void PrepareModesSteps()
        {
            modes = new Dictionary<string, List<Action>>();
            modes.Add("watch", new List<Action>
            {
                () => TokenHandler.GetToken(config, socket),
                () => new GetProjectListRequest().SendVia(socket),
                () => SelectProject(ref projectSlug, projects),
                //() => {
                //    new ListProjectFilesRequest
                //    {
                //        folder = "ms",
                //        project = (int)selectedProject.id
                //    }.SendVia(socket);
                //},
                
                () => {
                    if (!Directory.Exists(config.localDirectory))
                    {
                        Console.WriteLine($" <!> Parent directory for your projects ({config.localDirectory}) does not exist.");
                        config.AskForDirectory();
                        config.Save();
                    }
                    ChangeStep = true;
                },
                () => PullFiles(projectSlug, host, config, socket),
                () =>
                {
                    Task.Run( async () => {
                        await Task.Delay(1000);
                        int count = -1;
                        lock(RequestBase.RequestsSent)
                        {
                            count = RequestBase.RequestsSent.Count(kvp => !kvp.Value.Handled);
                        }
                        while(count > 0)
                        {
                            await Task.Delay(1000);
                            lock(RequestBase.RequestsSent)
                            {
                                count = RequestBase.RequestsSent.Count(kvp => !kvp.Value.Handled);
                            }
                        }
                        ChangeStep = true;
                    });
                },
                () => StartWatching(selectedProject)
            });

            modes.Add("pull", new List<Action>
            {
                () => TokenHandler.GetToken(config, socket),
                () => new GetProjectListRequest().SendVia(socket),
                () => SelectProject(ref projectSlug, projects),
                () => {
                    if (!Directory.Exists(config.localDirectory))
                    {
                        Console.WriteLine($" <!> Parent directory for your projects ({config.localDirectory}) does not exist.");
                        config.AskForDirectory();
                        config.Save();
                    }
                    ChangeStep = true;
                },
                () => PullFiles(projectSlug, host, config, socket),
                () =>
                {
                    Task.Run( async () => {
                        int count = -1;
                        lock(RequestBase.RequestsSent)
                        {
                            count = RequestBase.RequestsSent.Count(kvp => !kvp.Value.Handled);
                        }
                        while(count > 0)
                        {
                            await Task.Delay(1000);
                            lock(RequestBase.RequestsSent)
                            {
                                count = RequestBase.RequestsSent.Count(kvp => !kvp.Value.Handled);
                            }
                        }
                        finished = true;
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Logger.LogLocalInfo($"Project downloaded to: {Path.Combine(config.localDirectory, (string)selectedProject.title)}");
                        Console.ResetColor();
                        Console.WriteLine();
                    });
                }
            });
        }

        private static void ReadFiles(string folder, dynamic files)
        {
            foreach (var fileData in files)
            {
                new ReadProjectFileRequest
                {
                    file = $"{folder}/{fileData.file}",
                    project = (int)selectedProject.id
                }.SendVia(socket);
            }
        }

        private static void HandleResponse(ResponseMessage data)
        {
            var response = JsonConvert.DeserializeObject<dynamic>(data.Text);
            string responseTypeText = response.name;
            if (Enum.TryParse(responseTypeText, out ResponseTypes responseType))
            {
                var requestId = (response.request_id != null) ? (int)response.request_id : -1;
                if (requestId != -1)
                {
                    RequestBase.GetSentRequest<RequestBase>(requestId).Handled = true;
                }

                switch (responseType)
                {
                    case ResponseTypes.error:
                        Console.WriteLine($"  ->  <!> Error occured: {response.error}");
                        HandleError((string)response.error);
                        break;
                    case ResponseTypes.logged_in:
                        TokenHandler.SaveToken((string)response.token);
                        ChangeStep = true;
                        break;
                    case ResponseTypes.token_valid:
                        Logger.LogIncomingInfo("Token valid");
                        ChangeStep = true;
                        break;
                    case ResponseTypes.project_list:
                        Logger.LogIncomingInfo($"Received project list");
                        projects = new Dictionary<string, dynamic>();
                        foreach (var element in response.list)
                        {
                            projects[(string)element.slug] = element;
                        }
                        ChangeStep = true;
                        break;
                    case ResponseTypes.write_project_file:
                        Logger.LogIncomingInfo($"Writing of file {RequestBase.GetSentRequest<WriteProjectFileRequest>(requestId).file} completed");
                        break;
                    case ResponseTypes.delete_project_file:
                        Logger.LogIncomingInfo($"Deleting of file {RequestBase.GetSentRequest<DeleteProjectFileRequest>(requestId).file} completed");
                        break;
                    case ResponseTypes.project_file_locked:
                        Logger.LogIncomingInfo($"File {(string)response.file} locked remotely by {(string)response.user}");
                        LockFile((string)response.file);
                        break;
                    case ResponseTypes.project_file_update:
                        Logger.LogIncomingInfo($"File {(string)response.file} updated remotely");
                        UpdateFile((string)response.file, (string)response.content);
                        break;
                    case ResponseTypes.project_file_deleted:
                        Logger.LogIncomingInfo($"File {(string)response.file} deleted remotely");
                        DeleteFile((string)response.file);
                        break;
                    case ResponseTypes.list_project_files:
                        Logger.LogIncomingInfo($"Received files list for directory {RequestBase.GetSentRequest<ListProjectFilesRequest>(requestId).folder}");
                        if (shouldDownloadFiles)
                        {
                            ReadFiles(RequestBase.GetSentRequest<ListProjectFilesRequest>(requestId).folder, response.files);
                        }
                        subDirHandled[RequestBase.GetSentRequest<ListProjectFilesRequest>(requestId).folder] = true;

                        if (subDirHandled.Count(kvp => !kvp.Value) == 0)
                        {
                            ChangeStep = true;
                        }
                        break;
                    case ResponseTypes.read_project_file:
                        Logger.LogIncomingInfo($"Reading of remote file {RequestBase.GetSentRequest<ReadProjectFileRequest>(requestId).file} completed");
                        UpdateFile((string)RequestBase.GetSentRequest<ReadProjectFileRequest>(requestId).file, (string)response.content);
                        break;
                    case ResponseTypes.pong:
                        break;
                    default:
                        Console.WriteLine($"  ->  <!> Unhandled response type: {responseTypeText}");
                        Console.WriteLine($"  ->  Incomming message: {response}");
                        break;
                }
            }
            else
            {
                Console.WriteLine($"  ->  <!> Unknown response type: {responseTypeText}");
                Console.WriteLine($"  ->  Incomming message: {response}");
            }
        }

        private static void LockFile(string filePath)
        {
            var projectDirectory = (string)selectedProject.title;
            var localFilePath = Path.Combine(config.localDirectory, projectDirectory, filePath);
            if (!lockStreams.ContainsKey(filePath))
            {
                lockStreams.Add(filePath, new FileStream(localFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None));
            }
        }

        private static void UpdateFile(string filePath, string content)
        {
            Logger.LogLocalInfo($"Updating local file {filePath} updated to remote content");
            changingFile = true;

            var projectDirectory = (string)selectedProject.title;
            var localFilePath = Path.Combine(config.localDirectory, projectDirectory, filePath);

            var directoryCreated = false;
            while (!directoryCreated)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));
                    directoryCreated = true;
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    config.AskForDirectory();
                    config.Save();
                    localFilePath = Path.Combine(config.localDirectory, projectDirectory, filePath);
                }
            }

            if (isWatching && !lockStreams.ContainsKey(filePath))
            {
                lockStreams.Add(filePath, new FileStream(localFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None));
            }

            var extension = Path.GetExtension(filePath);
            if(fileSystemWatcher != null)
            {
                fileSystemWatcher.EnableRaisingEvents = false;
                Logger.LogLocalInfo($"Wathing disabled while updating file {filePath}");
            }

            switch (extension)
            {
                case ".png":
                    if (isWatching)
                    {
                        lockStreams[filePath].Write(Convert.FromBase64String(content));
                    }
                    else
                    {
                        System.IO.File.WriteAllBytes(localFilePath, Convert.FromBase64String(content));
                    }
                    break;
                default:
                    if (isWatching)
                    {
                        lockStreams[filePath].Write(System.Text.Encoding.UTF8.GetBytes(content));
                    }
                    else
                    {
                        System.IO.File.WriteAllText(localFilePath, content);
                    }
                    break;
            }
            Logger.LogLocalInfo($"Local file {filePath} updated to remote content");

            changingFile = false;

            if (lockStreams.ContainsKey(filePath))
            {
                lockStreams[filePath].Close();
                lockStreams.Remove(filePath);
            }
            if (fileSystemWatcher != null)
            {
                fileSystemWatcher.EnableRaisingEvents = true;
                Logger.LogLocalInfo($"Wathing enabled after updating file {filePath}");
            }
        }
        private static void DeleteFile(string filePath)
        {
            changingFile = true;

            var projectDirectory = (string)selectedProject.title;
            var localFilePath = Path.Combine(config.localDirectory, projectDirectory, filePath);
            System.IO.File.Delete(localFilePath);

            changingFile = false;
        }

        private static void HandleError(string error)
        {
            if (Enum.TryParse(error.Replace(' ', '_'), out ResponseErrors errorType))
            {
                switch (errorType)
                {
                    case ResponseErrors.unknown_user:
                        Console.WriteLine($"  ->  <!> Login error occured: {error}");
                        config.AskForNick();
                        config.Save();
                        TokenHandler.Login(config, socket);
                        break;
                    case ResponseErrors.invalid_token:
                        Console.WriteLine("  ->  <!> Invalid token");
                        TokenHandler.Login(config, socket);
                        break;
                    case ResponseErrors.not_connected:
                        TokenHandler.Login(config, socket);
                        break;
                    case ResponseErrors.wrong_password:
                        Console.WriteLine("  ->  <!> Wrong password");
                        TokenHandler.Login(config, socket);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Console.WriteLine($" <!> Unknown error type: {error}");
            }
        }

        private static async Task StartSendingPing(WebsocketClient client)
        {
            while (true)
            {
                await Task.Delay(30000);

                if (!client.IsRunning)
                    continue;

                new PingRequest().SendVia(client);
            }
        }

        private static void StartWatching(dynamic selectedProject)
        {
            var localProjectPath = Path.Combine(config.localDirectory, (string)selectedProject.title);
            fileSystemWatcher = new FileSystemWatcher(localProjectPath)
            {
                IncludeSubdirectories = true,
                Filters = { "*.ms", "*.png", "*.json", "*.md" },
                EnableRaisingEvents = true,
                //NotifyFilter = NotifyFilters.FileName
                //    | NotifyFilters.DirectoryName
                //    //| NotifyFilters.Attributes
                //    | NotifyFilters.Size
                //    //| NotifyFilters.LastWrite
                //    //| NotifyFilters.LastAccess
                //    | NotifyFilters.CreationTime
                //    //| NotifyFilters.Security
            };
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Logger.LogLocalInfo($"Watching project {(string)selectedProject.title} directory: {localProjectPath}");
            Console.ResetColor();
            isWatching = true;
        }

        private static void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            var filePath = e.Name.Replace('\\', '/');
            if (lockStreams.ContainsKey(filePath))
            {
                return;
            }

            if (!subDirectories.Contains(Path.GetDirectoryName(e.Name)))
            {
                return;
            }
            DeleteRemoteFile(filePath, selectedProject, config, socket);
        }

        private static void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            var filePath = e.Name.Replace('\\', '/');
            if (lockStreams.ContainsKey(filePath))
            {
                return;
            }

            if (!subDirectories.Contains(Path.GetDirectoryName(e.Name)))
            {
                return;
            }

            PushFile(filePath, selectedProject, config, socket);
        }

        private static void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            var filePath = e.Name.Replace('\\', '/');
            var oldFilePath = e.OldName.Replace('\\', '/');
            if (lockStreams.ContainsKey(filePath))
            {
                return;
            }

            if (!subDirectories.Contains(Path.GetDirectoryName(e.Name)))
            {
                return;
            }
            DeleteRemoteFile(oldFilePath, selectedProject, config, socket);
            PushFile(filePath, selectedProject, config, socket);
        }

        private static void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var filePath = e.Name.Replace('\\', '/');
            if (lockStreams.ContainsKey(filePath))
            {
                return;
            }

            if (!subDirectories.Contains(Path.GetDirectoryName(e.Name)))
            {
                return;
            }

            PushFile(filePath, selectedProject, config, socket);
        }

        private static void DeleteRemoteFile(string filePath, dynamic selectedProject, Config config, WebsocketClient socket)
        {
            new LockProjectFileRequest
            {
                file = filePath,
                project = (int)selectedProject.id
            }.SendVia(socket);

            new DeleteProjectFileRequest
            {
                project = (int)selectedProject.id,
                file = filePath
            }.SendVia(socket);
        }

        private static void PushFile(string filePath, dynamic selectedProject, Config config, WebsocketClient socket)
        {
            var slug = (string)selectedProject.slug;
            var title = (string)selectedProject.title;

            new LockProjectFileRequest
            {
                file = filePath,
                project = (int)selectedProject.id
            }.SendVia(socket);
            
            string content;
            try
            {
                content = ReadFileContent(filePath, selectedProject, config);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($" <!> File {filePath} in project {slug} does not exist");
                return;
            }
            catch
            {
                Thread.Sleep(300);
                content = ReadFileContent(filePath, title, config);
            }
            new WriteProjectFileRequest
            {
                project = selectedProject.id,
                file = filePath,
                content = content
            }.SendVia(socket);
        }

        private static string ReadFileContent(string filePath, string projectDirectory, Config config)
        {
            string content;
            var localFilePath = Path.Combine(config.localDirectory, projectDirectory, filePath);
            var extension = Path.GetExtension(filePath);
            switch (extension)
            {
                case ".png":
                    content = Convert.ToBase64String(System.IO.File.ReadAllBytes(localFilePath));
                    break;
                default:
                    content = System.IO.File.ReadAllText(localFilePath);
                    break;
            }

            return content;
        }

        private static void PullFiles(string projectSlug, string host, Config config, WebsocketClient socket)
        {
            var localDirectoryMapping = new Dictionary<string, string>
                        {
                            { "ms", "ms" },
                            { "sprites", "sprites" },
                            { "maps", "maps" },
                            { "doc", "doc" }
                        };


            foreach (var dir in subDirectories)
            {
                var name = (string)selectedProject.title;
                var localDirectoryPath = Path.Combine(config.localDirectory, name, localDirectoryMapping[dir]);
                if (Directory.Exists(localDirectoryPath))
                {
                    Directory.Delete(localDirectoryPath, true);
                }
                new ListProjectFilesRequest
                {
                    folder = dir,
                    project = (int)selectedProject.id
                }.SendVia(socket);
            }
        }

        private static void SelectProject(ref string projectSlug, Dictionary<string, dynamic> projects)
        {
            while (true)
            {
                if (string.IsNullOrWhiteSpace(projectSlug))
                {
                    Console.Write("      (?) Project slug to backup (leave empty to see available projects): ");
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
            ChangeStep = true;
        }
    }
}
