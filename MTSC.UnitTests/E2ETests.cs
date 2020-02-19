using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Client;
using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.Common.Http.ServerModules;
using MTSC.Common.WebSockets.ServerModules;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.Server.Handlers;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MTSC.UnitTests
{
    [TestClass]
    public class E2ETests
    {
        private static int stressIterations = 1000;
        public TestContext TestContext { get; set; }
        static Server.Server Server { get; set; }

        [ClassInitialize]
        public static void InitializeServer(TestContext testContext)
        {
            Server = new Server.Server(800)
                //.AddHandler(new WebsocketHandler()
                //    .AddWebsocketHandler(new EchoModule()))
                .AddHandler(new HttpHandler()
                    .AddHttpModule(new HttpRoutingModule()
                        .AddRoute(HttpMessage.HttpMethods.Get, "/", new Http200Module())
                        .AddRoute(HttpMessage.HttpMethods.Get, "/query", new TestQueryModule())))
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .AddExceptionHandler(new ExceptionConsoleLogger());
            Server.RunAsync().Start();
        }

        [TestMethod]
        public void HelloWorldHTTP()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var result = httpClient.GetAsync("").Result;
            Assert.AreEqual(result.StatusCode, System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void GetWithQueryHttp()
        {
            var builder = new UriBuilder("http://localhost:800/query");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["key1"] = "value1";
            query["key2"] = "value2";
            builder.Query = query.ToString();
            string url = builder.ToString();

            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            var result = httpClient.GetAsync("").Result;
            Assert.AreEqual(result.StatusCode, System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void EchoWebsocket()
        {
            byte[] bytes = new byte[100];
            ClientWebSocket client = new ClientWebSocket();
            client.ConnectAsync(new Uri("ws://localhost:800"), CancellationToken.None).Wait();
            client.SendAsync(ASCIIEncoding.ASCII.GetBytes("Hello world!"), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
            client.ReceiveAsync(bytes, CancellationToken.None).Wait();
            var resultString = ASCIIEncoding.ASCII.GetString(bytes, 0, 12);
            Assert.AreEqual(resultString, "Hello world!");
        }

        [TestMethod]
        public void HTTPStressTest()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            for(int i = 0; i < stressIterations; i++)
            {
                var startTime = DateTime.Now;
                var tasks = new Task[Environment.ProcessorCount];
                for(int j = 0; j < Environment.ProcessorCount; j++)
                {
                    tasks[j] = httpClient.GetAsync("");
                }
                Task.WaitAll(tasks);
                var duration = DateTime.Now - startTime;
                foreach(Task<HttpResponseMessage> t in tasks)
                {
                    Assert.AreEqual(t.Result.StatusCode, System.Net.HttpStatusCode.OK);
                }
                TestContext.WriteLine($"{i}: Processed {tasks.Length} requests in {duration.TotalMilliseconds} ms.");
            }           
        }

        [ClassCleanup]
        public static void CleanupServer()
        {
            Server.Stop();
        }
    }
}
