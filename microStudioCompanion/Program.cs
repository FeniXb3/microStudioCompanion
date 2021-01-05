using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
        static bool shouldDownloadFiles = true;
        static List<string> subDirectories = new List<string> { "ms", "sprites", "maps", "doc" };
        static Dictionary<string, bool> subDirHandled = new Dictionary<string, bool>();
        private static Dictionary<string, FileStream> lockStreams = new Dictionary<string, FileStream>();
        private static bool isWatching;
        static FileSystemWatcher fileSystemWatcher;
        static Dictionary<string, (string content, bool fromRemote)> changeHistory = new Dictionary<string, (string content, bool fromRemote)>();

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
                Logger.LogLocalInfo("Starting...");
                socket.Start().Wait();
                Logger.LogLocalInfo("Started!");
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
            List<Action> commonSteps = GetCommonSteps();

            var watchSteps = commonSteps.ToList();
            watchSteps.Add(() => StartWatching(selectedProject));
            modes.Add("watch", watchSteps);

            var pullSteps = commonSteps.ToList();
            pullSteps.Add(() => FinishPulling());
            modes.Add("pull", pullSteps);
        }

        private static void FinishPulling()
        {
            finished = true;
            var message = $"Project downloaded to: {Path.Combine(config.localDirectory, (string)selectedProject.title)}";
            Logger.LogLocalInfo(message, ConsoleColor.Black, ConsoleColor.DarkGreen);
            Console.WriteLine();
        }

        private static List<Action> GetCommonSteps()
        {
            return new List<Action>
            {
                () => TokenHandler.GetToken(config, socket),
                () => new GetProjectListRequest().SendVia(socket),
                () => SelectProject(ref projectSlug, projects),
                () => {
                    if (!Directory.Exists(config.localDirectory))
                    {
                        Logger.LogLocalError($"Parent directory for your projects ({config.localDirectory}) does not exist.");
                        config.AskForDirectory();
                        config.Save();
                    }
                    ChangeStep = true;
                },
                () => PullFiles(config, socket),
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

            };
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
                string tmp = response.name;
                int requestId = response.request_id ?? -1;
                if (requestId != -1)
                {
                    RequestBase.GetSentRequest<RequestBase>(requestId).Handled = true;
                }

                switch (responseType)
                {
                    case ResponseTypes.error:
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
                        Logger.LogLocalError($"Unhandled response type: {responseTypeText}");
                        Logger.LogIncomingInfo($"Incomming message: {response}");
                        break;
                }
            }
            else
            {
                Logger.LogLocalError($"Unknown response type: {responseTypeText}");
                Logger.LogIncomingInfo($"Incomming message: {response}");
            }
        }

        private static void LockFile(string filePath)
        {
            var projectDirectory = (string)selectedProject.title;
            var localFilePath = Path.Combine(config.localDirectory, projectDirectory, filePath);
            if (!lockStreams.ContainsKey(filePath) && System.IO.File.Exists(localFilePath))
            {
                lockStreams.Add(filePath, new FileStream(localFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None));
            }
        }

        private static void UpdateFile(string filePath, string content)
        {
            Logger.LogLocalInfo($"Updating local file {filePath} updated to remote content");

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
                    Logger.LogLocalError(e.Message);
                    config.AskForDirectory();
                    config.Save();
                    localFilePath = Path.Combine(config.localDirectory, projectDirectory, filePath);
                }
            }

            if (isWatching && !lockStreams.ContainsKey(filePath) && System.IO.File.Exists(localFilePath))
            {
                lockStreams.Add(filePath, new FileStream(localFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None));
            }

            changeHistory[filePath] = (content, true);
            var extension = Path.GetExtension(filePath);
            switch (extension)
            {
                case ".png":
                    if (isWatching && lockStreams.ContainsKey(filePath))
                    {
                        lockStreams[filePath].Write(Convert.FromBase64String(content));
                    }
                    else
                    {
                        System.IO.File.WriteAllBytes(localFilePath, Convert.FromBase64String(content));
                    }
                    break;
                default:
                    if (isWatching && lockStreams.ContainsKey(filePath))
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

            if (lockStreams.ContainsKey(filePath))
            {
                lockStreams[filePath].Close();
                lockStreams.Remove(filePath);
                Logger.LogLocalInfo($"Unlocked local file {filePath}");
            }
        }
        private static void DeleteFile(string filePath)
        {
            var projectDirectory = (string)selectedProject.title;
            var localFilePath = Path.Combine(config.localDirectory, projectDirectory, filePath);
            if(lockStreams.ContainsKey(filePath))
            {
                lockStreams[filePath].Close();
                lockStreams.Remove(filePath);
                Logger.LogLocalInfo($"Unlocked local file {filePath}");
            }

            System.IO.File.Delete(localFilePath);
            Logger.LogLocalInfo($"Removed local file {filePath}");
        }

        private static void HandleError(string error)
        {
            if (Enum.TryParse(error.Replace(' ', '_'), out ResponseErrors errorType))
            {
                switch (errorType)
                {
                    case ResponseErrors.unknown_user:
                        Logger.LogIncomingError($"Login error occured: {error}");
                        config.AskForNick();
                        config.Save();
                        TokenHandler.Login(config, socket);
                        break;
                    case ResponseErrors.invalid_token:
                        Logger.LogIncomingError($"Invalid token");
                        TokenHandler.Login(config, socket);
                        break;
                    case ResponseErrors.not_connected:
                        Logger.LogIncomingError($"Not connected");
                        TokenHandler.Login(config, socket);
                        break;
                    case ResponseErrors.wrong_password:
                        Logger.LogIncomingError($"Wrong password");
                        TokenHandler.Login(config, socket);
                        break;
                    default:
                        Logger.LogIncomingError($"Unhandled error: {error}");
                        break;
                }
            }
            else
            {
                Logger.LogIncomingError($"Unknown error type: {error}");
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
                EnableRaisingEvents = true
            };
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
            var message = $"Watching project {(string)selectedProject.title} directory: {localProjectPath}";
            Logger.LogLocalInfo(message, ConsoleColor.Black, ConsoleColor.DarkGreen);
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

            if (changeHistory.ContainsKey(filePath))
            {
                changeHistory.Remove(filePath);
            }
            DeleteRemoteFile(filePath, selectedProject, config, socket);
        }

        private static void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            var filePath = e.Name.Replace('\\', '/');
            if (!ShouldPerformFileChange(filePath))
            {
                return;
            }

            if (RenameIfNeeded(filePath, e.FullPath))
            {
                return;
            }

            PushFile(filePath, selectedProject, config, socket);
        }

        private static bool RenameIfNeeded(string filePath, string fullPath)
        {
            var newStringBuilder = new StringBuilder();
            newStringBuilder.Append(filePath.Normalize(NormalizationForm.FormKD)
                                            .Where(x => x < 128)
                                            .ToArray());

            var clearedPath = Regex.Replace(newStringBuilder.ToString(), @"[^\w\d_/\.]", "").ToLower();
            if (filePath != clearedPath)
            {
                Logger.LogLocalError($"File name {filePath} is not allowed in microStudio. Renaming to {clearedPath}");
                var newFullPath = Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileName(clearedPath));
                Task.Run(async () => {
                    await Task.Delay(1000);
                    System.IO.File.Move(fullPath, newFullPath);
                });
                return true;
            }

            return false;
        }

        private static void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            var filePath = e.Name.Replace('\\', '/');
            var oldFilePath = e.OldName.Replace('\\', '/');
            if (!ShouldPerformFileChange(filePath))
            {
                return;
            }

            if (changeHistory.ContainsKey(oldFilePath))
            {
                changeHistory.Remove(oldFilePath);
            }

            if(RenameIfNeeded(filePath, e.FullPath))
            { 
                return;
            }

            DeleteRemoteFile(oldFilePath, selectedProject, config, socket);
            PushFile(filePath, selectedProject, config, socket);
        }

        private static bool ShouldPerformFileChange(string filePath)
        {
            var projectDirectory = (string)selectedProject.title;
            if (!System.IO.File.Exists(Path.Combine(config.localDirectory, projectDirectory, filePath)))
            {
                return false;
            }

            Thread.Sleep(1000);
            var content = ReadFileContent(filePath, projectDirectory, config);

            if (changeHistory.ContainsKey(filePath))
            {
                var (lastContent, fromRemote) = changeHistory[filePath];

                if (fromRemote || content == lastContent || content == null)
                {
                    return false;
                }
            }

            if (lockStreams.ContainsKey(filePath))
            {
                return false;
            }

            if (!subDirectories.Contains(Path.GetDirectoryName(filePath)))
            {
                return false;
            }
            changeHistory[filePath] = (content, false);

            return true;
        }

        private static void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var filePath = e.Name.Replace('\\', '/');
            if (!ShouldPerformFileChange(filePath))
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
                Logger.LogLocalError($"File {filePath} in project {slug} does not exist");
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
            if (!System.IO.File.Exists(localFilePath))
            {
                return null;
            }

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

        private static void PullFiles(Config config, WebsocketClient socket)
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
                    Logger.LogLocalQuery("Project slug to backup (leave empty to see available projects): ");
                    projectSlug = Console.ReadLine();
                }

                if (projects.ContainsKey(projectSlug))
                {
                    selectedProject = projects[projectSlug];
                    break;
                }
                else
                {
                    Logger.LogLocalError($"Pick project slug from the list: (Press any key to continue)");
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
