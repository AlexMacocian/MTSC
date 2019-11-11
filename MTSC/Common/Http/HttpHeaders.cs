using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Common.Http
{
    class HttpHeaders
    {
        public static string[] methods = new string[] { "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT", "extension-method" };
        public static string[] generalHeaders = new string[] { "Cache-Control", "Connection", "Date", "Pragma", "Trailer", "Transfer-Encoding", "Upgrade", "Via", "Warning" };
        public static string[] requestHeaders = new string[] { "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language", "Authorization", "Expect", "From", "Host", "If-Match",
        "If-Modified-Since", "If-None-Match", "If-Range", "If-Unmodified-Since", "Max-Forwards", "Proxy-Authorizatio", "Range", "Referer", "TE", "User-Agent"};
        public static string[] responseHeaders = new string[] { "Accept-Ranges", "Age", "ETag", "Location", "Retry-After", "Server", "Vary", "WWW-Authenticate" };
        public static string[] entityHeaders = new string[] { "Allow", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location", "Content-MD5", "Content-Range", "Content-Type",
        "Expires", "Last-Modified" };

        public static char SP = ' ';
        public static char HT = '\t';
        public static string CRLF = "\r\n";
        public static string HTTPVER = "HTTP/1.1";
        public static string RequestCookieHeader = "Cookie";
        public static string ResponseCookieHeader = "Set-Cookie";
    }
}
