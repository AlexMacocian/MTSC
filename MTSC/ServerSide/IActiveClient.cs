namespace MTSC.ServerSide
{
    interface IActiveClient
    {
        /// <summary>
        /// Updates the latest received message and activity time to DateTime.Now
        /// </summary>
        void UpdateLastReceivedMessage();
        /// <summary>
        /// Updates the latest activity time to DateTime.Now
        /// </summary>
        void UpdateLastActivity();
    }
}
