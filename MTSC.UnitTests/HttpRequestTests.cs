using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Common.Http;
using MTSC.Common.Http.Forms;
using System.Text;

namespace MTSC.UnitTests
{
    [TestClass]
    public class HttpRequestTests
    {
        private const string postWithHeader = "POST /test/demo_form.php HTTP/1.1\r\nHost: w3schools.com\r\n\r\n";

        private const string postWithMultipart = "POST / HTTP/1.1\r\nHost: localhost:8000\r\n" +
            "User-Agent: Mozilla/5.0 (X11; Ubuntu; Linux i686; rv:29.0) Gecko/20100101 Firefox/29.0\r\n" +
            "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n" +
            "Accept-Language: en-US,en;q=0.5\r\n" +
            "Accept-Encoding: gzip, deflate\r\n" +
            "Connection: keep-alive\r\n" +
            "Content-Type: multipart/form-data; boundary=---------------------------9051914041544843365972754266 \r\n" +
            "Content-Length: 554\r\r\n\n" +
            "-----------------------------9051914041544843365972754266\r\n" +
            "Content-Disposition: form-data; name=\"text\"\r\n\r\n" +
            "text default\r\n" +
            "-----------------------------9051914041544843365972754266\r\n" +
            "Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
            "Content-Type: text/plain\r\n\r\n" +
            "Content of a.txt.\r\n\r\n" +
            "-----------------------------9051914041544843365972754266\r\n" +
            "Content-Disposition: form-data; name=\"file2\"; filename=\"a.html\"\r\n" +
            "Content-Type: text/html\r\n\r\n" +
            "&lt;!DOCTYPE html&gt;&lt;title&gt;Content of a.html.&lt;/title&gt;\r\n\r\n" +
            "-----------------------------9051914041544843365972754266--";

        private const string emptyGet = "GET / HTTP/1.1\r\n\r\n";

        [DataTestMethod]
        [DataRow(postWithHeader)]
        [DataRow(emptyGet)]
        public void ParseShouldPass(string requestString)
        {
            var request = HttpRequest.FromBytes(ASCIIEncoding.ASCII.GetBytes(requestString));
        }

        [DataTestMethod]
        [DataRow(postWithHeader, "test/demo_form.php")]
        [DataRow(emptyGet, "")]
        public void UriShouldBeAsExpected(string requestString, string uri)
        {
            var request = HttpRequest.FromBytes(ASCIIEncoding.ASCII.GetBytes(requestString));
            Assert.AreEqual(request.RequestURI, uri);
        }

        [TestMethod]
        public void HeaderShouldExist()
        {
            var request = HttpRequest.FromBytes(ASCIIEncoding.ASCII.GetBytes(postWithHeader));
            Assert.AreEqual(request.Headers[HttpMessage.RequestHeaders.Host], "w3schools.com");
        }

        [TestMethod]
        public void RequestShouldParse()
        {
            var bytes = Encoding.UTF8.GetBytes(postWithMultipart);
            var request = HttpRequest.FromBytes(bytes);

            Assert.IsNotNull(request.Form.GetValue<FileContentType>("file2"));
            Assert.IsNotNull(request.Form.GetValue<TextContentType>("text"));
            Assert.IsNotNull(request.Form.GetValue<FileContentType>("file1"));
            request.Form.TryGetValue<FileContentType>("file2", out var value);
            Assert.IsNotNull(value);
        }
        [TestMethod]
        public void BuiltRequestShouldContainFormData()
        {
            var httpRequest = new HttpRequest();
            httpRequest.Method = HttpMessage.HttpMethods.Post;
            httpRequest.Form.SetValue("name", new TextContentType("text/plain", "Some random text here"));
            var requestBytes = httpRequest.GetPackedRequest();
            var requestString = Encoding.UTF8.GetString(requestBytes);
            Assert.IsTrue(requestString.Contains("Some random text here"));
        }
    }
}
