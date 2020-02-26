using System;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Throws all handled exceptions.
    /// </summary>
    public sealed class ExceptionThrower : IExceptionHandler
    {
        public bool HandleException(Exception e)
        {
            throw e;
        }
    }
}
