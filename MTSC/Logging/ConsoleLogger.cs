using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Logging
{
    /// <summary>
    /// Basic logger that outputs the log messages to the console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Outputs the message to the console.
        /// </summary>
        /// <param name="message"></param>
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
        /// <summary>
        /// Outputs the exception error and the stacktrace to the console.
        /// </summary>
        /// <param name="e"></param>
        public void Log(Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }
}
