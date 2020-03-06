using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTSC.UnitTests
{
    [TestClass]
    public class ResourcesTests
    {
        public static ServerSide.Server server;
        [ClassInitialize]
        public static void InitializeServer(TestContext testContext)
        {
            server = new ServerSide.Server();
        }
        [TestMethod]
        public void AddAndGetResource()
        {
            StringResource resource = new StringResource { Value = "hello" };
            server.WithResource(resource);
            var gotResource = server.GetResource<StringResource>();
            Assert.AreEqual(gotResource, resource);
        }
    }
}
