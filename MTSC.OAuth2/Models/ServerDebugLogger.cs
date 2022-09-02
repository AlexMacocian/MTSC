using Microsoft.Extensions.Logging;
using MTSC.ServerSide;
using System;

namespace MTSC.OAuth2.Models
{
    internal sealed class ServerDebugLogger<T> : ILogger<T>
    {
        private readonly Server server;

        public ServerDebugLogger(Server server)
        {
            this.server = server;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            this.server.LogDebug(message);
        }
    }
}
