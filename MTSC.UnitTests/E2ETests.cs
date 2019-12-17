using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Common.Http.ServerModules;
using MTSC.Common.WebSockets.ServerModules;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.Server;
using MTSC.Server.Handlers;
using MTSC.Server.UsageMonitors;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC.UnitTests
{
    [TestClass]
    public class E2ETests
    {
        private static int stressIterations = 100000;
        public TestContext TestContext { get; set; }
        static Server.Server Server { get; set; }

        [ClassInitialize]
        public static void InitializeServer(TestContext testContext)
        {
            Server = new Server.Server(800)
                .AddHandler(new WebsocketHandler()
                    .AddWebsocketHandler(new EchoModule())
                    )
                .AddHandler(new HttpHandler()
                    .AddHttpModule(new HelloWorldModule())
                    )
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .AddExceptionHandler(new ExceptionConsoleLogger());
                //.AddServerUsageMonitor(new TickrateEnforcer().SetTicksPerSecond(60));
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