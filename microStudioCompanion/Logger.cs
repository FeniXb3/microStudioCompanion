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
        private static ConsoleColor defaultBackgroundColor = ConsoleColor.Black;

        private static string incoming = "->";
        private static string outgoing = "<-";
        private static string local = "  ";

        public static void LogOutgoingInfo(string text, bool withNewLine = true)
        {
            Log(text, info, outgoing, infoColor, defaultBackgroundColor, withNewLine);
        }

        public static void LogIncomingInfo(string text, bool withNewLine = true)
        {
            Log(text, info, incoming, infoColor, defaultBackgroundColor, withNewLine);
        }

        public static void LogLocalInfo(string text, ConsoleColor? textColor = null, ConsoleColor? backgroundColor = null, bool withNewLine = true)
        {
            Log(text, info, local, textColor ?? infoColor, backgroundColor ?? defaultBackgroundColor, withNewLine);
        }

        internal static void LogLocalError(string text, bool withNewLine = true)
        {
            Log(text, error, local, errorColor, defaultBackgroundColor, withNewLine);
        }

        internal static void LogIncomingError(string text, bool withNewLine = true)
        {
            Log(text, error, incoming, errorColor, defaultBackgroundColor, withNewLine);
        }

        internal static void LogLocalQuery(string text, bool withNewLine = false)
        {
            Log(text, query, local, queryColor, defaultBackgroundColor, withNewLine);
        }

        private static void Log(string text, string type, string source, ConsoleColor textColor, ConsoleColor backgroundColor, bool withNewLine = true)
        {
            Console.ForegroundColor = textColor;
            Console.BackgroundColor = backgroundColor;
            Console.Write($" {source} {type} {text}");
            Console.ResetColor();
            if (withNewLine)
            {
                Console.WriteLine();
            }
        }
    }
}
