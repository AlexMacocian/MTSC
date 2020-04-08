using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Common.Ftp;
using MTSC.Common.Ftp.FtpModules;
using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.Common.Http.ServerModules;
using MTSC.Common.WebSockets;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.Schedulers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
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
        private static int stressIterations = 100000;
        public TestContext TestContext { get; set; }
        static ServerSide.Server Server { get; set; }

        [ClassInitialize]
        public static void InitializeServer(TestContext testContext)
        {
            Server = new ServerSide.Server(800)
                .WithCertificate(new X509Certificate2("mycert.pfx", "password"))
                .WithClientCertificate(false)
                .AddHandler(new WebsocketRoutingHandler()
                    .AddRoute("echo", new EchoWebsocketModule()
                        .WithReceiveTemplateProvider((message) => UTF8Encoding.UTF8.GetString(message.Data))
                        .WithSendTemplateProvider((s) => 
                            {
                                WebsocketMessage websocketMessage = new WebsocketMessage();
                                websocketMessage.Data = UTF8Encoding.UTF8.GetBytes(s);
                                websocketMessage.Opcode = WebsocketMessage.Opcodes.Text;
                                return websocketMessage;
                            })))
                .AddHandler(new HttpRoutingHandler()
                    .AddRoute(HttpMessage.HttpMethods.Get, "", new Http200Module())
                    .AddRoute(HttpMessage.HttpMethods.Get, "query", new TestQueryModule())
                    .AddRoute(HttpMessage.HttpMethods.Get, "echo", new EchoModule())
                    .AddRoute(HttpMessage.HttpMethods.Post, "echo", new EchoModule())
                    .AddRoute(HttpMessage.HttpMethods.Get, "long-running", new LongRunningModule())
                    .WithFragmentsExpirationTime(TimeSpan.FromMilliseconds(3000))
                    .WithMaximumSize(250000))
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .AddExceptionHandler(new ExceptionConsoleLogger())
                .SetScheduler(new TaskAwaiterScheduler())
                .WithSslAuthenticationTimeout(TimeSpan.FromMilliseconds(100));
            Server.RunAsync();
        }
        [TestMethod]
        public async Task ServerRespondsDuringLongRunningTask()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://localhost:800");
            var longRunningTask = httpClient.GetAsync("long-running");
            int responses = 0;
            HttpClient client2 = new HttpClient();
            client2.BaseAddress = new Uri("https://localhost:800");
            while (!longRunningTask.IsCompleted)
            {
                var echoResponse = await client2.GetAsync("echo");
                responses++;
            }
            var result = longRunningTask.Result;
            Assert.AreEqual(result.StatusCode, System.Net.HttpStatusCode.OK);
            Assert.IsTrue(responses > 50);
        }

        [TestMethod]
        public void HelloWorldHTTP()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://localhost:800");
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
                .WithSsl(true)
                .Connect();

            HttpRequest request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Get;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            byte[] message = request.GetPackedRequest();

            client.QueueMessage(message.Take(message.Length - request.BodyString.Length).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).Take(10).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length + 10).Take(request.BodyString.Length - 10).ToArray());
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
            //Trim the null bytes from encryption/decryption
            response.BodyString = response.BodyString.Trim('\0');
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
                .WithSsl(true)
                .Connect();

            HttpRequest request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Get;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            byte[] message = request.GetPackedRequest();

            client.QueueMessage(message.Take(message.Length - request.BodyString.Length).ToArray());
            Thread.Sleep(300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).Take(10).ToArray());
            Thread.Sleep(3300);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length + 10).Take(request.BodyString.Length - 10).ToArray());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (receivedMessage == null)
            {
                if (sw.ElapsedMilliseconds > 5000)
                {
                    Assert.Fail("Should receive that message has expired!");
                }
            }
            var response = HttpResponse.FromBytes(receivedMessage);
            Assert.AreEqual(response.StatusCode, HttpMessage.StatusCodes.BadRequest);
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
                .WithSsl(true)
                .Connect();

            HttpRequest request = new HttpRequest();
            request.Method = HttpMessage.HttpMethods.Get;
            request.BodyString = "Brought a message to you my guy!";
            request.RequestURI = "/echo";
            request.Headers[HttpMessage.EntityHeaders.ContentLength] = request.BodyString.Length.ToString();
            request.Body = new byte[255000];
            Array.Fill<byte>(request.Body, 50);
            byte[] message = request.GetPackedRequest();

            client.QueueMessage(message.Take(message.Length - request.BodyString.Length).ToArray());
            Thread.Sleep(5);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length).Take(10).ToArray());
            Thread.Sleep(5);
            client.QueueMessage(message.Skip(message.Length - request.BodyString.Length + 10).Take(request.BodyString.Length - 10).ToArray());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (receivedMessage == null)
            {
                if (sw.ElapsedMilliseconds > 15000)
                {
                    Assert.Fail("Should receive that message has exceeded size!");
                }
            }
            var response = HttpResponse.FromBytes(receivedMessage);
            Assert.AreEqual(response.StatusCode, HttpMessage.StatusCodes.BadRequest);
        }

        [TestMethod]
        public void SendLargeHttpMessage()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://localhost:800");
            string s = string.Empty;
            for(int i = 0; i < 200000; i++)
            {
                s += "C";
            }
            using(var sc = new ByteArrayContent(Encoding.UTF8.GetBytes(s)))
            {
                var response = httpClient.PostAsync("echo", sc).Result;
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
                Assert.AreEqual(response.Content.ReadAsStringAsync().Result, s);
            }
        }

        [TestMethod]
        public void GetWithQueryHttp()
        {
            var builder = new UriBuilder("https://localhost:800/query");
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
            ServicePointManager.ServerCertificateValidationCallback += (o, e, s, p) => true;
            byte[] bytes = new byte[100];
            ClientWebSocket client = new ClientWebSocket();
            client.ConnectAsync(new Uri("wss://localhost:800/echo"), CancellationToken.None).Wait();
            client.SendAsync(ASCIIEncoding.ASCII.GetBytes("Hello world!"), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
            client.ReceiveAsync(bytes, CancellationToken.None).Wait();
            var resultString = ASCIIEncoding.ASCII.GetString(bytes, 0, 12);
            Assert.AreEqual(resultString, "Hello world!");
        }

        [TestMethod]
        public void HTTPStressTest()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://localhost:800");
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
