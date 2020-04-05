namespace MTSC.Common.Ftp
{
    /// <summary>
    /// List of Ftp response codes. Compiled from https://en.wikipedia.org/wiki/List_of_FTP_server_return_codes
    /// </summary>
    public enum FtpResponseCodes
    {
        /// <summary>
        /// The requested action is being initiated, expect another reply before proceeding with a new command.
        /// </summary>
        ActionInitiated = 100,
        /// <summary>
        /// Restart marker replay . In this case, the text is exact and not left to the particular implementation; it must read: MARK yyyy = mmmm where yyyy is User-process data stream marker, and mmmm server's equivalent marker (note the spaces between markers and "=").
        /// </summary>
        RestartMarkerReplay = 101,
        /// <summary>
        /// Service ready in nnn minutes.
        /// </summary>
        ServiceReadyInMinutes = 120,
        /// <summary>
        /// Data connection already open; transfer starting.
        /// </summary>
        TransferStarting = 125,
        /// <summary>
        /// File status okay; about to open data connection.
        /// </summary>
        FileStatusOkay = 150,
        /// <summary>
        /// The requested action has been successfully completed.
        /// </summary>
        ActionCompleted = 200,
        /// <summary>
        /// Command not implemented, superfluous at this site.
        /// </summary>
        CommandNotImplementedSuperfluous = 202,
        /// <summary>
        /// System status, or system help reply.
        /// </summary>
        SystemStatus = 211,
        /// <summary>
        /// Directory status.
        /// </summary>
        DirectoryStatus = 212,
        /// <summary>
        /// File status.
        /// </summary>
        FileStatus = 213,
        /// <summary>
        /// Help message. Explains how to use the server or the meaning of a particular non-standard command. This reply is useful only to the human user.
        /// </summary>
        HelpMessage = 214,
        /// <summary>
        /// NAME system type. Where NAME is an official system name from the registry kept by IANA.
        /// </summary>
        NAMESystemType = 215,
        /// <summary>
        /// Service ready for new user.
        /// </summary>
        ServiceReady = 220,
        /// <summary>
        /// Service closing control connection.
        /// </summary>
        ServiceClosingControlConnection = 221,
        /// <summary>
        /// Data connection open; no transfer in progress.
        /// </summary>
        NoTransferInProgress = 225,
        /// <summary>
        /// Closing data connection. Requested file action successful (for example, file transfer or file abort).
        /// </summary>
        ClosingDataConnection = 226,
        /// <summary>
        /// Entering Passive Mode (h1,h2,h3,h4,p1,p2).
        /// </summary>
        EnterPassiveMode = 227,
        /// <summary>
        /// Entering Long Passive Mode (long address, port).
        /// </summary>
        EnterLongPassiveMode = 228,
        /// <summary>
        /// Entering Extended Passive Mode (|||port|).
        /// </summary>
        EnterExtendedPassiveMode = 229,
        /// <summary>
        /// User logged in, proceed. Logged out if appropriate.
        /// </summary>
        UserLoggedIn = 230,
        /// <summary>
        /// User logged out; service terminated.
        /// </summary>
        UserLoggedOut = 231,
        /// <summary>
        /// Logout command noted, will complete when transfer done.
        /// </summary>
        LogoutCommandNoted = 232,
        /// <summary>
        /// Specifies that the server accepts the authentication mechanism specified by the client, and the exchange of security data is complete. A higher level nonstandard code created by Microsoft.
        /// </summary>
        AcceptAuthentication = 234,
        /// <summary>
        /// Requested file action okay, completed.
        /// </summary>
        RequestedFileActionOkay = 250,
        /// <summary>
        /// "PATHNAME" created.
        /// </summary>
        PATHNAMECreated = 257,
        /// <summary>
        /// The command has been accepted, but the requested action is on hold, pending receipt of further information.
        /// </summary>
        CommandAccepted = 300,
        /// <summary>
        /// User name okay, need password.
        /// </summary>
        NeedPassword = 331,
        /// <summary>
        /// Need account for login.
        /// </summary>
        NeedAccount = 332,
        /// <summary>
        /// Requested file action pending further information
        /// </summary>
        RequestedActionPending = 350,
        /// <summary>
        /// The command was not accepted and the requested action did not take place, but the error condition is temporary and the action may be requested again.
        /// </summary>
        TemporaryError = 400,
        /// <summary>
        /// Service not available, closing control connection. This may be a reply to any command if the service knows it must shut down.
        /// </summary>
        ServiceNotAvailable = 421,
        /// <summary>
        /// Can't open data connection.
        /// </summary>
        NoDataConnection = 425,
        /// <summary>
        /// Connection closed; transfer aborted.
        /// </summary>
        ConnectionClosed = 426,
        /// <summary>
        /// Invalid username or password
        /// </summary>
        InvalidUsernameOrPassword = 430,
        /// <summary>
        /// Requested host unavailable.
        /// </summary>
        RequestedHostUnavailable = 434,
        /// <summary>
        /// Requested file action not taken.
        /// </summary>
        RequestedFileActionNotTaken = 450,
        /// <summary>
        /// Requested action aborted. Local error in processing.
        /// </summary>
        RequestedActionAborted = 451,
        /// <summary>
        /// Requested action not taken. Insufficient storage space in system.File unavailable (e.g., file busy).
        /// </summary>
        RequestedActionNotTaken = 452,
        /// <summary>
        /// Syntax error, command unrecognized and the requested action did not take place. This may include errors such as command line too long.
        /// </summary>
        SyntaxError = 500,
        /// <summary>
        /// Syntax error in parameters or arguments.
        /// </summary>
        SyntaxErrorInParameters = 501,
        /// <summary>
        /// Command not implemented.
        /// </summary>
        CommandNotImplemented = 502,
        /// <summary>
        /// Bad sequence of commands.
        /// </summary>
        BadSequenceOfCommands = 503,
        /// <summary>
        /// Command not implemented for that parameter.
        /// </summary>
        CommandNotImplementedForParameter = 504,
        /// <summary>
        /// Not logged in.
        /// </summary>
        NotLoggedIn = 530,
        /// <summary>
        /// Need account for storing files.
        /// </summary>
        NeedAccountForStoring = 532,
        /// <summary>
        /// Could Not Connect to Server - Policy Requires SSL
        /// </summary>
        RequireSSL = 534,
        /// <summary>
        /// Requested action not taken. File unavailable (e.g., file not found, no access).
        /// </summary>
        FileUnavailable = 550,
        /// <summary>
        /// Requested action aborted. Page type unknown.
        /// </summary>
        PageTypeUnknown = 551,
        /// <summary>
        /// Requested file action aborted. Exceeded storage allocation (for current directory or dataset).
        /// </summary>
        ExceededStorageAllocation = 552,
        /// <summary>
        /// Requested action not taken. File name not allowed.
        /// </summary>
        FileNameNotAllowed = 553,
        /// <summary>
        /// Replies regarding confidentiality and integrity.
        /// </summary>
        ReplyRegardingConfidentiality = 600,
        /// <summary>
        /// Integrity protected reply.
        /// </summary>
        IntegrityProtected = 631,
        /// <summary>
        /// Confidentiality and integrity protected reply.
        /// </summary>
        ConfidentialityAndIntegrityProtected = 632,
        /// <summary>
        /// Confidentiality protected reply.
        /// </summary>
        ConfidentialityProtected = 633,
    }
}
