using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Common;

namespace MTSC.UnitTests
{
    [TestClass]
    public sealed class ExtendedUrlTests
    {
        private const string Placeholder1 = "{placeholder1}";
        private const string Placeholder2 = "{placeholder2}";
        private const string Value1 = "12345123";
        private const string Value2 = "jdddw+_@#%!*@($&&@50*^23<cansieWQE";

        [TestMethod]
        [DataRow("https://test.com")]
        [DataRow("/test")]
        [DataRow("test/{placeholder}/test2")]
        [DataRow("test/{placeholder}/test2/{placeholder2}")]
        public void ExtendedUrl_ParsesUrl(string url)
        {
            _ = new ExtendedUrl(url);
        }

        [TestMethod]
        [DataRow("https://test/{placeholder}/test2/{placeholder2}", "https://test/something/test2/somethingElse%20%")]
        [DataRow("/test/{placeholder}/test2/{placeholder2}", "/test/something/test2/somethingElse%20%")]
        public void ExtendedUrl_MatchesCorrectUrl(string initialUrl, string matchingUrl)
        {
            var extendedUrl = new ExtendedUrl(initialUrl);
            var result = extendedUrl.TryMatchUrl(matchingUrl, out var _);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ExtendedUrl_ExtractsExpectedValues()
        {
            var extendedUrl = new ExtendedUrl($"/test/{Placeholder1}/test2/{Placeholder2}");
            var result = extendedUrl.TryMatchUrl($"/test/{Value1}/test2/{Value2}", out var values);

            result.Should().BeTrue();
            values.Should().HaveCount(2);
            values.First().Placeholder = Placeholder1.Trim('{', '}');
            values.Last().Placeholder = Placeholder2.Trim('{', '}');
            values.First().Value = Value1;
            values.Last().Value = Value2;
        }
    }
}
