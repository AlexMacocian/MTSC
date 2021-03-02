namespace MTSC.ClientSide
{
    /// <summary>
    /// Policy for connection issues.
    /// </summary>
    public enum ReconnectPolicy
    {
        /// <summary>
        /// Never attempt to reconnect automatically.
        /// </summary>
        Never,
        /// <summary>
        /// Attempt to reconnect automatically once.
        /// </summary>
        Once,
        /// <summary>
        /// Attempt to forever reconnect automatically unless stopped.
        /// </summary>
        Forever
    }
}
