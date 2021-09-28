using MTSC.Exceptions;
using System;
using System.Linq;
using System.Text;

namespace MTSC.Common.Ftp
{
    public class FtpResponse
    {
        public FtpResponseCodes StatusCode { get; set; }

        public string Message { get; set; }

        public FtpResponse()
        {

        }

        private FtpResponse(byte[] bytes)
        {
            var message = Encoding.UTF8.GetString(bytes);
            var tokens = message.Trim('\0').Trim('\n').Trim('\r').Split(' ');
            if (!int.TryParse(tokens[0], out var statusCodeInt))
            {
                throw new InvalidFtpStatusCodeException($"Expected status code but found [{tokens[0]}]");
            }

            if (!Enum.IsDefined(typeof(FtpResponseCodes), statusCodeInt))
            {
                throw new InvalidFtpStatusCodeException($"Undefined status code [{statusCodeInt}]");
            }

            this.StatusCode = (FtpResponseCodes)statusCodeInt;
            this.Message = tokens.Length > 1 ? tokens.Skip(1).Aggregate((current, next) => current + ' ' + next) : null;
        }

        public static FtpResponse FromBytes(byte[] bytes)
        {
            return new FtpResponse(bytes);
        }

        public static byte[] ToBytes(FtpResponse response)
        {
            var sb = new StringBuilder();
            sb.Append((int)response.StatusCode);
            if (!string.IsNullOrWhiteSpace(response.Message))
            {
                sb.Append(' ').Append(response.Message);
            }

            sb.Append("\r\n");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
