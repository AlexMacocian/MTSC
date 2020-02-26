using System;

namespace MTSC.Logging
{
    public sealed class DebugConsoleLogger : ILogger
    {
        /// <summary>
        /// Ignores simple logging messages.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>False</returns>
        public bool Log(string message)
        {
            return false;
        }
        /// <summary>
        /// Outputs the debug message to console.
        /// </summary>
        /// <param name="message">Message to be output.</param>
        /// <returns>False</returns>
        public bool LogDebug(string message)
        {
            Console.WriteLine(message);
            return false;
        }
    }
}
