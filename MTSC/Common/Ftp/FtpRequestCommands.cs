namespace MTSC.Common.Ftp
{
    /// <summary>
    /// Enum containing the FTP Requests, compiled from https://en.wikipedia.org/wiki/List_of_FTP_commands
    /// </summary>
    public enum FtpRequestCommands
    {
        /// <summary>
        /// Abort an active file transfer.
        /// </summary>
        ABOR,
        /// <summary>
        /// Account information.
        /// </summary>
        ACCT,
        /// <summary>
        /// Authentication/Security Data
        /// </summary>
        ADAT,
        /// <summary>
        /// Allocate sufficient disk space to receive a file.
        /// </summary>
        ALLO,
        /// <summary>
        /// Append (with create)
        /// </summary>
        APPE,
        /// <summary>
        /// Authentication/Security Mechanism
        /// </summary>
        AUTH,
        /// <summary>
        /// Get the available space
        /// </summary>
        AVBL,
        /// <summary>
        /// Clear Command Channel
        /// </summary>
        CCC,
        /// <summary>
        /// Change to Parent Directory.
        /// </summary>
        CDUP,
        /// <summary>
        /// Confidentiality Protection Command
        /// </summary>
        CONF,
        /// <summary>
        /// Client / Server Identification
        /// </summary>
        CSID,
        /// <summary>
        /// Change working directory.
        /// </summary>
        CWD,
        /// <summary>
        /// Delete file.
        /// </summary>
        DELE,
        /// <summary>
        /// Get the directory size
        /// </summary>
        DSIZ,
        /// <summary>
        /// Privacy Protected Channel
        /// </summary>
        ENC,
        /// <summary>
        /// Specifies an extended address and port to which the server should connect.
        /// </summary>
        EPRT,
        /// <summary>
        /// Enter extended passive mode.
        /// </summary>
        EPSV,
        /// <summary>
        /// Get the feature list implemented by the server.
        /// </summary>
        FEAT,
        /// <summary>
        /// Returns usage documentation on a command if specified, else a general help document is returned.
        /// </summary>
        HELP,
        /// <summary>
        /// Identify desired virtual host on server, by name.
        /// </summary>
        HOST,
        /// <summary>
        /// Language Negotiation
        /// </summary>
        LANG,
        /// <summary>
        /// Returns information of a file or directory if specified, else information of the current working directory is returned.
        /// </summary>
        LIST,
        /// <summary>
        /// Specifies a long address and port to which the server should connect.
        /// </summary>
        LPRT,
        /// <summary>
        /// Enter long passive mode.
        /// </summary>
        LPSV,
        /// <summary>
        /// Return the last-modified time of a specified file.
        /// </summary>
        MDTM,
        /// <summary>
        /// Modify the creation time of a file.
        /// </summary>
        MFCT,
        /// <summary>
        /// Modify fact (the last modification time, creation time, UNIX group/owner/mode of a file).
        /// </summary>
        MFF,
        /// <summary>
        /// Modify the last modification time of a file.
        /// </summary>
        MFMT,
        /// <summary>
        /// Integrity Protected Command
        /// </summary>
        MIC,
        /// <summary>
        /// Make directory.
        /// </summary>
        MKD,
        /// <summary>
        /// Lists the contents of a directory if a directory is named.
        /// </summary>
        MLSD,
        /// <summary>
        /// Provides data about exactly the object named on its command line, and no others.
        /// </summary>
        MLST,
        /// <summary>
        /// Sets the transfer mode (Stream, Block, or Compressed).
        /// </summary>
        MODE,
        /// <summary>
        /// Returns a list of file names in a specified directory.
        /// </summary>
        NLST,
        /// <summary>
        /// No operation (dummy packet; used mostly on keepalives).
        /// </summary>
        NOOP,
        /// <summary>
        /// Select options for a feature (for example OPTS UTF8 ON).
        /// </summary>
        OPTS,
        /// <summary>
        /// Authentication password.
        /// </summary>
        PASS,
        /// <summary>
        /// Enter passive mode.
        /// </summary>
        PASV,
        /// <summary>
        /// Protection Buffer Size
        /// </summary>
        PBSZ,
        /// <summary>
        /// Specifies an address and port to which the server should connect.
        /// </summary>
        PORT,
        /// <summary>
        /// Data Channel Protection Level.
        /// </summary>
        PROT,
        /// <summary>
        /// Print working directory. Returns the current directory of the host.
        /// </summary>
        PWD,
        /// <summary>
        /// Disconnect.
        /// </summary>
        QUIT,
        /// <summary>
        /// Re initializes the connection.
        /// </summary>
        REIN,
        /// <summary>
        /// Restart transfer from the specified point.
        /// </summary>
        REST,
        /// <summary>
        /// Retrieve a copy of the file
        /// </summary>
        RETR,
        /// <summary>
        /// Remove a directory.
        /// </summary>
        RMD,
        /// <summary>
        /// Remove a directory tree
        /// </summary>
        RMDA,
        /// <summary>
        /// Rename from.
        /// </summary>
        RNFR,
        /// <summary>
        /// Rename to.
        /// </summary>
        RNTO,
        /// <summary>
        /// Sends site specific commands to remote server (like SITE IDLE 60 or SITE UMASK 002). Inspect SITE HELP output for complete list of supported commands.
        /// </summary>
        SITE,
        /// <summary>
        /// Return the size of a file.
        /// </summary>
        SIZE,
        /// <summary>
        /// Mount file structure.
        /// </summary>
        SMNT,
        /// <summary>
        /// Use single port passive mode (only one TCP port number for both control connections and passive-mode data connections)
        /// </summary>
        SPSV,
        /// <summary>
        /// Returns information on the server status, including the status of the current connection
        /// </summary>
        STAT,
        /// <summary>
        /// Accept the data and to store the data as a file at the server site
        /// </summary>
        STOR,
        /// <summary>
        /// Store file uniquely.
        /// </summary>
        STOU,
        /// <summary>
        /// Set file transfer structure.
        /// </summary>
        STRU,
        /// <summary>
        /// Return system type.
        /// </summary>
        SYST,
        /// <summary>
        /// Get a thumbnail of a remote image file
        /// </summary>
        THMB,
        /// <summary>
        /// Sets the transfer mode (ASCII/Binary).
        /// </summary>
        TYPE,
        /// <summary>
        /// Authentication username.
        /// </summary>
        USER,
        /// <summary>
        /// Change to the parent of the current working directory
        /// </summary>
        XCUP,
        /// <summary>
        /// Make a directory
        /// </summary>
        XMKD,
        /// <summary>
        /// Print the current working directory
        /// </summary>
        XPWD,
        /// <summary>
        /// 
        /// </summary>
        XRCP,
        /// <summary>
        /// Remove the directory
        /// </summary>
        XRMD,
        /// <summary>
        /// 
        /// </summary>
        XRSQ,
        /// <summary>
        /// Send, mail if cannot
        /// </summary>
        XSEM,
        /// <summary>
        /// Send to terminal
        /// </summary>
        XSEN
    }
}
