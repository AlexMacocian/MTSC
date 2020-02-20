using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.Common.Http.ServerModules;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.Server.Handlers;
using System;
using System.Diagnostics;
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
        private volatile byte[] receivedMessage = null;
        private static int stressIterations = 1000;
        public TestContext TestContext { get; set; }
        static Server.Server Server { get; set; }

        [ClassInitialize]
        public static void InitializeServer(TestContext testContext)
        {
            Server = new Server.Server(800)
                .AddHandler(new WebsocketHandler()
                    .AddWebsocketHandler(new Common.WebSockets.ServerModules.EchoModule()))
                .AddHandler(new HttpHandler()
                    .AddHttpModule(new HttpRoutingModule()
                        .AddRoute(HttpMessage.HttpMethods.Get, "/", new Http200Module())
                        .AddRoute(HttpMessage.HttpMethods.Get, "/query", new TestQueryModule())
                        .AddRoute(HttpMessage.HttpMethods.Get, "/echo", new EchoModule()))
                    .WithFragmentsExpirationTime(TimeSpan.FromSeconds(1))
                    .WithMaximumSize(300))
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
        public void SendFragmentedHttpMessage()
        {
            Client.Client client = new Client.Client();
            var notifyHandler = new NotifyReceivedMessageHandler();
            notifyHandler.ReceivedMessage += (o, m) => { receivedMessage = m.MessageBytes; };
            client.SetServerAddress("127.0.0.1")
                .SetPort(800)
                .AddHandler(notifyHandler)
                .Connect();

            HttpRequest request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Get;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            byte[] message = request.GetPackedRequest();

            client.QueueMessage(message.Take(5).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(5).Take(message.Length - request.BodyString.Length - 5).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).ToArray());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (receivedMessage == null)
            {
                if(sw.ElapsedMilliseconds > 15000)
                {
                    throw new Exception("Response not received!");
                }
            }
            HttpResponse response = HttpResponse.FromBytes(receivedMessage);
            Assert.AreEqual(response.StatusCode, HttpMessage.StatusCodes.OK);
            Assert.AreEqual(response.BodyString, "Brought a message to you my guy!");
        }

        [TestMethod]
        public void SendFragmentedHttpMessageShouldExpire()
        {
            Client.Client client = new Client.Client();
            var notifyHandler = new NotifyReceivedMessageHandler();
            notifyHandler.ReceivedMessage += (o, m) => { receivedMessage = m.MessageBytes; };
            client.SetServerAddress("127.0.0.1")
                .SetPort(800)
                .AddHandler(notifyHandler)
                .Connect();

            HttpRequest request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Get;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            byte[] message = request.GetPackedRequest();

            client.QueueMessage(message.Take(5).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(5).Take(message.Length - request.BodyString.Length - 5).ToArray());
            Thread.Sleep(1300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).ToArray());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (receivedMessage == null)
            {
                if (sw.ElapsedMilliseconds > 5000)
                {
                    return;
                }
            }
            Assert.Fail("Should not receive any response due to fragments expiring before being put back together!");
        }

        [TestMethod]
        public void SendFragmentedHttpMessageExceedingSizeShouldExpire()
        {
            Client.Client client = new Client.Client();
            var notifyHandler = new NotifyReceivedMessageHandler();
            notifyHandler.ReceivedMessage += (o, m) => { receivedMessage = m.MessageBytes; };
            client.SetServerAddress("127.0.0.1")
                .SetPort(800)
                .AddHandler(notifyHandler)
                .Connect();

            HttpRequest request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Get;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            request.Body = new byte[300];
            Array.Fill<byte>(request.Body, 50);
            byte[] message = request.GetPackedRequest();

            client.QueueMessage(message.Take(5).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(5).Take(message.Length - request.BodyString.Length - 5).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).ToArray());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (receivedMessage == null)
            {
                if (sw.ElapsedMilliseconds > 5000)
                {
                    return;
                }
            }
            Assert.Fail("Message should be discarded due to exceeding the size limit");
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
