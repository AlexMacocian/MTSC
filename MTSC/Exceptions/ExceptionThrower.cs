using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Throws all handled exceptions.
    /// </summary>
    public class ExceptionThrower : IExceptionHandler
    {
        public bool HandleException(Exception e)
        {
            throw e;
        }
    }
}
