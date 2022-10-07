namespace MTSC.ServerSide.Handlers;

/// <summary>
/// Implement this interface in handlers that need to run a procedure on server initialization.
/// </summary>
public interface IRunOnInitialization
{
    void OnInitialization(Server server);
}
