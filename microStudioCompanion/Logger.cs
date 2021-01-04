using System;
using System.Collections.Generic;
using System.Text;

namespace microStudioCompanion
{
    class Logger
    {
        public static void LogOutgoingInfo(string text)
        {
            Console.WriteLine($"  <-  [i] {text}");
        }
        public static void LogIncomingInfo(string text)
        {
            Console.WriteLine($"  ->  [i] {text}");
        }
        public static void LogLocalInfo(string text)
        {
            Console.WriteLine($"      [i] {text}");
        }
    }
}
