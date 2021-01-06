using CommandLine;

namespace microStudioCompanion
{
    class Options
    {
        [Option('m', "mode", Required = false, HelpText = "Mode the app should work in.")]
        public string Mode { get; set; }

        [Option('s', "slug", Required = false, HelpText = "Slug of the project the app should work on.")]
        public string Slug { get; set; }
    }
}
