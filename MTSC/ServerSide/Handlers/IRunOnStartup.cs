namespace MTSC.ServerSide.Handlers
{
    /// <summary>
    /// Implement this interface in handlers that need to run a procedure on server startup.
    /// </summary>
    public interface IRunOnStartup
    {
        void OnStartup(Server server);
    }
}
