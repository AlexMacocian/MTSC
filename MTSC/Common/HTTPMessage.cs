using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Common
{
    public class HTTPMessage
    {
        public enum StatusCodes
        {
            Continue = 100,
            SwitchingProtocols = 101,
            OK = 200,
            Created = 201,
            Accepted = 202,
            NonAuthoritativeInformation = 203,
            NoContent = 204,
            ResetContent = 205,
            PartialContent = 206,
            MultipleChoices = 300,
            MovedPermanently = 301,
            Found = 302,
            SeeOther = 303,
            NotModified = 304,
            UseProxy = 305,
            TemporaryRedirect = 307,
            BadRequest = 400,
            Unauthorized = 401,
            PaymentRequired = 402,
            Forbidden = 403,
            NotFound = 404,
            MethodNotAllowed = 405,
            NotAcceptable = 406,
            ProxyAuthenticationRequired = 407,
            RequestTimeout = 408,
            Conflict = 409,
            Gone = 410,
            LengthRequired = 411,
            PreconditionFailed = 412,
            RequestEntityTooLarge = 413,
            RequestURITooLarge = 414,
            UnsupportedMediaType = 415,
            RequestRangeNotSatisfiable = 416,
            ExpectationFailed = 417,
            InternalServerError = 500,
            NotImplemented = 501,
            BadGateway = 502,
            ServiceUnavailable = 503,
            GatewayTimeout = 504,
            HTTPVersionNotSupported = 505
        }
        public enum MethodEnum
        {
            Options = 0,
            Get = 1,
            Head = 2,
            Post = 3,
            Put = 4,
            Delete = 5,
            Trace = 6,
            Connect = 7,
            ExtensionMethod = 8
        }
        public enum GeneralHeadersEnum
        {
            CacheControl = 0,
            Connection = 1,
            Date = 2,
            Pragma = 3,
            Trailer = 4,
            TransferEncoding = 5,
            Upgrade = 6,
            Via = 7,
            Warning = 8
        }
        public enum RequestHeadersEnum
        {
            Accept = 0,
            AcceptCharset = 1,
            AcceptEncoding = 2,
            AcceptLanguage = 3,
            Authorization = 4,
            Expect = 5,
            From = 6,
            Host = 7,
            IfMatch = 8,
            IfModifiedSince = 9,
            IfNoneMatch = 10,
            IfRange = 11,
            IfUnmodifiedSince = 12,
            MaxForwards = 13,
            ProxyAuthorization = 14,
            Range = 15,
            Referer = 16,
            TE = 17,
            UserAgent = 18
        }
        public enum ResponseHeadersEnum
        {
            AcceptRanges = 0,
            Age = 1,
            ETag = 2,
            Location = 3,
            ProxyAuthentication = 4,
            RetryAfter = 5,
            Server = 6,
            Vary = 7,
            WWWAuthenticate = 8
        }
        public enum EntityHeadersEnum
        {
            Allow = 0,
            ContentEncoding = 1,
            ContentLanguage = 2,
            ContentLength = 3,
            ContentLocation = 4,
            ContentMD5 = 5,
            ContentRange = 6,
            ContentType = 7,
            Expired = 8,
            LastModified = 9
        }
        private static string[] methods = new string[] { "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT", "extension-method" };
        private static string[] generalHeaders = new string[] { "Cache-Control", "Connection", "Date", "Pragma", "Trailer", "Transfer-Encoding", "Upgrade", "Via", "Warning" };
        private static string[] requestHeaders = new string[] { "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language", "Authorization", "Expect", "From", "Host", "If-Match",
        "If-Modified-Since", "If-None-Match", "If-Range", "If-Unmodified-Since", "Max-Forwards", "Proxy-Authorizatio", "Range", "Referer", "TE", "User-Agent"};
        private static string[] responseHeaders = new string[] { "Accept-Ranges", "Age", "ETag", "Location", "Retry-After", "Server", "Vary", "WWW-Authenticate" };
        private static string[] entityHeaders = new string[] { "Allow", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location", "Content-MD5", "Content-Range", "Content-Type",
        "Expires", "Last-Modified" };

        private static char SP = ' ';
        private static char HT = '\t';
        private static string CRLF = "\r\n";
        private static string HTTPVER = "HTTP/1.1";

        /*
         * A request must follow the first line with: Method [SPACE] Request-URI [SPACE] HTTP-Ver [CR][LF]
         * Request URI = "*" | absoulteURI | absPath | auth
         * "*" - means the request applies to the server, not to a particular resource.
         * Authority is used by CONNECT method.
         */

        /*
         * Resume page 50
         */

        private Dictionary<string, string> headers = new Dictionary<string, string>();

        public MethodEnum Method { get; set; }
        public Uri RequestURI { get; set; }
        public byte[] Body { get; set; }

        public HTTPMessage()
        {

        }

        public void AddGeneralHeader(GeneralHeadersEnum header, string value)
        {
            headers.Add(generalHeaders[(int)header], value);
        }

        public void AddRequestHeader(RequestHeadersEnum requestHeader, string value)
        {
            headers.Add(requestHeaders[(int)requestHeader], value);
        }

        public void AddResponseHeader(ResponseHeadersEnum responseHeader, string value)
        {
            headers.Add(responseHeaders[(int)responseHeader], value);
        }

        public void AddEntityHeaders(EntityHeadersEnum entityHeader, string value)
        {
            headers.Add(entityHeaders[(int)entityHeader], value);
        }

        public string GetRequest()
        {
            StringBuilder request = new StringBuilder();
            request.Append(Method.ToString()).Append(SP).Append(RequestURI.ToString()).Append(HTTPVER).Append(CRLF);
            foreach(KeyValuePair<string, string> header in headers)
            {
                request.Append(header.Key).Append(':').Append(SP).Append(header.Value).Append(CRLF);
            }
            return request.ToString();
        }
    }
}
