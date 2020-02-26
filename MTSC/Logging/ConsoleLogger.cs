using System;

namespace MTSC.Logging
{
    /// <summary>
    /// Basic logger that outputs the log messages to the console.
    /// </summary>
    public sealed class ConsoleLogger : ILogger
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
        /// <summary>
        /// Ignores debug messages.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>False</returns>
        public bool LogDebug(string message)
        {
            return false;
        }
    }
}
