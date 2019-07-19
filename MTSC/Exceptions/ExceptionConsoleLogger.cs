using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Logs the received exceptions to the console.
    /// </summary>
    public class ExceptionConsoleLogger : IExceptionHandler
    {
        public bool HandleException(Exception e)
        {
            Console.WriteLine(e.Message + "\nStackTrace:\n" + e.StackTrace);
            return false;
        }
    }
}
