using MTSC.Exceptions;
using System;
using System.Linq;
using System.Text;

namespace MTSC.Common.Ftp
{
    public class FtpRequest
    {
        public FtpRequestCommands Command { get; set; }
        public string[] Arguments { get; set; } = new string[0];

        public FtpRequest()
        {

        }

        private FtpRequest(byte[] bytes)
        {
            var content = Encoding.UTF8.GetString(bytes);
            var tokens = content.Trim('\0').Trim('\n').Trim('\r').Split(' ');
            if(!Enum.TryParse<FtpRequestCommands>(tokens[0].ToUpper(), out var command))
            {
                throw new UnknownFtpCommandException($"Unknown FTP command: {tokens[0]}");
            }
            this.Command = command;
            this.Arguments = tokens.Length > 1 ? tokens.Skip(1).ToArray() : new string[0];
        }

        public static FtpRequest FromBytes(byte[] bytes)
        {
            return new FtpRequest(bytes);
        }

        public static byte[] ToBytes(FtpRequest request)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(request.Command.ToString());
            foreach(string argument in request.Arguments)
            {
                sb.Append(' ').Append(argument);
            }
            sb.Append("\r\n");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
