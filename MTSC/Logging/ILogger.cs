using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Logging
{
    /// <summary>
    /// Interface for loggers.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs the received message.
        /// </summary>
        /// <param name="message">Message to be received.</param>
        /// <returns>True if the message has been logged and no other logger should log this message.</returns>
        bool Log(string message);
    }
}
