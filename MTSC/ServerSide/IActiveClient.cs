namespace MTSC.ServerSide
{
    interface IActiveClient
    {
        /// <summary>
        /// Indicates that there is currently a reading operation on the client
        /// </summary>
        bool ReadingData { get; set; }
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
