using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace microStudioCompanion
{
    public class Config
    {
        public static string configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "config.JSON");
        public string nick { get; set; }
        public string localDirectory { get; set; }

        internal void Save()
        {
            System.IO.File.WriteAllText(configFilePath, JsonSerializer.Serialize(this));
            Console.WriteLine($"Login and projects root dir are saved here: {configFilePath}");
        }

        internal static Config Get()
        {
            Config config = new Config();
            if (!System.IO.File.Exists(configFilePath))
            {
                config.AskForNick();
                config.AskForDirectory();

                config.Save();
            }
            else
            {
                config = Config.Load();
            }

            return config;
        }

        public void AskForNick()
        {
            Console.Write("Your microStudio nick: ");
            this.nick = Console.ReadLine();
        }

        public void AskForDirectory()
        {
            do
            {
                Console.Write("Local projects parent directory (it must exist): ");
                this.localDirectory = Console.ReadLine();
            } while (!Directory.Exists(this.localDirectory));
        }

        private static Config Load()
        {
            return JsonSerializer.Deserialize<Config>(System.IO.File.ReadAllText(configFilePath));
        }
    }
}
