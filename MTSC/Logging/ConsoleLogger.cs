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
        public bool Log(string message)
        {
            Console.WriteLine(message);
            return false;
        }
    }
}
