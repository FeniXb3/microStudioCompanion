using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    class Logger
    {
        private static string info = "[i]";
        private static string error = "<!>";
        private static string query = "(?)";

        private static ConsoleColor infoColor = ConsoleColor.Gray;
        private static ConsoleColor errorColor = ConsoleColor.Red;
        private static ConsoleColor queryColor = ConsoleColor.Blue;

        private static string incoming = "->";
        private static string outgoing = "<-";
        private static string local = "  ";

        public static bool ShowTimestamps { get; set; }
        public static bool ColorMessages { get; internal set; }

        public static void LogOutgoingInfo(string text, bool withNewLine = true)
        {
            Log(text, info, outgoing, infoColor, null, withNewLine);
        }

        public static void LogIncomingInfo(string text, bool withNewLine = true)
        {
            Log(text, info, incoming, infoColor, null, withNewLine);
        }

        public static void LogLocalInfo(string text, ConsoleColor? textColor = null, ConsoleColor? backgroundColor = null, bool withNewLine = true)
        {
            Log(text, info, local, textColor ?? infoColor, backgroundColor, withNewLine);
        }

        internal static void LogLocalError(string text, bool withNewLine = true)
        {
            Log(text, error, local, errorColor, null, withNewLine);
        }

        internal static void LogIncomingError(string text, bool withNewLine = true)
        {
            Log(text, error, incoming, errorColor, null, withNewLine);
        }

        internal static void LogLocalQuery(string text, bool withNewLine = false)
        {
            Log(text, query, local, queryColor, null, withNewLine);
        }

        private static void Log(string text, string type, string source, ConsoleColor? textColor, ConsoleColor? backgroundColor, bool withNewLine = true)
        {
            if (ColorMessages && textColor.HasValue)
            {
                Console.ForegroundColor = textColor.Value;
            }
            if (ColorMessages && backgroundColor.HasValue)
            {
                Console.BackgroundColor = backgroundColor.Value;
            }

            if (ShowTimestamps)
            {
                Console.Write(DateTime.Now.ToString("u"));
            }
            Console.Write($" {source} {type} {text}");
            if (ColorMessages)
            {
                Console.ResetColor();
            }

            if (withNewLine)
            {
                Console.WriteLine();
            }
        }
    }
}
