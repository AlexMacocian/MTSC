using MTSC.Common.Http.Forms;
using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Common.Http
{
    public sealed class HttpMessage
    {
        private const string contentDisposition = "Content-Disposition";
        private const string formData = "form-data";
        private const string contentType = "Content-Type";

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
        public enum HttpMethods
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
        public enum GeneralHeaders
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
        public enum RequestHeaders
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
        public enum ResponseHeaders
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
        public enum EntityHeaders
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

        private Dictionary<string, string> headers = new Dictionary<string, string>();
        private List<Cookie> cookies = new List<Cookie>();
    }
}
