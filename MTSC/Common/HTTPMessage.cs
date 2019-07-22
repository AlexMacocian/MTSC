using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Common
{
    public class HTTPMessage
    {
        private enum MethodEnum
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
        private static string[] methods = new string[] { "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT", "extension-method" };

        public Dictionary<string, string> GeneralHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> RequestHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> ResponseHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> EntityHeaders = new Dictionary<string, string>();
    }
}
