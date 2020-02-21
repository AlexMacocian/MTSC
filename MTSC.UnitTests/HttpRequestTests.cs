using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Common.Http;
using System.Text;

namespace MTSC.UnitTests
{
    [TestClass]
    public class HttpRequestTests
    {
        private const string postWithHeader = "POST /test/demo_form.php HTTP/1.1\n\rHost: w3schools.com\n\r\n\r";

        private const string emptyGet = "GET / HTTP/1.1\n\r\n\r";

        [DataTestMethod]
        [DataRow(postWithHeader)]
        [DataRow(emptyGet)]
        public void ParseShouldPass(string requestString)
        {
            HttpRequest request = HttpRequest.FromBytes(ASCIIEncoding.ASCII.GetBytes(requestString));
        }

        [DataTestMethod]
        [DataRow(postWithHeader, "test/demo_form.php")]
        [DataRow(emptyGet, "")]
        public void UriShouldBeAsExpected(string requestString, string uri)
        {
            HttpRequest request = HttpRequest.FromBytes(ASCIIEncoding.ASCII.GetBytes(requestString));
            Assert.AreEqual(request.RequestURI, uri);
        }

        [TestMethod]
        public void HeaderShouldExist()
        {
            HttpRequest request = HttpRequest.FromBytes(ASCIIEncoding.ASCII.GetBytes(postWithHeader));
            Assert.AreEqual(request.Headers[HttpMessage.RequestHeaders.Host], "w3schools.com");
        }
    }
}
