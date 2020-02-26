using System;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Logs the received exceptions to the console.
    /// </summary>
    public sealed class ExceptionConsoleLogger : IExceptionHandler
    {
        public bool HandleException(Exception e)
        {
            Console.WriteLine(e.Message + "\nStackTrace:\n" + e.StackTrace);
            return false;
        }
    }
}
