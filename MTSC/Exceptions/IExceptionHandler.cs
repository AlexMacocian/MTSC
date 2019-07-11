using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Handler to be used for handling exception.
    /// </summary>
    public interface IExceptionHandler
    {
        /// <summary>
        /// Handles exceptions.
        /// </summary>
        /// <param name="e">Exception to be handled.</param>
        /// <returns>True if it handled the exception. Otherwise returns false.</returns>
        bool HandleException(Exception e);
    }
}
