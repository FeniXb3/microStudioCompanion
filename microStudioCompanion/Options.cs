using CommandLine;

namespace microStudioCompanion
{
    class Options
    {
        [Option('m', "mode", Required = false, HelpText = "Mode the app should work in.")]
        public string Mode { get; set; }

        [Option('s', "slug", Required = false, HelpText = "Slug of the project the app should work on.")]
        public string Slug { get; set; }

        [Option('t', "timestamps", Required = false, HelpText = "Flag defining if timestamps should be shown in logs. False by default.")]
        public bool Timestamps { get; set; }

        [Option("no-color", Required = false, HelpText = "Flag defining if the app should ommit displaying messages in color. False by default.")]
        public bool NoColor { get; set; }

        [Option('c', "clean-start", Required = false, HelpText = "Flag defining if project subdirectories should be removed before pulling all current files at the beginning in watch and pull modes. False by default.")]

        public bool CleanStart { get; internal set; }
    }
}
